"""Assemble the remaining approved popup states without operating Unity."""
from ui_batch import *


def extra_text(s,name,text,rect,font='GmarketSans-Bold SDF',size=12,tint='#f1e6d4',align='center',**kw):
    values=dict(text=text,font=font,fontSize=size*3,color=tint,spacing=0,italic=False,material='default',align=align)
    values.update(kw)
    return s.add(name,rect,kind='text',**values)


def revolver(key,name):
    s=Screen(key,name); empty=key=='rev-empty';warning=key.startswith('rev-warn')
    down=key in ['rev-nocrate','rev-warn-equip'];auto=key=='rev-auto'
    for e in LAYOUT['scenes'][key]:
        sel,i,r=e['sel'],e['i'],e['css400'];x,y,w,h=r
        if sel=='.rgc':s.image('EquippedCard' if i==0 else 'NewCard','ui_rev_card_frame_normal',r,button='select')
        elif sel=='.rgc .hx':
            s.image('EquippedHero' if i==0 else 'NewHero','ui_rev_hero_bg_01_E' if empty else ('ui_rev_hero_bg_05_A' if i==0 else 'ui_rev_hero_bg_06_S'),r,0,color='#ffffff@0.2' if empty else '#ffffff')
            s.solid('EquippedWeaponArt' if i==0 else 'NewWeaponArt',[x+(w-106)/2,y+20,106,64],'#ffffff@0')
        elif sel=='.rgc .hx .g':
            if empty:
                extra_text(s,'EmptyGrade','?',r,size=42,tint='#8f9aa8',italic=True)
                s.notes.append('rev-empty JSON omits the visible question-mark grade; added as a TMP glyph at .hx .g coordinates, not a PNG.')
            else:s.image('Grade_'+str(i),'ui_rev_grade_05_A' if i==0 else 'ui_rev_grade_06_S',r,14)
        elif sel=='.rgc .hx .tag':s.image('Tag_'+str(i),'ui_rev_tag_equipped_bg' if i==0 else 'ui_rev_tag_new_bg',r,2)
        elif sel=='.rgc .hx .lv':s.image('Level_'+str(i),'ui_rev_lv_pill',r,2)
        elif sel=='.rgc .n':s.solid('NameDivider_'+str(i),[x,y+h-2,w,1],'#d6ac60@0.16')
        elif sel=='.rgc .pw':s.icon('ico_power',139 if i==0 else 289,y+6,18)
        elif sel=='.rgc .k9-lb':s.image('EffectsBand_'+str(i),'ui_rev_card_section_band',r)
        elif sel=='.rgc .st':
            j=i%6;icons=['ico_attr_fire','ico_power','ico_crit','ico_critdmg','ico_attr_fire','ico_time']
            s.icon('ico_attr_ice' if i==6 else icons[j],(148 if i==0 else 325) if j==0 else x+4,y+(h-12)/2)
            s.solid('StatDivider_'+str(i),[x,y+h-1,w,1],'#d6ac60@0.08')
        elif sel=='.dk-pill':
            s.image('Explanation','ui_rev_info_pill',r)
            s.add('InfoCircle',[20,671.6,16,16],kind='circle',color='#f1e6d4')
            s.icon('ico_power',49,673.5)
        elif sel=='.dk-chip':
            s.image('CrateStock','ui_rev_crate_chip',r)
            s.icon('ico_gear' if auto else 'ico_crate',28,708.6,18)
        elif sel=='.dk-cta':
            sprite='ui_rev_btn_dark' if auto else 'ui_rev_btn_alt' if key=='rev-nocrate' else 'ui_rev_btn_gold_equip' if key in ['rev-autofound','rev-warn-discard'] else 'ui_rev_btn_gold'
            s.image('Minimize' if auto else 'GetCrates' if key=='rev-nocrate' else 'Draw',sprite,r,button='action')
            if sprite=='ui_rev_btn_gold':s.icon('ico_crate',207,734.7)
        elif sel=='.dk-x':
            s.solid('StopAuto' if auto else 'CloseLabel',r,'#ffffff@0',button='action' if auto else 'close')
            s.icon('ico_close_s',x,y+3,8)
    s.texts(range(5,len(DATA['sceneTexts'][key])) if warning else None)
    if down:
        for w in s.widgets:
            x,y,_,_=w['rect']
            if 198*3<=x and 229*3<=y<=653*3:w['tone']=True
    s.close()
    if warning:
        start=len(s.widgets)
        s.solid('WarningLayer',[0,0,400,867],'#000000@0.58',block=True)
        for e in LAYOUT['scenes'][key]:
            if e['sel']=='.cf-box':s.image('WarningPanel','ui_rev_confirm_panel',e['css400'])
            if e['sel']=='.cf-btn':s.image('Back' if e['i']==0 else 'Confirm','ui_rev_confirm_btn_back' if e['i']==0 else 'ui_rev_confirm_btn_ok',e['css400'],button='dismiss' if e['i']==0 else 'action',closeTarget='WarningLayer' if e['i']==0 else '')
        tx=DATA['sceneTexts'][key][0]['rect400'];s.icon('ico_warn',tx[0]-19,tx[1],13)
        s.icon('ico_power',66,397.4,13)
        s.texts(range(5))
        for w in s.widgets[start+1:]:w['parent']='WarningLayer'
        next(w for w in s.widgets if w.get('sourceIndex')==2)['wrap']=True
    s.notes.append('Game actions are integration slots only. All cards use plain frames; no reflection or animated highlight.')
    return s


