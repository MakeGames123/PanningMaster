"""File-only Unity UI assembly. Never starts or sends commands to the Editor.

Uses Unity-serialized component templates already present in this project's UI.
All generated objects and their source coordinates are recorded in a review manifest.
"""
from pathlib import Path
import json
import re
import uuid
import csv

ROOT = Path(__file__).resolve().parents[2]


def read(path):
    return Path(path).read_text(encoding='utf-8-sig')


def guid(path):
    return re.search(r'^guid: (\w+)', read(path), re.M)[1]


def meta(path, template=None):
    p = Path(str(path) + '.meta')
    if not p.exists():
        value = uuid.uuid4().hex
        s = read(template) if template else 'fileFormatVersion: 2\nguid: GUID\n'
        s = re.sub(r'^guid: .*', 'guid: ' + value, s, flags=re.M)
        p.write_text(s, encoding='utf-8')
    return guid(p)


def documents(path):
    return {re.match(r'--- !u!\d+ &(-?\d+)', b)[1]: b for b in
            re.split(r'(?=^--- !u!)', read(path), flags=re.M) if b.startswith('--- !u!')}


def field(block, key):
    return re.search(r'^  ' + re.escape(key) + r': (.*)$', block, re.M)[1]


def setf(block, key, value):
    return re.sub(r'^  ' + re.escape(key) + r': .*$', lambda _: f'  {key}: {value}', block, flags=re.M)


def oid(block):
    return re.match(r'--- !u!\d+ &(-?\d+)', block)[1]


def components(block):
    return re.findall(r'component: \{fileID: (-?\d+)\}', block)


def color(value):
    h, _, a = value.partition('@')
    rgb = [int(h[i:i+2], 16) for i in (1, 3, 5)]
    alpha = float(a) if a else 1
    return '{r: %.9f, g: %.9f, b: %.9f, a: %.9f}' % (*[v/255 for v in rgb], alpha), rgb[0] | rgb[1]<<8 | rgb[2]<<16 | round(alpha*255)<<24


DATA = json.loads(read(ROOT/'Docs/text-styles.json'))
STYLES = {s['id']: s for s in DATA['styles']}
LAYOUT = json.loads(read(ROOT/'Docs/layout-coordinates.json'))
MANIFEST = {Path(e['file']).stem:e for e in json.loads(read(ROOT/'Docs/unity-png-manifest.json'))}
SPRITES = {p.stem:p for p in (ROOT/'Sprites').rglob('*.png')}
IMPORTS = {Path(e['file']).stem:e for e in csv.DictReader(read(ROOT/'Docs/unity-import.csv').splitlines())}
TEMPLATE = documents(ROOT/'Prefabs/PopupRevolverCompare.prefab')
OBJECTS = {field(b,'m_Name'):b for b in TEMPLATE.values() if b.startswith('--- !u!1 ')}
TMP_TEMPLATE = next(b for b in TEMPLATE.values() if '\n  m_text:' in b)
IMAGE_TEMPLATE = next(TEMPLATE[c] for c in components(OBJECTS['EquippedHero']) if '\n  m_Sprite:' in TEMPLATE[c])
RENDERER_TEMPLATE = next(b for b in TEMPLATE.values() if b.startswith('--- !u!222 '))
RECT_TEMPLATE = TEMPLATE[components(OBJECTS['EquippedHero'])[0]]
BUTTON_TEMPLATE = next(TEMPLATE[c] for c in components(OBJECTS['Close']) if '\n  m_OnClick:' in TEMPLATE[c])
EMPTY_BUTTON_TEMPLATE = next(TEMPLATE[c] for c in components(OBJECTS['EquippedCard']) if '\n  m_OnClick:' in TEMPLATE[c])
CIRCLE_TEMPLATE = next(TEMPLATE[c] for c in components(OBJECTS['InfoCircle']) if TEMPLATE[c].startswith('--- !u!114 '))


