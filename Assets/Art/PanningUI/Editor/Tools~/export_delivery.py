"""Package actual Unity captures; never modifies source artwork or drives Unity."""
from pathlib import Path
import json, hashlib, zipfile, html, math, shutil
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/Delivery'
GROUPS=[
 ('01_홈', [('ScreenMainHome','홈 부분 적용 검수본')]),
 ('02_공통', [('PopupConfirm','확인'),('PopupInsufficientCurrency','재화 부족'),('PopupProductDetail','상품 상세'),('PopupDrawRates','뽑기 확률표'),('PopupFeatureUnlock','기능 해금'),('PopupNickname','닉네임 변경')]),
 ('03_전투력_우편', [('PopupCombatPower','전투력 상세'),('PopupStatSources','스탯 출처'),('PopupMailbox','우편함'),('PopupMailboxReceived','우편 보상 획득')]),
 ('04_방치', [('PopupIdleRewards','방치 보상'),('PopupIdleRewardsFull','방치 보상 최대'),('PopupIdleRewardsEmpty','방치 보상 빈 상태')]),
 ('05_리볼버_비교', [('PopupRevolverCompare','리볼버 상승'),('PopupRevolverCompareDown','리볼버 하락'),('PopupRevolverCompareEqual','리볼버 동률')]),
 ('06_리볼버_단독', [('PopupRevolverFirstDraw','리볼버 첫 뽑기'),('PopupRevolverEquipped','리볼버 장착 완료'),('PopupRevolverEmpty','리볼버 없음')]),
 ('07_리볼버_자동', [('PopupRevolverAutoDraw','자동 뽑기'),('PopupRevolverAutoFound','자동 중 상승 발견'),('PopupRevolverNoCrates','상자 부족')]),
 ('08_리볼버_경고', [('PopupRevolverDiscardWarning','판매 경고'),('PopupRevolverEquipWarning','장착 경고')])]
fontfile=ROOT/'Fonts/Pretendard-SemiBold.otf'
font=lambda n:ImageFont.truetype(str(fontfile),n)
SHEETS=OUT/'Overview_PNG';SHEETS.mkdir(exist_ok=True)
entries=[]
for group,items in GROUPS:
 for name,label in items:
  f=OUT/'PNG_2400'/f'{name}_2400x5202.png'
  with Image.open(f) as im:assert im.size==(2400,5202)
  entries.append(dict(number=len(entries)+1,group=group,name=name,label=label,file=f.relative_to(OUT).as_posix(),sha256=hashlib.sha256(f.read_bytes()).hexdigest()))
assert len(entries)==25

