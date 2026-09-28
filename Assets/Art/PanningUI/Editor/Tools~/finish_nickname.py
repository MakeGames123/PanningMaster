"""Wire the local TMP input field without opening Unity or connecting game services."""
from ui_batch import *


def finish():
    meta(ROOT/'Runtime/NicknameDraftView.cs',ROOT/'Runtime/UICircleGraphic.cs.meta')
    p=ROOT/'Docs/ui-batch-manifest.json';catalog=json.loads(read(p))
    entry=next(e for e in catalog['screens'] if e['key']=='pop-nick')
    prefab=ROOT/entry['prefab'];docs=documents(prefab)
    controller_guid=guid(ROOT/'Runtime/NicknameDraftView.cs.meta')
    if controller_guid in read(prefab):return
    widgets={w['name']:w for w in entry['widgets']}
    inp=widgets['NicknameInput']['fileIds'];value=widgets['NicknameValue']['fileIds']
    input_id='8300000000000000001';controller_id='8300000000000000002'
    def append_component(go,cid):
        docs[go]=docs[go].replace('  m_Layer:',f'  - component: {{fileID: {cid}}}\n  m_Layer:')
    def header(ident,go,script):
        return f'--- !u!114 &{ident}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {go}}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: {script}, type: 3}}\n  m_Name: \n  m_EditorClassIdentifier: \n'
    append_component(inp['gameObject'],input_id)
    # Reuse selectable serialization for navigation/colors, then append TMP input-specific fields.
    b=header(input_id,inp['gameObject'],'2da0c512f12947e489f739169773d7ca')
    template=EMPTY_BUTTON_TEMPLATE
    selectable=template[template.index('  m_Navigation:'):template.index('  m_OnClick:')]
    selectable=setf(selectable,'m_TargetGraphic','{fileID: '+inp['graphic']+'}')
    b+=selectable
    b+=f'  m_TextViewport: {{fileID: {inp["rect"]}}}\n  m_TextComponent: {{fileID: {value["graphic"]}}}\n  m_Placeholder: {{fileID: 0}}\n  m_VerticalScrollbar: {{fileID: 0}}\n'
    values={'m_ScrollSensitivity':1,'m_ContentType':0,'m_InputType':0,'m_AsteriskChar':42,'m_KeyboardType':0,'m_LineType':0,'m_HideMobileInput':0,'m_HideSoftKeyboard':0,'m_CharacterValidation':0,'m_GlobalPointSize':36,'m_CharacterLimit':10,'m_CaretBlinkRate':.85,'m_CaretWidth':1,'m_ReadOnly':0,'m_RichText':0,'m_OnFocusSelectAll':1,'m_ResetOnDeActivation':1,'m_RestoreOriginalTextOnEscape':1,'m_isRichTextEditingAllowed':0,'m_LineLimit':0,'m_ShouldActivateOnSelect':1}
    for k,v in values.items():b+=f'  {k}: {v}\n'
    b+='  m_Text: '+json.dumps('명사수')+'\n'
    b+='  m_CaretColor: {r: 0.95, g: 0.9, b: 0.8, a: 1}\n  m_CustomCaretColor: 1\n  m_SelectionColor: {r: 0.84, g: 0.67, b: 0.38, a: 0.5}\n'
    b+='  m_GlobalFontAsset: {fileID: 11400000, guid: '+guid(ROOT/'Fonts/SDF/Pretendard-SemiBold SDF.asset.meta')+', type: 2}\n'
    for e in ['m_OnEndEdit','m_OnSubmit','m_OnSelect','m_OnDeselect','m_OnTextSelection','m_OnEndTextSelection','m_OnValueChanged','m_OnTouchScreenKeyboardStatusChanged']:b+='  '+e+':\n    m_PersistentCalls:\n      m_Calls: []\n'
    docs[input_id]=b
    # TMP_InputField expects its text inside its viewport. Keep the same absolute rect.
    rt=docs[value['rect']];old_parent=re.search(r'm_Father: \{fileID: (\d+)\}',rt)[1]
    docs[old_parent]=docs[old_parent].replace('  - {fileID: '+value['rect']+'}\n','')
    docs[inp['rect']]=docs[inp['rect']].replace('  m_Children: []','  m_Children:\n  - {fileID: '+value['rect']+'}')
    rt=setf(rt,'m_Father','{fileID: '+inp['rect']+'}');rt=setf(rt,'m_AnchoredPosition','{x: 30, y: -21}')
    docs[value['rect']]=rt
    widgets['NicknameValue']['parent']='NicknameInput';widgets['NicknameValue']['rect']=[30,21,780,54]
    append_component(entry['rootId'],controller_id)
    b=header(controller_id,entry['rootId'],controller_guid)
    for name,ident in [('input',input_id),('count',widgets['Text_01_pop_SB_10_accentGold']['fileIds']['graphic']),('save',widgets['Save']['fileIds']['button']),('saveLabel',widgets['Text_05_pop_SB_14_off_sh']['fileIds']['graphic']),('costLabel',widgets['Text_06_pop_SB_10_off2']['fileIds']['graphic'])]:b+=f'  {name}: {{fileID: {ident}}}\n'
    for name,asset in [('enabledSprite','ui_pop_btn_main'),('disabledSprite','ui_pop_btn_main_off')]:b+=f'  {name}: {{fileID: 21300000, guid: {guid(str(SPRITES[asset])+".meta")}, type: 3}}\n'
    for name,asset in [('enabledMaterial','MAT_BtnGoldText'),('disabledMaterial','MAT_Shadow1')]:b+=f'  {name}: {{fileID: 2100000, guid: {guid(ROOT/f"Fonts/SDF/{asset}.mat.meta")}, type: 2}}\n'
    docs[controller_id]=b
    prefab.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+''.join(docs.values()),encoding='utf-8')
    entry['localInput']={'input':input_id,'draftController':controller_id,'characterLimit':10,'initialValue':'명사수','savePersistence':False}
    p.write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')


if __name__=='__main__':finish();print('Prepared TMP nickname input and local draft feedback; persistence remains unconnected.')