class Screen:
    def __init__(self, key, name, dim=True):
        self.key, self.name, self.dim = key, name, dim
        self.widgets = []
        self.notes = []
        self.input = None

    def add(self, name, rect, kind='image', **kwargs):
        w = dict(id=f'W{len(self.widgets):03d}', name=name, kind=kind,
                 rect=[round(float(v)*3, 4) for v in rect], parent='', **kwargs)
        self.widgets.append(w)
        return w

    def image(self, name, sprite, rect, pad=None, **kwargs):
        if pad is None: pad=MANIFEST.get(sprite,{}).get('pad',0)
        x,y,w,h=rect
        return self.add(name,[x-pad,y-pad,w+2*pad,h+2*pad],sprite=sprite,**kwargs)

    def solid(self, name, rect, tint, **kwargs):
        return self.add(name,rect,color=tint,**kwargs)

    def icon(self, name, x, y, size=12):
        return self.solid(name,[x,y,size,size],'#ffffff@0')

    def text(self, index, spec=None):
        spec = spec or DATA['sceneTexts'][self.key][index]
        style = STYLES[spec['style']]
        raw = spec['text']; value = raw.replace('\u2715 ','').replace('\u2713 ','')
        if value in ['\u25be','\u27a4']: value=''
        w=self.add(f'Text_{index:02d}_{spec["style"]}',spec['rectUnity1200'],kind='text',text=value,style=spec['style'],font=style['fontAsset'],fontSize=style['unityFontSize'],color=style['color'],spacing=style['characterSpacing'],italic=style['italic'],material=style['material'],align=spec['align'],margin=36 if raw.startswith(('\u2715 ','\u2713 ')) else 0,sourceIndex=index)
        # Already Unity coordinates, unlike image layout coordinates.
        w['rect']=list(spec['rectUnity1200'])
        if raw=='\u25be' or raw=='\u27a4':
            x,y,width,height=spec['rect400'];self.icon('ico_fold' if raw=='\u25be' else 'ico_arrow_r',x,y,min(width,height))
        if raw.startswith('\u2713 '):
            x,y,_,_=spec['rect400'];self.icon('ico_check',x,y,10)
        return w

    def texts(self, indices=None):
        for i in indices if indices is not None else range(len(DATA['sceneTexts'][self.key])):self.text(i)

    def close(self):
        self.image('Close','ui_nav_close_t01a',[178,808,44,46],0,button='close')


