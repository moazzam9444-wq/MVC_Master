'use strict';
const {spawnSync}=require('node:child_process'),path=require('node:path'),assert=require('node:assert/strict');
module.exports=function requireTestDatabase(){
 assert(process.env.KICKS_TEST_CONNECTION,'Set KICKS_TEST_CONNECTION explicitly; mutating tests must not use Kicks.');
 const result=spawnSync('pwsh',['-NoProfile','-File',path.join(__dirname,'require-test-database.ps1')],{encoding:'utf8',windowsHide:true});
 assert.equal(result.status,0,result.stderr||result.stdout||'Test database isolation failed.');
 process.env.ConnectionStrings__DefaultConnection=process.env.KICKS_TEST_CONNECTION;
};
module.exports.assertFreePort=function assertFreePort(port){
 assert(Number.isInteger(port)&&port>5602&&port<65536);
 const result=spawnSync('pwsh',['-NoProfile','-Command',`if(Get-NetTCPConnection -State Listen -LocalPort ${port} -ErrorAction SilentlyContinue){exit 1};exit 0`],{encoding:'utf8',windowsHide:true});
 assert.equal(result.status,0,`Test port ${port} is occupied; refusing to use an unknown application.`);
};
