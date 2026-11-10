'use strict';
require('./test-database.cjs')();
require('./test-database.cjs').assertFreePort(5613);
const fs=require('node:fs'),path=require('node:path'),cp=require('node:child_process'),crypto=require('node:crypto');
const {chromium}=require(process.env.KICKS_PLAYWRIGHT_PATH||'C:/Users/USER/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'..'),base=process.env.KICKS_TEST_URL;
if(!/^http:\/\/127\.0\.0\.1:5630$/.test(base||''))throw Error('Phase 8 requires the isolated host on 5630.');
const stage=process.env.KICKS_PHASE8_STAGE==='before'?'before':'after',dir=path.join(root,'.local/phase8',stage);
fs.mkdirSync(dir,{recursive:true});
const marker='booking-browser-'+crypto.randomUUID().replaceAll('-',''),reviewMarker='auth-browser-'+crypto.randomUUID().replaceAll('-',''),reviewToken=crypto.randomBytes(32).toString('hex');
const results=[],errors=[],network=[];let browser,bridge;
function check(ok,name){results.push({name,passed:!!ok});console.log((ok?'PASS: ':'FAIL: ')+name);}
function fixture(action){const r=cp.spawnSync('dotnet',['tests/Auth.Integration/bin/Release/net10.0/Auth.Integration.dll','--booking-browser-'+action,marker],{cwd:root,encoding:'utf8',windowsHide:true});if(r.status!==0)throw Error('Booking fixture '+action+' failed.');return action==='setup'?JSON.parse(r.stdout.trim()):null;}
async function ready(url){for(let i=0;i<100;i++){try{if((await fetch(url)).ok)return;}catch{}await new Promise(r=>setTimeout(r,100));}throw Error('Isolated server unavailable.');}
async function inspect(page,name,url,width,screenshot=true){
 await page.setViewportSize({width,height:1000});const response=await page.goto(url);await page.waitForTimeout(450);
 if(stage==='after'&&name.endsWith('-overview'))await page.locator('#sqlOverview').waitFor();
 check(!response || response.status()<400 || name==='denied'&&response.status()===403,name+' loads at '+width);
 if(stage==='after'&&name==='home')check(await page.locator('script[src*="full-demo.js"],script[src*="demo-data.js"]').count()===0,'live homepage does not load sample initialization at '+width);
 check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),name+' contains horizontal layout at '+width);
 if(screenshot)await page.screenshot({path:path.join(dir,name+'-'+width+'.png'),fullPage:true});
}
(async()=>{
 const data=fixture('setup');
 const log=fs.openSync(path.join(dir,'bridge.log'),'w');
 bridge=cp.spawn('dotnet',['tests/Auth.Integration/bin/Release/net10.0/Auth.Integration.dll','--browser-bridge'],{cwd:root,env:{...process.env,KICKS_BROWSER_MARKER:reviewMarker,KICKS_REVIEW_TOKEN:reviewToken},stdio:['ignore',log,log],windowsHide:true});
 await ready('http://127.0.0.1:5613/Pages/Auth/login.html');
 browser=await chromium.launch({channel:'msedge',headless:true});
 const context=await browser.newContext({reducedMotion:'reduce'}),page=await context.newPage();
 page.on('pageerror',e=>errors.push({url:new URL(page.url()).pathname,message:e.message}));
 page.on('response',r=>{if(r.status()>=400&&new URL(r.url()).origin===base)network.push({url:new URL(r.url()).pathname,status:r.status()});});
 const publicPages=[['home','/index.html'],['fields','/Pages/Fields/fields.html'],['field-details','/Pages/Fields/field-details.html?id='+data.fieldId],['games','/Pages/OpenGames/open-games.html'],['tournaments','/Pages/Tournaments/tournaments.html'],['teams','/Pages/Teams/teams.html'],['about','/Pages/About/about.html'],['contact','/Pages/Contact/contact.html'],['login','/Pages/Auth/login.html'],['register','/Pages/Auth/register.html?role=owner'],['denied','/Pages/Auth/access-denied.html']];
 for(const width of [1440,768,390])for(const [name,url] of publicPages)await inspect(page,name,base+url,width,width!==768);
 for(const role of ['player','owner']){
  await page.goto(base+'/Pages/Auth/login.html');await page.locator('#email').fill(marker+'-'+role+'@example.test');await page.locator('#password').fill('TemporaryBooking2026!');await Promise.all([page.waitForNavigation(),page.locator('#submitAuth').click()]);
  const sections=role==='owner'?['overview','fields','bookings','profile','messages']:['overview','bookings','profile','messages'];
  for(const width of [1440,768,390,320])for(const section of sections){await inspect(page,role+'-'+section,base+'/Pages/Dashboard/'+role+'.html#'+section,width,width!==768&&width!==320);}
  if(stage==='after'){
   await page.setViewportSize({width:390,height:844});await page.goto(base+'/Pages/Dashboard/'+role+'.html#overview');
   await page.locator('.kx-dashboard-menu').click();check(await page.locator('.kx-dashboard-menu').getAttribute('aria-expanded')==='true',role+' mobile workspace navigation opens');await page.keyboard.press('Escape');check(await page.locator('.kx-dashboard-menu').getAttribute('aria-expanded')==='false',role+' workspace navigation closes with Escape');
   await page.goto(base+'/Pages/Dashboard/'+role+'.html#messages');await page.locator('.p8-messages').waitFor();
   const contacts=await(await page.request.get(base+'/api/messages/contacts')).json();const recipient=contacts.items.find(p=>p.name==='Temporary booking '+(role==='player'?'owner':'player'));
   check(!!recipient,role+' sees authorized SQL conversation');await page.locator('[data-message-person="'+recipient.id+'"]').click();await page.locator('#messageText').waitFor();
   const text='<img src=x onerror="window.phase8Injected=true"> '+marker+' '+role;await page.locator('#messageText').fill(text);await page.locator('#sqlMessageForm button').click();await page.getByText(text,{exact:true}).waitFor();
   check(!await page.evaluate(()=>window.phase8Injected),role+' message content renders as text');await page.reload();await page.locator('[data-message-person="'+recipient.id+'"]').click();await page.getByText(text,{exact:true}).waitFor();check(true,role+' conversation persists after refresh');
   await page.screenshot({path:path.join(dir,role+'-sql-conversation-390.png'),fullPage:true});
   if(role==='player'){
    await page.goto(base+'/Pages/Contact/contact.html');await page.locator('#feedbackName').fill(marker);await page.locator('#feedbackEmail').fill(marker+'-player@example.test');await page.locator('[name=rating][value="5"]').check();await page.locator('#feedbackMessage').fill('Phase 8 persisted feedback '+marker);await page.locator('#feedbackForm button').click();await page.locator('#feedbackStatus').filter({hasText:'saved'}).waitFor();check(true,'feedback succeeds only after server persistence');
    await page.goto(base+'/index.html');await page.locator('#homeDate').fill(new Intl.DateTimeFormat('en-CA',{timeZone:'Asia/Amman'}).format(new Date(Date.now()+2*86400000)));await page.locator('#homeTime').selectOption('13:00');await page.locator('#homeSearch button[type=submit]').click();await page.locator('#homeSearchStatus').filter({hasText:'available pitch(es)'}).waitFor();check(true,'home search uses SQL session availability');
   }
  }
  await page.setViewportSize({width:1440,height:1000});await page.goto(base+'/Pages/Dashboard/'+role+'.html#profile');await page.locator('[data-logout]').click();await page.waitForURL('**/index.html');
 }
 const adminContext=await browser.newContext({reducedMotion:'reduce'});await adminContext.addCookies([{name:'Kicks.TestReviewer',value:reviewToken,url:'http://127.0.0.1:5613',httpOnly:true,sameSite:'Strict'}]);const admin=await adminContext.newPage();admin.on('pageerror',e=>errors.push({url:'admin',message:e.message}));
 for(const width of [1440,768,390,320])for(const section of ['overview','users','fields','bookings','revenue','settings','feedback','profile','messages'])await inspect(admin,'admin-'+section,'http://127.0.0.1:5613/Pages/Dashboard/admin.html#'+section,width,width!==768&&width!==320);
 if(stage==='after'){
  await admin.goto('http://127.0.0.1:5613/Pages/Dashboard/admin.html#feedback');await admin.locator('[data-workspace-filter] input').fill(marker);await admin.locator('[data-workspace-filter] button').click();await admin.getByText('Phase 8 persisted feedback '+marker,{exact:true}).waitFor();check(true,'administrator sees SQL feedback submitted by player');
  await page.goto(base+'/missing-phase8-page');check((await page.title()).includes('404'),'missing HTML pages show branded recovery page');check((await page.request.get(base+'/js/admin-account.js')).headers()['x-content-type-options']==='nosniff','static responses include MIME sniffing protection');
 }
 check(errors.length===0,'no JavaScript runtime errors on audited pages');
})().catch(e=>{check(false,e.message);}).finally(async()=>{
 if(browser)await browser.close();if(bridge)bridge.kill();fixture('cleanup');
 const report={stage,passed:results.filter(r=>r.passed).length,failed:results.filter(r=>!r.passed).length,skipped:0,results,errors,network};
 fs.writeFileSync(path.join(dir,'result.json'),JSON.stringify(report,null,2));console.log(JSON.stringify({stage,passed:report.passed,failed:report.failed}));if(report.failed)process.exitCode=1;
});
