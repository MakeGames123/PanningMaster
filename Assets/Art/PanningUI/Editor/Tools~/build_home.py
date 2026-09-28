"""Home UI draft using recorded local design coordinates, no editor operations."""
from build_remaining_ui import extra_text
from ui_batch import *


def home():
    d=json.loads(read(ROOT/'Docs/home-ui-layout.json'))
    s=Screen('home','ScreenMainHome',False)
    def panel(name,r,top='#241d19',bottom='#17120f',radius=6,border='#d6ac60@0.55',width=1,**kw):
        return s.add(name,r,kind='panel',color=top,bottomColor=bottom,radius=radius,borderColor=border,borderWidth=width,**kw)
    def elements(sel):return [e for e in d['elements'] if e['sel']==sel]
    def rect(sel):return elements(sel)[0]['rect400']
    def pill(name,r,**kw):return panel(name,r,'#0a0705@0.72','#0a0705@0.72',12,'#d6ac60@0.18',1,**kw)
    s.solid('BattleArtSlot',[0,0,400,232],'#ffffff@0')
    panel('LowerBackground',[0,232,400,635],'#3a2a22@0','#2e211b',0,width=0)
    panel('HudShade',rect('#hud'),'#0c0806@0.62','#0c0806@0',0,width=0)
    for sel,name in [('#s-pow','CombatPower'),('#s-rank','Rank'),('#c-gold','Gold'),('#c-ruby','Ruby'),('#menu','Menu'),('#stage','Stage')]:pill(name,rect(sel),button='action' if name in ['CombatPower','Rank','Menu'] else '')
    for name,x,y,size in [('ico_edit',66.5,14,9),('ico_power',18,35,10),('ico_trophy',18,57,10),('ico_gold',203,17,17),('ico_ruby',296,17,17),('ico_fold',380,21,10),('ico_boss',348,83,11)]:s.icon(name,x,y,size)
    extra_text(s,'BossLabel','보스',[366,83,24,12],size=9,tint='#ff8a7a')
    panel('BossHealthTrack',rect('#boss .bar'),'#1a0d0a','#1a0d0a',4)
    panel('BossHealthValue',rect('#boss .bar i'),'#ff8a7a','#7a1a12',4,width=0)
    panel('Cylinder',rect('#cyl'),'#2e251e','#150f0c',132)
    panel('WeaponHub',rect('#hub'),'#2a221c','#140f0c',44)
    r=rect('#hub img');s.solid('EquippedWeaponArt',r,'#ffffff@0')
    for e in elements('.ch'):
        i=e['i'];x,y,w,h=e['rect400'];panel('Chamber_'+str(i),[x,y,w,h],'#221a15','#0f0b09',37,e['elementColor'],3,button='action')
        s.icon('ico_bullet',x+18,y+11,38)
    for sel in ['.ch .no','.ch .lv']:
        for e in elements(sel):
            if sel.endswith('.no'):panel('ChamberNumber_'+str(e['i']),e['rect400'],'#fff0b0','#9a6a1c',10,'#201408',1)
            else:pill('ChamberLevel_'+str(e['i']),e['rect400'])
    pill('Chat',rect('#chat'),button='action');s.icon('ico_chat',16,502,17)
    panel('AutoFormation',rect('#auto-f'),button='action');s.icon('ico_power',20,535,12)
    panel('Quest',rect('#quest'),button='action')
    panel('QuestReward',rect('#quest .ico'),'#2a221c','#140f0c',7,'#4aa3ff',2)
    s.icon('ico_gem',228,508,18)
    panel('QuestProgressTrack',rect('#quest .pg'),'#0c0907','#0c0907',2,width=0)
    panel('QuestProgress',rect('#quest .pg i'),'#e8b44a','#9a6a1c',2,width=0)
    # Reserve inventory as a separate clipped view. The third source row is intentionally clipped.
    panel('Inventory',rect('#inv'))
    inv_start=len(s.widgets);s.solid('InventoryViewport',rect('#inv'),'#ffffff@0',mask=True)
    for e in elements('.sl'):
        i=e['i'];x,y,w,h=e['rect400'];panel('InventorySlot_'+str(i),[x,y,w,h],'#2a221c','#140f0c',7,e['tier'],2,button='action')
        s.icon('ico_bullet',x+20,y+14,20)
    for e in elements('.sl .g'):
        grade=e['grade'];n=['E','D','C','B','A','S','SS','SSS','SR','SSR','UR','LR','MR'].index(grade)+1
        x,y,w,h=e['rect400']
        # Grade PNG includes 14px padding around a 42px letter; preserve that ratio at 15px.
        s.image('InventoryGrade_'+str(e['i']),f'ui_rev_grade_{n:02d}_{grade}',[x,y,w,h],5)
    for e in elements('.sl .lv'):pill('InventoryLevel_'+str(e['i']),e['rect400'])
    for e in elements('.sl .eq'):panel('EquippedNumber_'+str(e['i']),e['rect400'],'#fff0b0','#9a6a1c',8,'#201408',1)
    for e in elements('.sl .nw'):s.add('NewItemDot_'+str(e['i']),e['rect400'],kind='circle',color='#ff4f3a')
    for w in s.widgets[inv_start+1:]:
        w['parent']='InventoryViewport';w['rect'][0]-=24;w['rect'][1]-=570*3
    for e in elements('.tb'):
        i=e['i'];panel('DrawCount_'+str(i),e['rect400'],'#e8c878' if i==0 else '#0c0907','#c79d48' if i==0 else '#0c0907',3,button='action')
    panel('DrawLevelTrack',rect('#dlv .pg'),'#0c0907','#0c0907',3,width=0)
    panel('DrawLevelProgress',rect('#dlv .pg i'),'#fff0b0','#9a6a1c',3,width=0)
    pill('DrawRates',rect('#dlv .info'),button='action')
    s.image('AutoDraw','ui_rev_crate_chip',rect('#chip'),button='action');s.icon('ico_gear',29,756,16)
    s.image('Draw','ui_rev_btn_gold',rect('#draw'),button='action')
    s.icon('ico_crate',173,782,10)
    extra_text(s,'DrawCost','1 · 보유 140/200',[188,781,98,12],font='GmarketSans-Medium SDF',size=9.5,tint='#5a3a10')
    panel('Navigation',rect('#nav'),'#1c1612','#120e0b',0)
    nav=['character','growth','dungeon','weapon',None,'shop']
    for e in elements('.nv'):
        i=e['i'];x,y,w,h=e['rect400'];s.solid('NavButton_'+str(i),[x,y,w,h],'#ffffff@0',button='action' if i!=4 else 'disabled')
        if i==3:panel('SelectedNav',[x+w*.18,y,w*.64,3],'#fff0b0','#9a6a1c',0,width=0)
        if nav[i]:
            import struct
            file=SPRITES['ui_nav_'+nav[i]];pw,ph=struct.unpack('>II',file.read_bytes()[16:24]);height=32 if i==3 else 34;width=height*pw/ph
            s.image('NavIcon_'+nav[i],'ui_nav_'+nav[i],[x+(w-width)/2,819 if i==3 else 821,width,height],0)
        else:s.icon('ico_lock',x+(w-28)/2,824,28)
    for e in elements('.dot'):s.add('NavNotification_'+str(e['i']),e['rect400'],kind='circle',color='#ff4f3a')
    for i,t in enumerate(d['texts']):
        if t['text']=='✎':continue
        # Higher-level source metadata tracks typography independently of popup styles.
        w=extra_text(s,f'HomeText_{i:02d}',t['text'],t['rect400'],font=t['fontAsset'],size=t['unityFontSize']/3,tint=t['color'],align=t['align'],spacing=t['characterSpacing'],italic=t['italic'],homeSourceIndex=i)
        if t['parent']=='inventory':w['parent']='InventoryViewport';w['rect'][0]-=24;w['rect'][1]-=1710
    s.notes += ['Home source is the prior home-skin-r01 proposal; all source paths and hashes are recorded in home-ui-layout.json. Final visual approval remains pending.', 'Home has no supplied sceneTexts entry. Text rectangles were extracted headlessly with local Gmarket fonts; no user browser/desktop was controlled.', 'World/battle and weapon art remain empty integration slots. Existing game scenes are untouched. Native TMP labels, separate icons, clipped inventory, six chamber views; no baked text or sheen.', 'Current button bible replaces old hexagonal draw buttons; current icon placeholders replace all source emojis.']
    # User correction 2026-09-28: reserve physical centre slot for contextual X.
    cell=1200/7
    slots={'NavIcon_character':0,'NavIcon_growth':1,'NavIcon_dungeon':2,'NavIcon_weapon':4,'ico_lock':5,'NavIcon_shop':6,'HomeText_57':4}
    for w in s.widgets:
        if w['name'].startswith('NavButton_'):
            i=int(w['name'].split('_')[-1]);slot=i if i<3 else i+1
            w['rect'][0]=slot*cell;w['rect'][2]=cell
        elif w['name'] in slots:w['rect'][0]=(slots[w['name']]+.5)*cell-w['rect'][2]/2
        elif w['name']=='SelectedNav':w['rect'][0]=4.18*cell;w['rect'][2]=.64*cell
        elif w['name']=='NavNotification_0':w['rect'][0]=.5*cell+30.99
    s.notes.append('Default navigation has seven physical slots, centre empty with no button; six menus use slots 0,1,2,4,5,6. Contextual X belongs to popup.')
    # Only these four home component families were selected by the user.
    supplied={'Gold':'ui_hud_currency_frame','Ruby':'ui_hud_currency_frame','Chat':'ui_hud_notice_background','AutoFormation':'ui_hud_auto_formation','Quest':'ui_hud_quest'}
    for w in s.widgets:
        if w['name'] in supplied:
            asset=supplied[w['name']];pw,ph=struct.unpack('>II',SPRITES[asset].read_bytes()[16:24])
            x,y,width,height=w['rect'];newheight=width*ph/pw
            w['rect']=[x,y+(height-newheight)/2,width,newheight]
            w.update(kind='image',sprite=asset,color='#ffffff',preserveAspect=True)
        if w['name']=='HomeText_20':w['active']=False
        if w['name'] in ['HomeText_12','HomeText_13','HomeText_19']:w['rect'][0]={'HomeText_12':138,'HomeText_13':298.08,'HomeText_19':978.42}[w['name']]
        if w['name'] in ['QuestProgressTrack','QuestProgress']:w['rect'][2]=240 if w['name']=='QuestProgressTrack' else 72
    chat=next(w for w in s.widgets if w['name']=='Chat')
    bubble=s.image('ChatSummaryBubble','ui_chat_bubble',[0,0,chat['rect'][2]/3,chat['rect'][3]/3])
    bubble['parent']='Chat'
    bubble['preserveAspect']=True
    bubble['stretch']=True
    s.notes.append('User-provided chat summary (background plus bubble), quest, currency frame and auto formation only. Inventory slots and baked equipped badge explicitly excluded. All other home graphics remain placeholders pending design.')
    return s


if __name__=='__main__':
    record=build(home());p=ROOT/'Docs/ui-batch-manifest.json';d=json.loads(read(p));d['screens'].append(record);p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
    print('Prepared ScreenMainHome and MainHomePreview; source approval and Unity review pending.')
