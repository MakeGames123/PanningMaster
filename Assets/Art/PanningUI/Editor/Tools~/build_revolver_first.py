"""Offline prefab assembly from existing Unity-serialized UI component templates.

Does not contact or operate Unity. Game View validation remains deferred.
"""
from pathlib import Path
import hashlib
import json
import re
import uuid

ROOT = Path(__file__).resolve().parents[2]


def guid(path):
    return re.search(r'^guid: (\w+)', path.read_text(encoding='utf-8-sig'), re.M)[1]


def documents(path):
    result = {}
    for block in re.split(r'(?=^--- !u!)', path.read_text(encoding='utf-8-sig'), flags=re.M):
        match = re.match(r'--- !u!(\d+) &(\d+)', block)
        if match:
            result[match[2]] = block
    return result


def objects(docs):
    return [block for block in docs.values() if block.startswith('--- !u!1 ')]


def name(block):
    return re.search(r'^  m_Name: (.*)$', block, re.M)[1]


def component_ids(game_object):
    return re.findall(r'component: \{fileID: (\d+)\}', game_object)


def object_id(block):
    return re.match(r'--- !u!\d+ &(\d+)', block)[1]


def set_field(block, field, value):
    return re.sub(r'^  ' + re.escape(field) + r': .*$', lambda _: f'  {field}: {value}', block, flags=re.M)


def color_value(value):
    hex_value, _, alpha = value.partition('@')
    rgb = [int(hex_value[i:i + 2], 16) for i in (1, 3, 5)]
    a = float(alpha) if alpha else 1
    return '{r: %.9f, g: %.9f, b: %.9f, a: %.9f}' % (*[v / 255 for v in rgb], a), rgb[0] | rgb[1] << 8 | rgb[2] << 16 | round(a * 255) << 24


up = documents(ROOT / 'Prefabs/PopupRevolverCompare.prefab')
equal = documents(ROOT / 'Prefabs/PopupRevolverCompareEqual.prefab')
up_objects = objects(up)
equal_objects = objects(equal)
data = json.loads((ROOT / 'Docs/text-styles.json').read_text(encoding='utf-8-sig'))
styles = {s['id']: s for s in data['styles']}
specs = data['sceneTexts']['rev-first']
layout = json.loads((ROOT / 'Docs/layout-coordinates.json').read_text(encoding='utf-8-sig'))['scenes']['rev-first']
output = {}
children = []
next_id = 8100000000000000000


def find(docs, object_name):
    return next(g for g in objects(docs) if name(g) == object_name)


def keep_group(g):
    for key in [object_id(g)] + component_ids(g):
        output[key] = up[key]


root = find(up, 'PopupRevolverCompare')
design = find(up, 'Design1200x2601')
for g in [root, design, find(up, 'Dim90')]:
    keep_group(g)
output[object_id(root)] = set_field(root, 'm_Name', 'PopupRevolverFirstDraw')
design_rect = component_ids(design)[0]


def clone(docs, template, new_name, rect):
    global next_id
    old_ids = [object_id(template)] + component_ids(template)
    remap = {}
    for key in old_ids:
        next_id += 1
        remap[key] = str(next_id)
    group = []
    for old in old_ids:
        b = docs[old]
        for before, after in remap.items():
            b = re.sub(r'(?<!\d)' + before + r'(?!\d)', after, b)
        if b.startswith('--- !u!1 '):
            b = set_field(b, 'm_Name', new_name)
        if b.startswith('--- !u!224 '):
            b = set_field(b, 'm_Father', '{fileID: ' + design_rect + '}')
            x, y, w, h = rect
            b = set_field(b, 'm_AnchoredPosition', f'{{x: {x}, y: {-y}}}')
            b = set_field(b, 'm_SizeDelta', f'{{x: {w}, y: {h}}}')
            children.append(remap[old])
        output[remap[old]] = b
        group.append(remap[old])
    return group


def css(rect, pad=0):
    x, y, w, h = rect
    return [(x-pad)*3, (y-pad)*3, (w+pad*2)*3, (h+pad*2)*3]


def clone_named(template, new_name, rect, pad=0, docs=up):
    return clone(docs, find(docs, template), new_name, css(rect, pad))


def icon(icon_name, x, y, size):
    # All icon templates are intentionally empty transparent Images.
    clone_named(icon_name, icon_name, [x, y, size, size])