def build(screen):
    path=ROOT/f'Prefabs/{screen.name}.prefab'
    if path.exists(): raise RuntimeError(f'Refusing to overwrite {path.name}')
    output={};next_id=8200000000000000000
    def alloc():
        nonlocal next_id
        next_id+=1
        return str(next_id)
    def keep(name):
        g=OBJECTS[name]
        for i in [oid(g)]+components(g):output[i]=TEMPLATE[i]
        return oid(g),components(g)[0]
    root_id,root_rect=keep('PopupRevolverCompare')
    # Structural template is produced by ScreenAdaptBuilder; preserve its serialized component links.
    if 'PopupBody' not in OBJECTS:
        raise RuntimeError('Run Tools/Panning UI/Build Screen Adapt Previews before file-only generation')
    body_id,design_rect=keep('PopupBody')
    structural=['FullBleed','PillarFill','Left','Right','LeftHairline','RightHairline','SafeArea','Column','Top','Middle','Bottom']
    structure={name:keep(name) for name in structural}
    bottom_rect=structure['Bottom'][1] if structure else design_rect
    dim_id,dim_rect=keep('Dim90')
    output[root_id]=setf(output[root_id],'m_Name',screen.name)
    output[root_rect]=setf(output[root_rect],'m_LocalScale','{x: 1, y: 1, z: 1}')
    output[dim_id]=setf(output[dim_id],'m_IsActive','1' if screen.dim else '0')
    children={design_rect:[]}
    children[bottom_rect]=[]
    ids={w['id']:(alloc(),alloc()) for w in screen.widgets}
    ids_by_name={w['name']:ids[w['id']] for w in screen.widgets}
    def new_block(template, ident, go):
        b=re.sub(r'^(--- !u!\d+ &)-?\d+',lambda m:m[1]+ident,template)
        return setf(b,'m_GameObject','{fileID: '+go+'}')
    for w in screen.widgets:
        go,rt=ids[w['id']];parent=ids_by_name[w['parent']][1] if w.get('parent') else (bottom_rect if w['name']=='Close' else design_rect)
        children.setdefault(parent,[]).append(rt);children.setdefault(rt,[])
        cids=[rt];x,y,width,height=w['rect']
        rect=new_block(RECT_TEMPLATE,rt,go)
        for k,v in dict(m_Father='{fileID: '+parent+'}',m_AnchoredPosition=f'{{x: {x}, y: {-y}}}',m_SizeDelta=f'{{x: {width}, y: {height}}}',m_LocalScale='{x: 1, y: 1, z: 1}',m_LocalPosition='{x: 0, y: 0, z: 0}').items():rect=setf(rect,k,v)
        if w['name']=='Close' and structure:
            rect=setf(rect,'m_AnchorMin','{x: 0, y: 0}');rect=setf(rect,'m_AnchorMax','{x: 0, y: 0}')
            rect=setf(rect,'m_AnchoredPosition',f'{{x: {x}, y: {2601-y}}}')
        if w.get('stretch'):
            for key,value in {'m_AnchorMin':'{x: 0, y: 0}','m_AnchorMax':'{x: 1, y: 1}','m_Pivot':'{x: 0.5, y: 0.5}','m_AnchoredPosition':'{x: 0, y: 0}','m_SizeDelta':'{x: 0, y: 0}'}.items():rect=setf(rect,key,value)
        output[rt]=rect
        render=alloc();cids.append(render);output[render]=new_block(RENDERER_TEMPLATE,render,go)
        graphic=alloc();cids.append(graphic)
        if w['kind']=='text':
            b=new_block(TMP_TEMPLATE,graphic,go)
            font_path=ROOT/f'Fonts/SDF/{w["font"]}.asset';fg=guid(str(font_path)+'.meta')
            b=re.sub(r'  m_text: .*?\n  m_isRightToLeft:',lambda _: '  m_text: '+json.dumps(w['text'],ensure_ascii=True)+'\n  m_isRightToLeft:',b,flags=re.S)
            mat=w.get('material','default')
            if mat=='default':
                mid=re.search(r'^--- !u!21 &(-?\d+)',read(font_path),re.M)[1]
                matref=f'{{fileID: {mid}, guid: {fg}, type: 2}}'
            else:
                if w['font'].startswith('Gmarket'):
                    mat={'MAT_Shadow1':'MAT_GmarketBold_Shadow1','MAT_BtnGoldText':'MAT_GmarketBold_BtnGoldText'}.get(mat,mat)
                mp=ROOT/f'Fonts/SDF/{mat}.mat'
                matref=f'{{fileID: 2100000, guid: {guid(str(mp)+".meta")}, type: 2}}'
            values=dict(m_fontAsset=f'{{fileID: 11400000, guid: {fg}, type: 2}}',m_sharedMaterial=matref,m_fontSize=w['fontSize'],m_fontSizeBase=w['fontSize'],m_fontStyle=2 if w.get('italic') else 0,m_fontWeight=400,m_characterSpacing=w.get('spacing',0),m_enableAutoSizing=0,m_HorizontalAlignment={'center':2,'right':4}.get(w.get('align'),1),m_VerticalAlignment=512 if w.get('align')=='center' else 4096,m_margin='{x: '+str(w.get('margin',0))+', y: 0, z: 0, w: 0}',m_RaycastTarget=0,m_isRichText=0,m_textWrappingMode=1 if w.get('wrap') else 0)
            for k,v in values.items():b=setf(b,k,str(v))
            co,packed=color(w.get('color','#ffffff'));b=setf(b,'m_fontColor',co)
            b=re.sub(r'(m_fontColor32:\n    serializedVersion: 2\n    rgba: )\d+',lambda m:m[1]+str(packed),b)
        else:
            b=new_block(CIRCLE_TEMPLATE if w['kind'] in ['circle','panel'] else IMAGE_TEMPLATE,graphic,go)
            b=setf(b,'m_Color',color(w.get('color','#ffffff'))[0]);b=setf(b,'m_RaycastTarget','1' if w.get('button') or w.get('block') else '0')
            b=setf(b,'m_Material','{fileID: 0}')
            if w.get('sprite'):
                sprite=SPRITES[w['sprite']]
                b=setf(b,'m_Sprite',f'{{fileID: 21300000, guid: {guid(str(sprite)+".meta")}, type: 3}}')
                border=re.search(r'^  spriteBorder: (.*)$',read(str(sprite)+'.meta'),re.M)[1]
                sliced=any(float(v)!=0 for v in re.findall(r': ([\d.]+)',border))
                b=setf(b,'m_Type','1' if sliced else '0')
                if w.get('preserveAspect'):b=setf(b,'m_PreserveAspect','1')
            elif w['kind'] not in ['circle','panel']:b=setf(b,'m_Sprite','{fileID: 0}');b=setf(b,'m_Type','0')
            if w['kind']=='panel':
                b=setf(b,'m_Script','{fileID: 11500000, guid: '+guid(ROOT/'Runtime/UIRoundedPanel.cs.meta')+', type: 3}')
                b+='  radius: '+str(w.get('radius',0)*3)+'\n  borderWidth: '+str(w.get('borderWidth',0)*3)+'\n  bottomColor: '+color(w.get('bottomColor',w.get('color','#ffffff')))[0]+'\n  borderColor: '+color(w.get('borderColor','#ffffff@0'))[0]+'\n'
        if w.get('tone'):
            mat='MAT_'+w['font'].replace(' ','_')+'_CompareDown' if w['kind']=='text' else 'MAT_UI_CompareDown'
            matpath=ROOT/f'Materials/{mat}.mat'
            b=setf(b,'m_sharedMaterial' if w['kind']=='text' else 'm_Material',f'{{fileID: 2100000, guid: {guid(str(matpath)+".meta")}, type: 2}}')
        output[graphic]=b
        if w.get('mask'):
            mi=alloc();cids.append(mi)
            mb=new_block(CIRCLE_TEMPLATE,mi,go)
            mb=setf(mb,'m_Script','{fileID: 11500000, guid: 3312d7739989d2b4e91e6319e9a96d76, type: 3}')
            mb=mb[:mb.index('  m_Material:')]+'  m_Padding: {x: 0, y: 0, z: 0, w: 0}\n  m_Softness: {x: 0, y: 0}\n'
            output[mi]=mb
        if w.get('button'):
            bi=alloc();cids.append(bi)
            closing=w['button'] in ['close','dismiss']
            bb=new_block(BUTTON_TEMPLATE if closing else EMPTY_BUTTON_TEMPLATE,bi,go)
            bb=setf(bb,'m_TargetGraphic','{fileID: '+graphic+'}')
            bb=setf(bb,'m_Interactable','0' if w['button']=='disabled' else '1')
            if closing and w.get('closeTarget'):
                bb=bb.replace('{fileID: '+root_id+'}','{fileID: '+ids_by_name[w['closeTarget']][0]+'}')
            output[bi]=bb
        g=new_block(OBJECTS['EquippedHero'],go,go)
        g=setf(g,'m_Name',w['name'])
        g=setf(g,'m_IsActive','0' if w.get('active') is False else '1')
        g=re.sub(r'  m_Component:.*?\n  m_Layer:','  m_Component:\n'+''.join('  - component: {fileID: '+i+'}\n' for i in cids)+'  m_Layer:',g,flags=re.S)
        output[go]=g
        w['layoutParent']='Bottom' if w['name']=='Close' and structure else ('PopupBody/'+w['parent'] if w.get('parent') and structure else ('PopupBody' if structure else 'Design1200x2601'))
        w['fileIds']={'gameObject':go,'rect':rt,'graphic':graphic,'button':bi if w.get('button') else None}
    if screen.key=='home':
        # Match HomeScreenAdaptBuilder without starting Unity. Source widget rects stay intact.
        hud_go,hud_rt=alloc(),alloc()
        output[hud_go]=new_block(output[body_id],hud_go,hud_go)
        output[hud_go]=setf(output[hud_go],'m_Name','TopContent')
        output[hud_go]=output[hud_go].replace('{fileID: '+design_rect+'}','{fileID: '+hud_rt+'}')
        output[hud_rt]=new_block(output[design_rect],hud_rt,hud_go)
        def rect_fields(ident, **fields):
            for key,value in fields.items():output[ident]=setf(output[ident],key,value)
        for ident,parent,edge in [(hud_rt,structure['Top'][1],1),(design_rect,bottom_rect,0)]:
            rect_fields(ident,m_Father='{fileID: '+parent+'}',m_AnchorMin=f'{{x: 0.5, y: {edge}}}',m_AnchorMax=f'{{x: 0.5, y: {edge}}}',m_Pivot=f'{{x: 0.5, y: {edge}}}',m_AnchoredPosition='{x: 0, y: 0}',m_SizeDelta='{x: 1200, y: 2601}')
        output[body_id]=setf(output[body_id],'m_Name','BottomContent')
        children[structure['Top'][1]]=[hud_rt];children[hud_rt]=[]
        children[bottom_rect]=[design_rect];children[structure['Middle'][1]]=[]
        rect_fields(structure['Middle'][1],m_AnchoredPosition='{x: 0, y: 807}',m_SizeDelta='{x: 0, y: -2262}')
        for ident,block in list(output.items()):
            if '\n  popupBody:' in block:output[ident]=setf(block,'popupBody','{fileID: 0}')
        full=structure['FullBleed'][1]
        children[full]=[dim_rect]
        for w in screen.widgets:
            rt=ids[w['id']][1]
            if w.get('parent'):
                w['layoutParent']='Bottom/BottomContent/'+w['parent'];continue
            bg=w['name'] in ['BattleArtSlot','LowerBackground','HudShade']
            parent=full if bg else (hud_rt if w['rect'][1]<400 else design_rect)
            if parent!=design_rect:
                children[design_rect].remove(rt);children[parent].append(rt)
                output[rt]=setf(output[rt],'m_Father','{fileID: '+parent+'}')
            w['layoutParent']='FullBleed' if bg else ('Top/TopContent' if parent==hud_rt else 'Bottom/BottomContent')
            if bg:
                upper=w['name']=='HudShade';battle=w['name']=='BattleArtSlot';edge=1 if upper else 0
                rect_fields(rt,m_AnchorMin=f'{{x: 0, y: {edge}}}',m_AnchorMax=f'{{x: 1, y: {1 if battle else edge}}}',m_Pivot='{x: 0.5, y: 0.5}' if battle else f'{{x: 0, y: {edge}}}',m_AnchoredPosition='{x: 0, y: 0}',m_SizeDelta='{x: 0, y: '+str(0 if battle else (222 if upper else 1905))+'}')
        children[full].append(structure['PillarFill'][1])
    # Secondary modal dim uses the same serialized layout as the Unity migration.
    # Keep its content/control under Column while its dim spans the entire screen.
    layer_name='ReceivedLayer' if screen.key=='pop-mailgot' else ('WarningLayer' if screen.key in ['rev-warn-discard','rev-warn-equip'] else None)
    if layer_name:
        odocs=documents(ROOT/'Prefabs/PopupMailboxReceived.prefab')
        onames={field(b,'m_Name'):b for b in odocs.values() if b.startswith('--- !u!1 ')}
        lg=onames['ReceivedLayer']; dg=onames['ReceivedLayerDim']
        target_go,target_rt=ids_by_name[layer_name]
        layer_widget=next(w for w in screen.widgets if w['name']==layer_name)
        target_graphic=layer_widget['fileIds']['graphic']
        output[target_graphic]=setf(output[target_graphic],'m_Color',color('#000000@0')[0])
        # Copy only the overlay-specific components, not the template's Button/Image or children.
        extra=[cid for cid in components(lg) if odocs[cid].startswith('--- !u!223 ') or 'm_BlockingObjects:' in odocs[cid] or '\n  dim:' in odocs[cid]]
        dim_ids=[oid(dg)]+components(dg)
        mapping={old:alloc() for old in dim_ids+extra}
        mapping[oid(lg)]=target_go
        old_parent=re.search(r'm_Father: \{fileID: (-?\d+)\}',odocs[components(dg)[0]])[1]
        mapping[old_parent]=structure['FullBleed'][1]
        def remap(b):
            b=re.sub(r'^(--- !u!\d+ &)(-?\d+)',lambda m:m[1]+mapping.get(m[2],m[2]),b)
            return re.sub(r'fileID: (-?\d+)',lambda m:'fileID: '+mapping.get(m[1],m[1]),b)
        for old in dim_ids+extra:output[mapping[old]]=remap(odocs[old])
        dim_go=mapping[oid(dg)];dim_rt=mapping[components(dg)[0]]
        output[dim_go]=setf(output[dim_go],'m_Name',layer_name+'Dim')
        for old in components(dg):
            if '\n  m_Color:' in odocs[old]:output[mapping[old]]=setf(output[mapping[old]],'m_Color',color(layer_widget['color'])[0])
        output[target_go]=output[target_go].replace('  m_Layer:', ''.join('  - component: {fileID: '+mapping[cid]+'}\n' for cid in extra)+'  m_Layer:',1)
        full_rect=structure['FullBleed'][1]
        children[full_rect]=re.findall(r'  - \{fileID: (-?\d+)\}',output[full_rect])+[dim_rt]
    for rt,cc in children.items():
        output[rt]=re.sub(r'  m_Children:.*?\n  m_Father:','  m_Children:'+ ('\n'+''.join('  - {fileID: '+i+'}\n' for i in cc) if cc else ' []\n')+'  m_Father:',output[rt],flags=re.S)
    path.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+''.join(output.values()),encoding='utf-8')
    pg=meta(path,ROOT/'Prefabs/PopupRevolverCompare.prefab.meta')
    scene_name=screen.name.removeprefix('Popup').removeprefix('Screen')+'Preview'
    sp=ROOT/f'Scenes/{scene_name}.unity'
    assert not sp.exists()
    scene=read(ROOT/'Scenes/RevolverComparePreview.unity').replace(guid(ROOT/'Prefabs/PopupRevolverCompare.prefab.meta'),pg).replace('PopupRevolverCompare',screen.name).replace('Display 8','Display 7').replace('m_TargetDisplay: 7','m_TargetDisplay: 6').replace('propertyPath: m_TargetDisplay\n      value: 7','propertyPath: m_TargetDisplay\n      value: 6')
    sp.write_text(scene,encoding='utf-8');meta(sp,ROOT/'Scenes/RevolverComparePreview.unity.meta')
    return dict(key=screen.key,name=screen.name,prefab=path.relative_to(ROOT).as_posix(),scene=sp.relative_to(ROOT).as_posix(),display=7,widgets=screen.widgets,notes=screen.notes,rootId=root_id,dim=screen.dim)