def forge():
    s=Screen('pop-forgeinfo','PopupDrawRates')
    for e in LAYOUT['finalScenes'][s.key]:
        sel,i,r=e['sel'],e['i'],e['css400'];x,y,w,h=r
        if sel=='.fi-box':s.image('Panel','ui_pop_slab',r)
        elif sel=='.fi-header':
            s.solid('Header',r,'#121110')
            s.solid('HeaderDivider',[x,y+h-1,w,1],'#d6ac60@0.3')
        elif sel=='.c3-strip':s.image('GradeSummary','ui_pop_strip_panel',r)
        elif sel=='.fi-row':
            s.solid('RateRow_'+str(i),r,'#ffffff@0.035')
            sp=DATA['sceneTexts'][s.key][18+i*2];t=sp['rect400']
            s.image('Probability_'+str(i),'ui_pop_pct_pill',[t[0]-12,y+9,t[2]+24,30])
        elif sel=='.c3-fold':
            s.image('LockedGrades','ui_pop_fold_row',r,button='action');s.icon('ico_lock',26,y+9,14)
    for i,x in enumerate([100.7,174.9]):
        s.image('PreviousLevel' if i==0 else 'NextLevel','ui_pop_nav_btn',[x,426,28,26],button='disabled' if i==0 else 'action')
        s.icon('ico_arrow_l' if i==0 else 'ico_arrow_r',x+8,433,12)
    tiers=['#9e9e9e','#4caf50','#2196f3','#9c27b0','#ff9800','#ff6b5b','#cddc39','#e91e63','#00bcd4','#7986cb','#ffeb3b','#eeeeee','#f062d0']
    for i,col in enumerate(tiers):
        s.solid('GradeSwatch_'+str(i),[35.4+(i%7)*51.9,477+(i//7)*33,18,18],col+('@0.45' if i>=3 else ''))
    s.texts();s.close()
    return s


def unlock():
    s=Screen('pop-unlock','PopupFeatureUnlock')
    s.solid('TapToClose',[0,0,400,867],'#ffffff@0',button='close')
    for e in LAYOUT['finalScenes'][s.key]:
        if e['sel']=='.ul-banner':s.image('Banner','ui_pop_unlock_banner',e['css400'])
        elif e['sel']=='.ul-desc':s.image('Description','ui_pop_unlock_desc',e['css400'])
    s.texts()
    return s


def nickname():
    s=Screen('pop-nick','PopupNickname')
    for e in LAYOUT['finalScenes'][s.key]:
        if e['sel']=='.cf-box':s.image('Panel','ui_pop_card',e['css400'])
        elif e['sel']=='#nick-in':s.image('NicknameInput','ui_pop_input',e['css400'],block=True)
        elif e['sel']=='.cf-btn':s.image('Cancel' if e['i']==2 else 'Save','ui_pop_btn_sub' if e['i']==2 else 'ui_pop_btn_main_off',e['css400'],6 if e['i']==2 else 8,button='close' if e['i']==2 else 'disabled')
    s.texts()
    # Input values are excluded from browser text-node exports. Document this explicit field.
    extra_text(s,'NicknameValue','명사수',[70,408.9,260,18],font='Pretendard-SemiBold SDF',size=12,tint='#f3e8d6')
    s.notes.append('Editable nickname value is absent from sceneTexts; added separately as TMP inside the documented input rect. Local draft/count state only; persistence and prohibited-name policy remain host integration.')
    s.close()
    return s


def mail_received():
    s=Screen('pop-mailgot','PopupMailboxReceived');reward_ids=['ico_gold','ico_ticket','ico_crate','ico_gold','ico_crate','ico_scroll','ico_crate']
    for e in LAYOUT['finalScenes'][s.key]:
        sel,i,r=e['sel'],e['i'],e['css400'];x,y,w,h=r
        if sel=='.dgr-box':s.image('Panel','ui_pop_slab',r)
        elif sel=='.dgr-title':s.image('Header','ui_pop_title_band',r)
        elif sel=='.mail-row':
            s.solid('RowDivider_'+str(i),[x,y+h-1,w,1],'#d6ac60@0.16')
            s.image('IconFrame_'+str(i),'ui_pop_icon_frame',[22,y+8,36,36])
            s.icon(['ico_trophy','ico_calendar','ico_gift'][i],31,y+17,18)
        elif sel=='.c3-mc span' and i<7:
            first=i in [0,3,5];s.icon(reward_ids[i],x+(2 if first else 13),y+.5)
            if not first:s.solid('RewardDivider_'+str(i),[x+4,y,1,h],'#d6ac60@0.16')
        elif sel=='#mail-claim':s.image('ClaimedAll','ui_pop_btn_main_off',r,button='disabled')
    s.texts(range(15));s.close()
    start=len(s.widgets);s.solid('ReceivedLayer',[0,0,400,867],'#000000@0.42',button='dismiss',closeTarget='ReceivedLayer')
    band=next(e['css400'] for e in LAYOUT['finalScenes'][s.key] if e['sel']=='.c3-gotband')
    s.image('RewardBand','ui_pop_got_band',band)
    for e in LAYOUT['finalScenes'][s.key]:
        if e['sel']=='.c3-mc span' and e['i']>=7:
            i=e['i']-7;x,y,w,h=e['css400'];s.icon(reward_ids[i],x+(2 if i in [0,5] else 13),y+3,12)
            if i not in [0,5]:s.solid('SumDivider_'+str(i),[x+4,y+3,1,13],'#d6ac60@0.16')
    s.texts(range(15,24))
    for w in s.widgets[start+1:]:w['parent']='ReceivedLayer'
    s.notes.append('Ignoring .mi-go entries from unrelated hidden source popups. Reward overlay is last in draw order; tap dismisses only the overlay.')
    return s


def idle(key,name):
    s=Screen(key,name);empty=key=='off-empty';capped=key=='off-cap'
    panel=LAYOUT['finalScenes'][key][0]['css400'];header=LAYOUT['finalScenes'][key][1]['css400']
    s.image('Panel','ui_idle_slab_after' if empty else 'ui_idle_slab_before',panel)
    s.image('Header','ui_idle_title_band',header)
    # User revision: match Crumble's visible art height and foreground overlap.
    # Alpha bounds remove transparent PNG margins from the measurement.
    hero=json.loads(read(ROOT/'Docs/idle-hero-layout.json'))
    r=hero['rect400RelativeToHeader'].copy();r[1]+=header[1]
    s.image('RewardHero','ui_idle_reward_hero_gold_glow',r,0)
    if not empty:
        if not capped:
            s.image('StageBreakthrough','ui_idle_breakthrough_panel',MANIFEST['ui_idle_breakthrough_panel']['css400'])
            s.icon('ico_gate',120,564,10)
            for x,y in [(44,498),(337,507),(76,560)]:s.icon('ico_sparkle',x,y,12)
        offset=-2.5 if capped else 0
        r=MANIFEST['ui_idle_tiles_panel']['css400'].copy();r[1]+=offset;s.image('RewardTiles','ui_idle_tiles_panel',r)
        for i,ic in enumerate(['ico_gold','ico_ticket','ico_crate','ico_scroll','ico_ruby']):
            s.image('RewardTile_'+str(i),'ui_idle_tile_gold' if i==0 else 'ui_idle_tile',[20+i*73.2,605+offset,67.2,72.6])
            s.icon(ic,41.6+i*73.2,614+offset,24)
        s.icon('ico_time',94 if capped else 126,710,12)
        if capped:s.image('MaxBadge','ui_idle_badge_max_bg',MANIFEST['ui_idle_badge_max_bg']['css400'])
    else:
        for n,asset in [('CurrentStage','ui_idle_stage_box'),('NextStage','ui_idle_next_chip'),('Rates','ui_idle_rates_panel')]:s.image(n,asset,MANIFEST[asset]['css400'])
        s.icon('ico_search',228,467,22)
        for i,ic in enumerate(['ico_gold','ico_ticket','ico_crate','ico_scroll','ico_ruby']):
            x=20 if i in [0,1,3] else 203;y=548 if i==0 else 598 if i in [1,2] else 648;w=360 if i==0 else 177
            s.image('RateRow_'+str(i),'ui_idle_rate_row',[x,y,w,44])
            s.image('RateIconFrame_'+str(i),'ui_idle_rate_icon_bg_gold' if i==0 else 'ui_idle_rate_icon_bg',[x+5,y+5,34,34])
            s.icon(ic,x+11,y+11,22)
    s.image('InstantClaim' if empty else 'DoubleClaim','ui_idle_btn_side',MANIFEST['ui_idle_btn_side']['css400'],button='action')
    s.icon('ico_power' if empty else 'ico_ad',38,740,22)
    s.image('Claim','ui_idle_btn_claim_disabled' if empty else 'ui_idle_btn_claim',MANIFEST['ui_idle_btn_claim']['css400'],button='disabled' if empty else 'action')
    s.texts();s.close()
    s.notes.append('R4 manifest supplies the missing detailed idle layout; finalScenes supplies panel/header, text positions use sceneTexts without shifts. Icons and combat integration deferred.')
    return s


def make_shadow_material():
    p=ROOT/'Fonts/SDF/MAT_GmarketBold_Shadow1.mat'
    if not p.exists():
        b=read(ROOT/'Fonts/SDF/MAT_GmarketBold_BtnGoldText.mat').replace('MAT_GmarketBold_BtnGoldText','MAT_GmarketBold_Shadow1')
        b=re.sub(r'(_UnderlayColor: )\{[^}]+\}',r'\1{r: 0, g: 0, b: 0, a: 1}',b)
        p.write_text(b,encoding='utf-8');meta(p,ROOT/'Fonts/SDF/MAT_GmarketBold_BtnGoldText.mat.meta')


if __name__=='__main__':
    make_shadow_material()
    screens=[revolver(k,n) for k,n in [('rev-empty','PopupRevolverEmpty'),('rev-auto','PopupRevolverAutoDraw'),('rev-autofound','PopupRevolverAutoFound'),('rev-nocrate','PopupRevolverNoCrates'),('rev-warn-discard','PopupRevolverDiscardWarning'),('rev-warn-equip','PopupRevolverEquipWarning')]]
    screens += [forge(),unlock(),nickname(),mail_received(),idle('off','PopupIdleRewards'),idle('off-cap','PopupIdleRewardsFull'),idle('off-empty','PopupIdleRewardsEmpty')]
    records=[build(s) for s in screens]
    p=ROOT/'Docs/ui-batch-manifest.json'
    assert not p.exists()
    p.write_text(json.dumps({'version':1,'status':'FILES_PREPARED_EDITOR_REVIEW_PENDING','screens':records},ensure_ascii=False,indent=2),encoding='utf-8');meta(p)
    print('Prepared',len(records),'screens without Unity operations.')
