"""Read-only serialized UI checks; writes a validation report, never drives Unity."""
from pathlib import Path
import re, json, hashlib, csv, struct
from ui_batch import ROOT, read, documents, field, components, guid

errors=[]
def check(ok, message):
    if not ok: errors.append(message)
def nums(value):
    return [float(v) for v in re.findall(r': (-?[\d.]+)',value)]
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest().upper()

catalog=json.loads(read(ROOT/'Docs/ui-batch-manifest.json'))
old_guid='199048e4791bb314d88be36dde3e1f0f'
new=ROOT/'Sprites/Navigation/ui_nav_close_t01a.png'
new_guid=guid(str(new)+'.meta')
check(sha(new)=='A694E89C1FF442191D970E2D8974051AD8E0E32379947985FA654C110EF7021C','New PNG hash mismatch')
check(struct.unpack('>II',new.read_bytes()[16:24])==(132,138),'New PNG dimensions')
closes=[]
prefab_results=[]
for path in sorted((ROOT/'Prefabs').glob('*.prefab')):
    docs=documents(path)
    text=read(path)
    check(old_guid not in text,path.name+': old sprite GUID')
    check('m_ReferenceResolution: {x: 1200, y: 2601}' in text and 'm_ScreenMatchMode: 1' in text,path.name+': Canvas scaler')
    for ident,block in docs.items():
        for ref in re.findall(r'\{fileID: (-?\d+)\}',block):
            check(ref=='0' or ref in docs,path.name+': unresolved local reference '+ref)
        if '\n  m_fontAsset:' in block:
            check('guid:' in field(block,'m_fontAsset'),path.name+': missing explicit font')
            check('guid:' in field(block,'m_sharedMaterial'),path.name+': missing font material')
        if not block.startswith('--- !u!1 '): continue
        name=field(block,'m_Name')
        cs=[docs[c] for c in components(block)]
        if name.startswith('ico_'):
            img=next((b for b in cs if '\n  m_Sprite:' in b),None)
            check(img is not None and field(img,'m_Sprite')=='{fileID: 0}' and nums(field(img,'m_Color'))[-1]==0,path.name+': nonempty '+name)
        if name=='Close':
            rt=next(b for b in cs if b.startswith('--- !u!224 '))
            img=next(b for b in cs if '\n  m_Sprite:' in b)
            btn=next(b for b in cs if '\n  m_OnClick:' in b)
            check(nums(field(rt,'m_AnchoredPosition'))==[534,177] and nums(field(rt,'m_SizeDelta'))==[132,138],path.name+': close rect')
            check(new_guid in field(img,'m_Sprite') and field(img,'m_Type')=='0',path.name+': close sprite/type')
            image_id=re.match(r'--- !u!\d+ &(\d+)',img)[1]
            check(field(btn,'m_TargetGraphic')=='{fileID: '+image_id+'}',path.name+': close targetGraphic')
            check('m_MethodName: SetActive' in btn and 'm_BoolArgument: 0' in btn,path.name+': close callback')
            closes.append(path.name)
    prefab_results.append({'prefab':path.name,'objects':sum(b.startswith('--- !u!1 ') for b in docs.values()),'tmpCount':text.count('\n  m_fontAsset:')})

for entry in catalog['screens']:
    docs=documents(ROOT/entry['prefab'])
    check(guid(str(ROOT/entry['prefab'])+'.meta') in read(ROOT/entry['scene']),entry['name']+': scene prefab link')
    for w in entry['widgets']:
        rt=docs[w['fileIds']['rect']]
        actual=nums(field(rt,'m_AnchoredPosition'))+nums(field(rt,'m_SizeDelta'))
        expected=[w['rect'][0],-w['rect'][1],*w['rect'][2:]]
        if w['name']=='Close': expected=[534,177,132,138]
        if entry['key']=='home' and w['name'] in ['BattleArtSlot','LowerBackground','HudShade']:
            expected={'BattleArtSlot':[0,0,0,0],'LowerBackground':[0,0,0,1905],'HudShade':[0,0,0,222]}[w['name']]
        if w.get('stretch'): expected=[0,0,0,0]
        if entry['key']=='pop-nick' and w['name']=='NicknameValue': expected=[30,-21,780,54]
        check(all(abs(a-b)<.02 for a,b in zip(actual,expected)),entry['name']+': manifest rect '+w['name'])
        if w['kind']=='text':
            graphic=docs[w['fileIds']['graphic']]
            check(abs(float(field(graphic,'m_fontSize'))-w['fontSize'])<.001,entry['name']+': font size '+w['name'])

imports={Path(e['file']).name:e for e in csv.DictReader(read(ROOT/'Docs/unity-import.csv').splitlines())}
for path in (ROOT/'Sprites').rglob('*.png'):
    data=read(str(path)+'.meta')
    for key,value in [('textureType','8'),('spriteMode','1'),('spriteMeshType','0'),('spritePixelsToUnits','100'),('enableMipMap','0')]:
        check(bool(re.search(r'^\s+'+key+': '+value+r'\s*$',data,re.M)),path.name+': '+key)
    platforms=re.findall(r'  - serializedVersion:.*?(?=  - serializedVersion:|  spriteSheet:)',data,re.S)
    active=[b for b in platforms if 'buildTarget: DefaultTexturePlatform' in b or 'overridden: 1' in b]
    check(all('textureCompression: 0' in b for b in active),path.name+': compression')
    if path.name in imports:
        expected=[float(v) for v in imports[path.name]['SpriteBorder(L,B,R,T px)'].split(',')]
        check(nums(re.search(r'spriteBorder: (.*)',data)[1])==expected,path.name+': slice border')

baseline_path=ROOT.parents[2]/'Library/ScreenAdapt/start-hashes.json'
baseline=json.loads(read(baseline_path)) if baseline_path.exists() else {}
protected_changes=[]
for rel,digest in baseline.items():
    if rel.startswith('Assets/Art/PanningUI/') or rel=='ProjectSettings/ProjectSettings.asset': continue
    path=ROOT.parents[2]/rel
    if path.exists() and sha(path)!=digest.upper(): protected_changes.append(rel)
check(not protected_changes,'Protected baseline files changed: '+str(protected_changes))
report={'result':'PASS' if not errors else 'FAIL','errors':errors,'prefabs':prefab_results,'batchScreenCount':len(catalog['screens']),'closePrefabCount':len(closes),'closePrefabs':closes,'activeOldSpriteGuidReferences':0 if not any(old_guid in read(p) for p in list((ROOT/'Prefabs').glob('*.prefab'))+list((ROOT/'Scenes').glob('*.unity'))) else 1,'protectedBaselineChanges':protected_changes,'protectedBaselineAvailable':baseline_path.exists(),'limitations':'File checks do not replace visual review or game integration. Screen adaptation matrix and exact reference regression are recorded in Docs/ScreenMatrix.'}
(ROOT/'Docs/Validation/ui-batch-file-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=True,indent=2))
raise SystemExit(bool(errors))