for e in layout:
    sel, i, r = e['sel'], e['i'], e['css400']
    if sel == '.rgc':
        clone_named('EquippedCard', 'EquippedCard', r, 6)
    elif sel == '.rgc .hx':
        clone_named('EquippedHero', 'EquippedHero', r)
        clone_named('EquippedWeaponArt', 'EquippedWeaponArt', [r[0]+(r[2]-106)/2, r[1]+20, 106, 64])
    elif sel == '.rgc .hx .g':
        clone_named('Grade_0', 'Grade_0', r, 14)
    elif sel == '.rgc .hx .tag':
        clone_named('Tag_0', 'Tag_0', r, 2)
    elif sel == '.rgc .hx .lv':
        clone_named('Level_0', 'Level_0', r, 2)
    elif sel == '.rgc .n':
        clone_named('NameDivider_0', 'NameDivider_0', [r[0], r[1]+r[3]-2, r[2], 1])
    elif sel == '.rgc .pw':
        icon('ico_power', 227, r[1]+6, 18)
    elif sel == '.rgc .k9-lb':
        clone_named('EquippedEffectsBand_0', 'EquippedEffectsBand_0', r)
    elif sel == '.rgc .st':
        ids = ['ico_attr_fire', 'ico_power', 'ico_crit', 'ico_critdmg', 'ico_attr_fire', 'ico_time']
        icon(ids[i], 271.3 if i == 0 else r[0]+4, r[1]+(r[3]-12)/2, 12)
        clone_named('StatDivider_0', f'StatDivider_{i}', [r[0], r[1]+r[3]-1, r[2], 1])
    elif sel == '.dk-chip':
        clone_named('CrateStock', 'CrateStock', r, 3)
        icon('ico_crate', 28, 708.6, 18)
    elif sel == '.dk-cta':
        clone_named('Draw', 'Draw', r, 3, equal)
        icon('ico_crate', 207, 734.7, 12)
    elif sel == '.dk-x':
        clone_named('CloseLabelButton', 'CloseLabelButton', r)
        icon('ico_close_s', r[0], r[1]+3, 8)

for index, spec in enumerate(specs):
    st = styles[spec['style']]
    template = next((g for g in up_objects if name(g).endswith('_' + spec['style'])), None)
    docs = up
    if template is None:
        template = next(g for g in equal_objects if name(g).endswith('_' + spec['style']))
        docs = equal
    group = clone(docs, template, f'Text_{index:02d}_{spec["style"]}', spec['rectUnity1200'])
    for key in group:
        b = output[key]
        if not b.startswith('--- !u!114 '):
            continue
        text = spec['text'].replace('\u2715 ', '')
        b = re.sub(r'  m_text: .*?\n  m_isRightToLeft:', lambda _: '  m_text: ' + json.dumps(text, ensure_ascii=True) + '\n  m_isRightToLeft:', b, flags=re.S)
        b = set_field(b, 'm_HorizontalAlignment', {'center': '2', 'right': '4'}.get(spec['align'], '1'))
        b = set_field(b, 'm_VerticalAlignment', '512' if spec['align'] == 'center' else '4096')
        b = set_field(b, 'm_margin', '{x: 36, y: 0, z: 0, w: 0}' if spec['text'].startswith('\u2715 ') else '{x: 0, y: 0, z: 0, w: 0}')
        color, packed = color_value(st['color'])
        b = set_field(b, 'm_fontColor', color)
        b = re.sub(r'(m_fontColor32:\n    serializedVersion: 2\n    rgba: )\d+', lambda m: m[1] + str(packed), b)
        output[key] = b

clone_named('Close', 'Close', [178, 808, 44, 46])
output[design_rect] = re.sub(r'  m_Children:.*?\n  m_Father:', '  m_Children:\n' + ''.join('  - {fileID: '+k+'}\n' for k in children) + '  m_Father:', output[design_rect], flags=re.S)

path = ROOT / 'Prefabs/PopupRevolverFirstDraw.prefab'
assert not path.exists(), 'Refusing to overwrite an existing first-draw prefab'
path.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n' + ''.join(output.values()), encoding='utf-8')
new_guid = uuid.uuid4().hex
meta = (ROOT / 'Prefabs/PopupRevolverCompare.prefab.meta').read_text()
path.with_suffix('.prefab.meta').write_text(re.sub(r'^guid: \w+', 'guid: '+new_guid, meta, flags=re.M), encoding='utf-8')
scene_path = ROOT / 'Scenes/RevolverFirstDrawPreview.unity'
scene = (ROOT / 'Scenes/RevolverComparePreview.unity').read_text()
scene = scene.replace(guid(ROOT / 'Prefabs/PopupRevolverCompare.prefab.meta'), new_guid).replace('PopupRevolverCompare', 'PopupRevolverFirstDraw').replace('Display 8', 'Display 7').replace('m_TargetDisplay: 7', 'm_TargetDisplay: 6')
scene = scene.replace('propertyPath: m_TargetDisplay\n      value: 7', 'propertyPath: m_TargetDisplay\n      value: 6')
assert not scene_path.exists()
scene_path.write_text(scene, encoding='utf-8')
scene_path.with_suffix('.unity.meta').write_text(re.sub(r'^guid: \w+', 'guid: '+uuid.uuid4().hex, (ROOT / 'Scenes/RevolverComparePreview.unity.meta').read_text(), flags=re.M), encoding='utf-8')
print(f'Prepared {path.name}: {len(specs)} TMP texts, 10 empty icons, one weapon-art slot. No Unity operation performed.')
