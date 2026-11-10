'use strict';
require('./test-database.cjs')();
const fs=require('node:fs'),path=require('node:path');
const {chromium}=require(process.env.KICKS_PLAYWRIGHT_PATH||'C:/Users/USER/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const base=process.env.KICKS_TEST_URL;if(base!=='http://127.0.0.1:5630')throw Error('Use the isolated Phase 8 host on 5630.');
const results=[],errors=[],links=new Set();let browser;
function check(ok,name){results.push({passed:!!ok,name});console.log((ok?'PASS: ':'FAIL: ')+name);}
(async()=>{
 browser=await chromium.launch({channel:'msedge',headless:true});const context=await browser.newContext({reducedMotion:'reduce',viewport:{width:1440,height:1000}}),page=await context.newPage();
 page.on('pageerror',e=>errors.push({page:new URL(page.url()).pathname,message:e.message}));
 const routes=JSON.parse(fs.readFileSync(path.join(__dirname,'../migration-routes.json'),'utf8'));
 for(const route of routes){
  const url=new URL(route.url,base+'/');const response=await page.goto(url.href);await page.waitForTimeout(200);
  check(response&&[200,403].includes(response.status()),'mapped page or authorization response: '+url.pathname);
  for(const href of await page.locator('a[href]').evaluateAll(nodes=>nodes.map(n=>n.href))){const link=new URL(href);if(link.origin===base&&link.pathname!==url.pathname&&!link.hash)links.add(link.href);}
 }
 for(const href of links){const response=await context.request.get(href);check([200,403].includes(response.status()),'internal GET link: '+new URL(href).pathname);}
 check(errors.length===0,'no runtime errors across complete legacy page map, including documentation and explicit demos');
})().catch(e=>check(false,e.message)).finally(async()=>{if(browser)await browser.close();const report={passed:results.filter(r=>r.passed).length,failed:results.filter(r=>!r.passed).length,skipped:0,results,errors};fs.writeFileSync(path.join(__dirname,'../.local/phase8/route-browser.json'),JSON.stringify(report,null,2));console.log(JSON.stringify({passed:report.passed,failed:report.failed}));if(report.failed)process.exitCode=1;});
