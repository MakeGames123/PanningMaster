// Read-only extraction in a separate headless browser. Never connects to a user browser.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const {chromium}=require('C:/Users/kykyw/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..');
const source='D:/Dev/260701 panning master/아트 연구/아스트라 하네스/edits/20260927-227/home.html';
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try {
  const page=await browser.newPage({viewport:{width:400,height:867}});
  await page.route(/^https?:/,r=>r.abort());
  let html=fs.readFileSync(source,'utf8');
  const faces=[['GmkB','GmarketSans-Bold.ttf'],['GmkM','GmarketSans-Medium.ttf'],['PlX','GmarketSans-Bold.ttf']].map(([family,file])=>`@font-face{font-family:'${family}';src:url(data:font/ttf;base64,${fs.readFileSync(path.join(root,'Fonts',file)).toString('base64')})}`).join('');
  html=html.replace('</style>',faces+'</style>');
  await page.setContent(html,{waitUntil:'load'});await page.evaluate(()=>document.fonts.ready);
  const data=await page.evaluate(()=>{
   const rect=r=>[r.x,r.y,r.width,r.height].map(v=>Math.round(v*100)/100);
   const color=c=>{const a=c.match(/[\d.]+/g).map(Number);return '#'+a.slice(0,3).map(n=>n.toString(16).padStart(2,'0')).join('')+(a.length>3&&a[3]!==1?'@'+a[3]:'');};
   const elements=[];
   const selectors=['#hud','#nick','#s-pow','#s-rank','#c-gold','#c-ruby','#menu','#stage','#boss','#boss .lb','#boss .bar','#boss .bar i','#cyl','#hub','#hub img','.ch','.ch .bl','.ch .no','.ch .lv','#chat','#auto-f','#quest','#quest .ico','#quest .ico .dia','#quest .pg','#quest .pg i','#quest .go','#inv','#inv .crest','#grid','.sl','.sl .bt','.sl .g','.sl .lv','.sl .eq','.sl .nw','#tabs','.tb','#dlv','#dlv .pg','#dlv .pg i','#dlv .info','#chip','#draw','#nav','.nv','.nv img','.dot'];
   for(const sel of selectors)document.querySelectorAll(sel).forEach((e,i)=>{const cs=getComputedStyle(e);elements.push({sel,i,rect400:rect(e.getBoundingClientRect()),color:color(cs.color),grade:e.textContent.trim(),tier:cs.getPropertyValue('--gc').trim(),elementColor:cs.getPropertyValue('--ec').trim()});});
   const walker=document.createTreeWalker(document.body,NodeFilter.SHOW_TEXT);const texts=[];
   while(walker.nextNode()){
    const n=walker.currentNode,e=n.parentElement,t=n.textContent.trim();
    if(!t||e.closest('script,style,.g,.ic,.bub,#chip>i,#auto-f>i,#menu,#boss .lb,#draw small'))continue;
    if(/[\p{Extended_Pictographic}\uFE0F]/u.test(t))continue;
    const cs=getComputedStyle(e);const range=document.createRange();range.selectNodeContents(n);const r=range.getBoundingClientRect();
    if(r.width<.1||r.height<.1)continue;
    texts.push({text:t,rect400:rect(r),rectUnity1200:rect(r).map(v=>Math.round(v*300)/100),fontAsset:cs.fontFamily.includes('GmkM')?'GmarketSans-Medium SDF':'GmarketSans-Bold SDF',unityFontSize:parseFloat(cs.fontSize)*3,color:color(cs.color),characterSpacing:cs.letterSpacing==='normal'?0:parseFloat(cs.letterSpacing)/parseFloat(cs.fontSize)*100,align:cs.textAlign==='center'?'center':'start',selector:e.id?'#'+e.id:e.className,parent:e.closest('#inv')?'inventory':'',italic:cs.fontStyle==='italic'});
   }
   return {elements,texts};
  });
  const doc={source,sourceSha256:crypto.createHash('sha256').update(fs.readFileSync(source)).digest('hex'),status:'HOME_SOURCE_DRAFT_FOR_BATCH_REVIEW',note:'Source home-skin-r01 is a prior home proposal, not the popup r4 approval. Current no-emoji/no-sheen rules override it. No raster screenshot is used as UI.',...data};
  fs.writeFileSync(path.join(root,'Docs/home-ui-layout.json'),JSON.stringify(doc,null,2));
  console.log(`Extracted ${data.elements.length} element rects and ${data.texts.length} text runs without desktop operations.`);
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