def sheet(filename,title,rows,cols,tile):
 gap=32;top=172;labelh=76;imageh=round(tile*2601/1200);cellh=imageh+labelh+gap
 w=cols*tile+(cols+1)*gap;h=top+math.ceil(len(rows)/cols)*cellh+gap
 im=Image.new('RGB',(w,h),'#211f1c');d=ImageDraw.Draw(im)
 d.text((gap,30),title,font=font(48),fill='#edd5a4')
 d.text((gap,96),'Unity 실제 렌더 · 개별 원본 2400 × 5202 · 홈은 4종 적용 검수본',font=font(28),fill='#bcb4a9')
 for j,e in enumerate(rows):
  x=gap+(j%cols)*(tile+gap);y=top+(j//cols)*cellh
  with Image.open(OUT/e['file']) as original:
   thumb=original.resize((tile,imageh),Image.Resampling.LANCZOS);im.paste(thumb,(x,y))
  d.text((x,y+imageh+12),f"{e['number']:02d}  {e['label']}",font=font(28 if tile<600 else 34),fill='white')
 im.save(SHEETS/filename,optimize=True)

sheet('00_전체_25종.png','PanningMaster UI · 팝업 24종 + 홈 부분 적용',entries,5,480)
for group,_ in GROUPS:
 rows=[e for e in entries if e['group']==group]
 sheet(group+'.png',group.replace('_',' '),rows,min(3,len(rows)),800)
(OUT/'catalog.json').write_text(json.dumps(entries,ensure_ascii=False,indent=2),encoding='utf-8')
(OUT/'README.md').write_text('# PanningMaster UI PNG 모음 — 2026-09-28\n\n팝업 완성본 24종 + 홈 부분 적용 검수본 1종. 던전은 사용자 후속 요청 전까지 보류하여 포함하지 않았습니다.\n\n- PNG_2400: Unity Game View에서 직접 렌더한 2400×5202 무손실 PNG 25장. 기존 작은 캡처를 확대하지 않았습니다. 원래 PNG 스프라이트는 변경 없이 Unity가 표시하며 TMP/벡터 패널은 출력 해상도로 렌더됩니다.\n- PNG_1200: 설계 기준 1200×2601 무손실 PNG 25장. 팝업 24종은 변경 전과 픽셀 차이 0입니다. 홈은 사용자 요청으로 제공 에셋 4종과 가운데 빈 내비를 적용해 의도적으로 변경했습니다.\n- Overview_PNG: 전체 목록 1장 + 분류별 목록 8장 + 홈 적용4종 요약 1장. 목록에는 검수용 제목만 추가했으며 게임 UI 원본에는 추가하지 않았습니다.\n- ZIP: 고해상도 원본25장(분류별 한글 파일명), 목록PNG10장 + 홈 제공원본5파일(4종), 화면목록/안내.\n\n아이콘·무기 그림·전투 아트의 빈 슬롯은 미제공 영역입니다. 홈은 채팅 요약·퀘스트·재화 프레임·최적 편성만 제공 에셋을 적용했습니다. 슬롯 PNG·장착중 배지는 사용하지 않았습니다. 그 밖의 홈 요소는 임시 표시이며 추후 새 시안으로 바뀝니다.  개발자 홈/전투 씬과 서버/보상/재화 로직 연결은 이번 UI 제작 범위에 포함되지 않습니다. 팝업은 자체 Preview 배경 위에 표시했습니다.\n\n## 화면 목록\n'+''.join(f"- {e['number']:02d} {e['label']} — {e['name']}\n" for e in entries),encoding='utf-8')
cards=''.join(f'<a href="PNG_1200/{e["name"]}_1200x2601.png"><img loading="lazy" src="PNG_1200/{e["name"]}_1200x2601.png"><b>{e["number"]:02d} {html.escape(e["label"])}</b><small>기준 원본 보기 · 고해상도는 ZIP</small></a>' for e in entries)
(OUT/'index.html').write_text('<!doctype html><html lang="ko"><meta charset="utf-8"><title>PanningMaster UI 25종</title><style>body{background:#211f1c;color:#eee;font-family:system-ui;margin:32px}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:24px}a{color:#edd5a4;text-decoration:none}img{width:100%;display:block}b,small{display:block;margin:10px 0}p{color:#bcb4a9}</style><h1>PanningMaster UI · 25종</h1><p>Unity 실제 렌더 · 홈 부분 적용 검수본 포함 · 던전 제외</p><p><a href="PanningMaster_UI_PNG_20260928.zip">고해상도 PNG 전체 ZIP</a> · <a href="Overview_PNG/00_전체_25종.png">전체 목록 PNG</a></p><main>'+cards+'</main></html>',encoding='utf-8')
source_folder=OUT/'Home_4Components_Source';source_folder.mkdir(exist_ok=True)
source_files=[('HUD/ui_hud_currency_frame.png','01_재화_프레임.png'),('HUD/ui_hud_auto_formation.png','02_최적_편성.png'),('HUD/ui_hud_quest.png','03_퀘스트.png'),('HUD/ui_hud_notice_background.png','04_채팅_요약_배경.png'),('Chat/ui_chat_bubble.png','04_채팅_요약_말풍선.png')]
for rel,name in source_files:shutil.copyfile(ROOT/'Sprites'/rel,source_folder/name)
board=Image.new('RGB',(2400,1400),'#292623');draw=ImageDraw.Draw(board)
draw.text((64,40),'홈에 적용한 제공 에셋 · 4종',font=font(54),fill='#edd5a4')
groups=[('재화 프레임',['01_재화_프레임.png']),('최적 편성',['02_최적_편성.png']),('퀘스트',['03_퀘스트.png']),('채팅 요약',['04_채팅_요약_배경.png','04_채팅_요약_말풍선.png'])]
for i,(label,names) in enumerate(groups):
 x=64+(i%2)*1170;y=170+(i//2)*590
 draw.text((x,y),label,font=font(40),fill='white')
 comp=Image.open(source_folder/names[0]).convert('RGBA')
 for name in names[1:]:comp=Image.alpha_composite(comp,Image.open(source_folder/name).convert('RGBA'))
 comp.thumbnail((1050,430));board.paste(comp,(x,y+90),comp)
board.save(SHEETS/'09_홈_적용4종.png',optimize=True)
zip_path=OUT/'PanningMaster_UI_PNG_20260928.zip'
with zipfile.ZipFile(zip_path,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for e in entries:z.write(OUT/e['file'],f"PNG_2400/{e['group']}/{e['number']:02d}_{e['label'].replace(' ','_')}_2400x5202.png")
 for f in SHEETS.glob('*.png'):z.write(f,'Overview_PNG/'+f.name)
 for f in source_folder.glob('*.png'):z.write(f,'Home_4Components_Source/'+f.name)
 z.write(OUT/'README.md','README.md');z.write(OUT/'catalog.json','catalog.json')
with zipfile.ZipFile(zip_path) as z:assert z.testzip() is None
print(json.dumps({'screens':len(entries),'overviewPNGs':len(list(SHEETS.glob('*.png'))),'zipBytes':zip_path.stat().st_size,'zip':str(zip_path)},ensure_ascii=True))
