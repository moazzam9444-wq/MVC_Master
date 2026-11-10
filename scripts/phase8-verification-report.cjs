'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'..'),dir=path.join(root,'.local/phase8');
const read=file=>fs.readFileSync(path.join(dir,file),'utf8').replace(/^\uFEFF/,'');
function count(file,pattern){const log=read(file),match=log.match(pattern);assert(match,'Missing completed result: '+file);assert(!/^FAIL:|Unhandled exception|AssertionError/m.test(log),'Failure in '+file);return Number(match[1]);}
const suiteDefinitions=[
 ['Authentication','auth','auth-final',/Completed (\d+) authentication checks/,/Completed (\d+) browser assertions; 0 failures/],
 ['Fields','fields','field',/Completed (\d+) field integration assertions/,/Completed (\d+) field browser assertions/],
 ['Bookings','bookings','booking',/Completed (\d+) booking integration assertions/,/Completed (\d+) booking browser assertions/],
 ['Teams','teams','teams',/Completed (\d+) teams integration assertions/,/Completed (\d+) teams browser assertions/],
 ['Open Games and challenges','phase5','activities',/Completed (\d+) phase5 SQL assertions/,/Completed (\d+) phase5 browser assertions/],
 ['Tournaments','tournaments','tournaments',/Completed (\d+) tournament SQL assertions/,/Completed (\d+) tournament browser assertions/],
 ['Stripe sandbox','stripe','stripe',/STRIPE SQL PASS: (\d+)/,/STRIPE BROWSER PASS: (\d+)/]
];
const suites=suiteDefinitions.map(([name,sql,browser,sqlPattern,browserPattern])=>({name,sql:count('final-sql-'+sql+'.log',sqlPattern),edge:count('final-edge-'+browser+'.log',browserPattern),failed:0,skipped:0}));
const extra=JSON.parse(read('after/result.json')),routes=JSON.parse(read('route-browser.json'));
assert(extra.failed===0&&routes.failed===0,'Phase 8 browser audits must pass');
const phase8Sql=count('final-sql-phase8.log',/Completed (\d+) Phase 8 SQL assertions/),isolation=count('isolation.log',/ISOLATION PASS: (\d+)/);
const sql=suites.reduce((n,s)=>n+s.sql,0)+phase8Sql,edge=suites.reduce((n,s)=>n+s.edge,0)+extra.passed+routes.passed;
for(const file of ['final-debug.log','final-release.log'])assert(/Build succeeded/.test(read(file))&&/0 Warning\(s\)/.test(read(file))&&/0 Error\(s\)/.test(read(file)),file+' must pass');
assert(/No changes have been made to the model/.test(read('ef-model.log')),'EF model consistency failed');
const preservation=JSON.parse(read('preservation-final.json'));assert(preservation.OriginalDatabaseUnchanged&&preservation.OriginalCheckDb&&preservation.IsolatedCheckDb,'Database preservation must pass');
const result={date:'2026-11-10',timezone:'Asia/Amman',suites,phase8Sql,phase8ResponsiveAndWorkflow:extra.passed,pageMapAndLinks:routes.passed,isolation,sql,edge,total:sql+edge+isolation,failed:0,skipped:0,debug:{warnings:0,errors:0},release:{warnings:0,errors:0},efModelConsistent:true,javascript:JSON.parse(read('javascript-syntax.json')),preservation};
fs.writeFileSync(path.join(dir,'verification-results.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));
