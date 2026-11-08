'use strict';
require('./test-database.cjs')();
require('./test-database.cjs').assertFreePort(5613);
const assert=require('node:assert/strict'),cp=require('node:child_process'),fs=require('node:fs'),crypto=require('node:crypto'),path=require('node:path');
const {chromium}=require(process.env.KICKS_PLAYWRIGHT_PATH||'C:/Users/USER/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'..'),base=process.env.KICKS_TEST_URL,reviewBase='http://127.0.0.1:5613';
assert(base && /^http:\/\/127\.0\.0\.1:\d+$/.test(base) && !base.endsWith(':5602'),'Set KICKS_TEST_URL to the isolated application; the normal port 5602 is forbidden.');
const marker='auth-browser-'+crypto.randomUUID().replaceAll('-',''),reviewToken=crypto.randomBytes(32).toString('hex');
const password='FinalBrowserAuth2026!',ownerEmail=marker+'-owner@example.test',playerEmail=marker+'-player@example.test';
const evidence=process.env.KICKS_TEST_EVIDENCE||path.join(root,'.local/final-auth');fs.mkdirSync(evidence,{recursive:true});
let bridge,browser,passed=0,failed=0;const results=[],errors=[];
function check(condition,name){assert(condition,name);passed++;results.push(name);console.log('PASS: '+name);}
function fixture(action){const r=cp.spawnSync('pwsh',['-NoProfile','-File','tests/auth-browser-data.ps1','-Marker',marker,'-Action',action],{cwd:root,encoding:'utf8'});assert.equal(r.status,0,r.stderr);return r.stdout.trim()?JSON.parse(r.stdout.replace(/^\uFEFF/,'')):[];}
const record=email=>fixture('Inspect').find(row=>row.email===email);
function proofPdf(){
 const objects=['<</Type/Catalog/Pages 2 0 R>>','<</Type/Pages/Kids[3 0 R]/Count 1>>','<</Type/Page/Parent 2 0 R/MediaBox[0 0 300 200]/Resources<<>>/Contents 4 0 R>>','<</Length 0>>\nstream\n\nendstream'];
 let pdf='%PDF-1.4\n',offsets=[0];for(let i=0;i<objects.length;i++){offsets.push(Buffer.byteLength(pdf));pdf+=`${i+1} 0 obj\n${objects[i]}\nendobj\n`;}
 const xref=Buffer.byteLength(pdf);pdf+='xref\n0 5\n0000000000 65535 f \n'+offsets.slice(1).map(n=>String(n).padStart(10,'0')+' 00000 n \n').join('')+'trailer\n<</Size 5/Root 1 0 R>>\nstartxref\n'+xref+'\n%%EOF\n';return Buffer.from(pdf);
}
async function registration(page,email,role){await page.goto(base+'/Pages/Auth/register.html?role='+role);await page.locator('#fullName').fill('Temporary Final '+(role==='owner'?'Owner':'Player'));await page.locator('#email').fill(email);await page.locator('#password').fill(password);await page.locator('#confirmPassword').fill(password);}
async function submit(page){await Promise.all([page.waitForNavigation(),page.locator('#submitAuth').click()]);}
async function login(page,email,expected){await page.goto(base+'/Pages/Auth/login.html');await page.locator('#email').fill(email);await page.locator('#password').fill(password);await Promise.all([page.waitForURL(expected),page.locator('#submitAuth').click()]);}
async function adminRow(page,id){const target=reviewBase+'/Pages/Dashboard/admin.html#users';if(page.url()===target)await page.reload();else await page.goto(target);const row=page.locator(`[data-account-id="${id}"]`);await row.waitFor();return row;}
async function clickReview(page,row,label){await Promise.all([page.waitForNavigation(),row.getByRole('button',{name:label,exact:true}).click()]);await page.locator('#serverAccounts').waitFor();}
async function waitForBridge(){for(let i=0;i<100;i++){if(bridge.exitCode!==null)throw Error('Test bridge exited before readiness.');try{const r=await fetch(reviewBase+'/Pages/Auth/login.html');if(r.ok)return;}catch{}await new Promise(resolve=>setTimeout(resolve,100));}throw Error('Test bridge did not become ready.');}
(async()=>{
 const baseline=JSON.parse(fs.readFileSync(path.join(evidence,'data-before.json'),'utf8').replace(/^\uFEFF/,''));
 const bridgeLog=fs.openSync(path.join(evidence,'bridge.log'),'w');
 bridge=cp.spawn('dotnet',['tests/Auth.Integration/bin/Release/net10.0/Auth.Integration.dll','--browser-bridge'],{cwd:root,env:{...process.env,KICKS_BROWSER_MARKER:marker,KICKS_REVIEW_TOKEN:reviewToken},stdio:['ignore',bridgeLog,bridgeLog],windowsHide:true});
 await waitForBridge();
 browser=await chromium.launch({channel:'msedge',headless:true});
 const context=await browser.newContext({reducedMotion:'reduce',viewport:{width:1440,height:1000}}),reviewContext=await browser.newContext({reducedMotion:'reduce',viewport:{width:1440,height:1000}});
 // A private HttpOnly test-host cookie also covers native download requests. The real MVC app never recognizes it.
 await reviewContext.addCookies([{name:'Kicks.TestReviewer',value:reviewToken,url:reviewBase,httpOnly:true,sameSite:'Strict'}]);
 const page=await context.newPage(),admin=await reviewContext.newPage();for(const tab of [page,admin]){tab.setDefaultTimeout(15000);tab.on('pageerror',error=>errors.push(error.message));}
 await page.goto(base+'/Pages/Auth/login.html');await page.screenshot({path:path.join(evidence,'login-desktop.png'),fullPage:true});
 check(await page.locator('#authForm').isVisible(),'actual running MVC login page renders on desktop');
 await page.locator('#switchCopy a').click();await page.waitForURL('**/register.html');check(await page.locator('#fullName').isVisible(),'animated login-to-registration switch works');
 await registration(page,ownerEmail,'owner');check(await page.locator('[name=role][value=owner]').isChecked(),'owner request selects the existing owner registration UI');
 await page.locator('#submitAuth').click();check(/Upload/.test(await page.locator('#proofError').innerText()),'ownership proof required by browser validation');
 await Promise.all([page.waitForNavigation(),page.locator('#authForm').evaluate(form=>HTMLFormElement.prototype.submit.call(form))]);
 check(/Upload your field ownership proof/.test(await page.locator('.validation-summary-errors').innerText()),'ownership proof required by server when JavaScript validation is bypassed');
 check(!record(ownerEmail),'missing proof creates no SQL account');
 await registration(page,ownerEmail,'owner');await page.locator('#ownerProof').setInputFiles({name:'invalid.txt',mimeType:'text/plain',buffer:Buffer.from('invalid')});await page.locator('#submitAuth').click();
 check(/Upload/.test(await page.locator('#proofError').innerText()),'unsupported document type rejected in UI');
 await page.locator('#ownerProof').setInputFiles({name:'spoofed.pdf',mimeType:'application/pdf',buffer:Buffer.from('<html>not a PDF</html>')});await submit(page);
 check(/matching file contents/.test(await page.locator('.validation-summary-errors').innerText()),'spoofed PDF rejected by server signature validation through the form');
 await registration(page,ownerEmail,'owner');await page.locator('#ownerProof').setInputFiles({name:'oversized.pdf',mimeType:'application/pdf',buffer:Buffer.alloc(1048577)});await page.locator('#submitAuth').click();
 check(/1 MB/.test(await page.locator('#proofError').innerText()),'oversized ownership document rejected in UI');
 await Promise.all([page.waitForNavigation(),page.locator('#authForm').evaluate(form=>HTMLFormElement.prototype.submit.call(form))]);check(/up to 1 MB/.test(await page.locator('.validation-summary-errors').innerText()),'oversized ownership document rejected on server');
 check(!record(ownerEmail),'invalid documents leave no SQL account or proof');
 await registration(page,ownerEmail,'owner');const pdf=proofPdf();await page.locator('#ownerProof').setInputFiles({name:'temporary-ownership.pdf',mimeType:'application/pdf',buffer:pdf});await page.screenshot({path:path.join(evidence,'owner-registration-desktop.png'),fullPage:true});await submit(page);
 check(new URL(page.url()).pathname.endsWith('/login.html') && /Request submitted/.test(await page.locator('#formStatus').innerText()),'valid PDF owner registration succeeds through real UI');
 let owner=record(ownerEmail);check(owner?.status==='Pending' && owner.requested_role==='owner','owner registration is Pending in real SQL Server');check(owner.role===null && owner.role_count===0,'pending owner has no Identity role');check(owner.has_password_hash===1,'owner has a stored Identity password hash');check(owner.document_id>0 && owner.storage_url,'ownership document metadata persisted in SQL');
 await login(page,ownerEmail,'**/login.html');check(/Unable to sign in/.test(await page.locator('.validation-summary-errors').innerText()),'pending owner login blocked');check(await page.evaluate(()=>KicksAuth.current())===null,'pending owner receives no authenticated navigation state');
 await registration(page,ownerEmail,'owner');await page.locator('#ownerProof').setInputFiles({name:'valid.png',mimeType:'image/png',buffer:Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jE1sAAAAASUVORK5CYII=','base64')});await submit(page);
 check(/Unable to register/.test(await page.locator('.validation-summary-errors').innerText()),'valid PNG passes document validation; duplicate owner email is blocked');
 check(fixture('Inspect').filter(row=>row.email===ownerEmail).length===1,'duplicate attempt creates no second account or proof');
 let row=await adminRow(admin,owner.id);check((await row.innerText()).includes(ownerEmail) && (await row.innerText()).includes('Pending'),'pending owner appears in real Admin Users UI under isolated reviewer principal');
 check(await row.locator('[name=AssignedRole]').inputValue()==='owner','admin review defaults assigned role to requested owner');
 check(await row.locator('small.table-secondary').first().evaluate(element=>element.scrollWidth<=element.clientWidth+1 && element.getBoundingClientRect().right<=element.parentElement.getBoundingClientRect().right),'long registration email wraps inside its review table column');
 await row.screenshot({path:path.join(evidence,'pending-owner-review.png')});
 const downloadPromise=admin.waitForEvent('download');await row.getByRole('link',{name:/Download temporary-ownership/}).click();const download=await downloadPromise;const stream=await download.createReadStream();const chunks=[];for await(const chunk of stream)chunks.push(chunk);
 const downloaded=Buffer.concat(chunks);console.log('Download diagnostics: '+JSON.stringify({bytes:downloaded.length,expectedBytes:pdf.length,suggestedFilename:download.suggestedFilename(),expectedDigest:crypto.createHash('sha256').update(pdf).digest('hex'),actualDigest:crypto.createHash('sha256').update(downloaded).digest('hex')}));
 check(downloaded.equals(pdf),'administrator downloads the exact private proof bytes through review UI');
 check(download.suggestedFilename()==='temporary-ownership.pdf','private proof download preserves the safe original filename');
 const anonymousResponse=await context.request.get(base+'/Pages/Auth/accounts/documents/'+owner.document_id,{maxRedirects:0});check(anonymousResponse.status()===302 && anonymousResponse.headers().location.includes('login.html'),'anonymous document download is denied');
 check((await context.request.get(base+'/App_Data/ownership/'+owner.storage_url)).status()===404,'private file is not exposed by static-file middleware');
 const originalPlayer=baseline.Accounts.find(account=>account.Roles==='player');
 if(originalPlayer){const blocked=await admin.evaluate(async id=>{const token=document.querySelector('#serverAccounts input[name=__RequestVerificationToken]').value;const body=new URLSearchParams({Id:String(id),Status:'Inactive',ConcurrencyStamp:'unused',__RequestVerificationToken:token});return (await fetch('/Pages/Auth/accounts/status',{method:'POST',body})).status;},originalPlayer.Id);check(blocked===403,'test host refuses mutation of the existing real player account');}
 await row.locator('[name=AssignedRole]').selectOption('owner');await clickReview(admin,row,'Approve');owner=record(ownerEmail);check(owner.status==='Active','UI approval persists Active owner status');check(owner.role==='owner' && owner.role_count===1,'approved owner receives exactly one Identity owner role');check(owner.reviewed_by===baseline.Accounts.find(account=>account.Roles==='admin').Id && owner.reviewed_at,'UI approval saves reviewer/time without modifying administrator');
 await login(page,ownerEmail,'**/Dashboard/owner.html');await page.locator('#accountName').filter({hasText:'Temporary Final Owner'}).waitFor();check(true,'approved owner logs in and redirects to Owner Dashboard');check(await page.evaluate(()=>KicksAuth.current()?.role)==='owner','owner navigation derives login state from server');
 await page.goto(base+'/Pages/Dashboard/owner.html#profile');check(/Temporary Final Owner/.test(await page.locator('.profile-details-grid').innerText()),'Owner Dashboard renders SQL-backed profile');await page.screenshot({path:path.join(evidence,'owner-dashboard-desktop.png'),fullPage:true});
 for(const route of ['/Pages/Dashboard/admin.html','/Pages/Dashboard/player.html','/Pages/Auth/accounts','/Pages/Auth/accounts/documents/'+owner.document_id]){const response=await context.request.get(base+route,{maxRedirects:0});check(response.status()===302 && response.headers().location.includes('access-denied'),'owner is denied restricted route '+route);}
 await page.locator('[data-logout]').click();await page.waitForURL('**/index.html');check(await page.evaluate(()=>KicksAuth.current())===null,'owner logout clears authenticated navigation');
 await page.goto(base+'/Pages/Dashboard/owner.html');await page.waitForURL('**/login.html?*');check(true,'logged-out owner cannot reopen protected dashboard');
 await login(page,ownerEmail,'**/Dashboard/owner.html');row=await adminRow(admin,owner.id);await clickReview(admin,row,'Deactivate');check(record(ownerEmail).status==='Inactive','admin UI deactivates only temporary owner');await page.reload();await page.waitForURL('**/login.html?*');check(true,'deactivation revokes existing owner session on next request');
 await login(page,ownerEmail,'**/login.html');check(/Unable to sign in/.test(await page.locator('.validation-summary-errors').innerText()),'Inactive owner cannot obtain a fresh session');
 row=await adminRow(admin,owner.id);await clickReview(admin,row,'Reactivate');check(record(ownerEmail).status==='Active','admin UI reactivates temporary owner');await login(page,ownerEmail,'**/Dashboard/owner.html');check(await page.evaluate(()=>KicksAuth.current()?.role)==='owner','reactivated owner can log in with a fresh session');
 await page.locator('[data-logout]').click();await page.waitForURL('**/index.html');
 await registration(page,playerEmail,'player');await submit(page);let player=record(playerEmail);check(player.status==='Pending' && player.role_count===0,'temporary player registration remains Pending and unassigned');row=await adminRow(admin,player.id);await row.locator('[name=AssignedRole]').selectOption('player');await clickReview(admin,row,'Approve');player=record(playerEmail);check(player.status==='Active' && player.role==='player' && player.role_count===1,'player approval through Admin Users UI persists one player role');
 await login(page,playerEmail,'**/index.html');await page.locator('#avatarMenuButton').click();await page.locator('#avatarMenu a').filter({hasText:'Dashboard'}).click();await page.waitForURL('**/Dashboard/player.html');await page.locator('#accountName').filter({hasText:'Temporary Final Player'}).waitFor();check(await page.evaluate(()=>KicksAuth.current()?.role)==='player','player login, navigation and dashboard regression pass');
 await page.locator('[data-logout]').click();await page.waitForURL('**/index.html');await page.evaluate(()=>{sessionStorage.setItem('kicks-session-v2','1');localStorage.setItem('kicks-data-v2',JSON.stringify({users:[{id:1,name:'Forged admin',role:'admin',status:'Active'}]}));});await page.goto(base+'/Pages/Dashboard/admin.html');await page.waitForURL('**/login.html?*');check(await page.evaluate(()=>KicksAuth.current())===null,'forged local/session storage cannot authenticate as admin');
 for(const [width,height] of [[390,844],[768,1024]]){await page.setViewportSize({width,height});await page.goto(base+'/Pages/Auth/register.html?role=owner');await page.screenshot({path:path.join(evidence,'owner-registration-'+width+'.png'),fullPage:true});check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),'owner registration has no horizontal overflow at width '+width);await page.goto(base+'/Pages/Auth/login.html');check(await page.locator('#submitAuth').isVisible() && await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),'login layout remains usable at width '+width);}
 check(errors.length===0,'no JavaScript runtime errors across real authentication and review UIs');
 const noBypass=await fetch(base+'/Pages/Dashboard/admin.html',{headers:{'X-Auth-Test-Reviewer':reviewToken,'Cookie':'Kicks.TestReviewer='+reviewToken},redirect:'manual'});check(noBypass.status===302 && noBypass.headers.get('location').includes('login.html'),'actual running MVC application does not accept test-host authentication credentials');
})().catch(error=>{failed++;console.error(error);process.exitCode=1;}).finally(async()=>{
 try{if(browser)await browser.close();}finally{
  if(bridge && bridge.exitCode===null)bridge.kill();
  try{fixture('Cleanup');check(fixture('Inspect').length===0,'only this run’s temporary SQL accounts and private documents cleaned up');}catch(error){failed++;console.error(error);process.exitCode=1;}
  fs.writeFileSync(path.join(evidence,'browser-results.json'),JSON.stringify({passed,failed,results,errors,actualApplication:base,reviewApplication:reviewBase,reviewMode:'Isolated test principal; existing administrator read only; target guard protects existing accounts'},null,2));
  console.log(`Completed ${passed} browser assertions; ${failed} failures.`);
 }
});
