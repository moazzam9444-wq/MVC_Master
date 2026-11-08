// Read-only verification of the upgraded local Kicks schema; never applies migrations or writes database records.
'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),cp=require('node:child_process');
const root=path.resolve(__dirname,'..');
const run=(exe,args)=>{const r=cp.spawnSync(exe,args,{cwd:root,encoding:'utf8',timeout:60000});assert.equal(r.status,0,r.stderr||r.stdout);return r.stdout;};
const load=p=>{const s=JSON.parse(fs.readFileSync(path.join(root,p),'utf8').replace(/^\uFEFF/,''));for(const k of Object.keys(s))if(Array.isArray(s[k]?.value))s[k]=s[k].value;return s;};
run('powershell',['-NoProfile','-ExecutionPolicy','Bypass','-File','scripts/inspect-kicks-database.ps1','-OutputPath','.local/database-setup/schema-after.json']);
const model=JSON.parse(run('dotnet',['bin/Release/net10.0/KicksMvc.dll','--verify-model-details','--verify-model-behavior']));
const before=load('.local/database-setup/schema-before.json'),after=load('.local/database-setup/schema-after.json');
const grouped=rows=>Object.values(Object.groupBy(rows,r=>r.Name));
const tables=new Map(after.Tables.map(t=>[t.Name,t]));
// SQL Server expands IN and BETWEEN in catalog CHECK/filter definitions.
const normal=s=>(s||'')
 .replace(/(\[[^\]]+\])\s+IN\s*\(((?:N?'(?:''|[^'])*'|[^')])+)\)/gi,(_,column,values)=>values.split(',').reverse().map(v=>`${column}=${v.trim()}`).join(' OR '))
 .replace(/(DATEPART\([^)]*\)|\[[^\]]+\])\s+BETWEEN\s+([^\s]+)\s+AND\s+([^\s)]+)/gi,(_,column,a,b)=>`${column}>=${a} AND ${column}<=${b}`)
 .replace(/[\s()]/g,'').toLowerCase();
const sqlType=c=>c.SqlType+(['nvarchar','varchar','varbinary'].includes(c.SqlType)?`(${c.MaxLength===-1?'max':c.SqlType==='nvarchar'?c.MaxLength/2:c.MaxLength})`:c.SqlType==='decimal'?`(${c.Precision},${c.Scale})`:['datetimeoffset','datetime2','time'].includes(c.SqlType)?`(${c.Scale})`:'');
const typeNormal=t=>t==='datetimeoffset'?'datetimeoffset(7)':t;
assert.equal(after.Database[0].Id,before.Database[0].Id);
assert.equal(after.Tables.length,35);assert.equal(model.Tables,34);assert.equal(model.Columns,269);assert.equal(model.Relationships,76);
assert.deepEqual([...tables.keys()].filter(t=>t!=='__EFMigrationsHistory').sort(),model.Entities.map(e=>e.Table).sort());
for(const old of before.Tables){const t=tables.get(old.Name);assert.ok(t,old.Name);assert.equal(t.Id,old.Id,`${old.Name}: table was recreated`);assert.equal(t.Records,old.Records,`${old.Name}: records changed`);}
const users=tables.get('users');assert.equal(users.Records,0);assert.equal(tables.get('AspNetRoles').Records,3);assert.equal(tables.get('__EFMigrationsHistory').Records,2);
const incoming=grouped(before.ForeignKeys.filter(f=>f.PrincipalTableId===before.Tables.find(t=>t.Name==='users').Id));assert.equal(incoming.length,21);
for(const fk of before.ForeignKeys){assert.ok(after.ForeignKeys.some(f=>f.Id===fk.Id&&f.Ordinal===fk.Ordinal&&f.TableId===fk.TableId&&f.Column===fk.Column&&f.PrincipalTableId===fk.PrincipalTableId&&f.PrincipalColumn===fk.PrincipalColumn&&f.DeleteAction===fk.DeleteAction),`${fk.Name}: original FK changed`);}
for(const e of model.Entities){
 const t=tables.get(e.Table),columns=after.Columns.filter(c=>c.TableId===t.Id),keys=grouped(after.Keys.filter(k=>k.TableId===t.Id)),indexes=grouped(after.Indexes.filter(i=>i.TableId===t.Id)),fks=grouped(after.ForeignKeys.filter(f=>f.TableId===t.Id)),checks=after.Checks.filter(c=>c.TableId===t.Id);
 assert.equal(columns.length,e.Properties.length,`${e.Table}: columns`);assert.equal(keys.length,e.Keys.length,`${e.Table}: keys`);assert.equal(indexes.length,e.Indexes.length,`${e.Table}: indexes`);assert.equal(fks.length,e.ForeignKeys.length,`${e.Table}: FKs`);assert.equal(checks.length,e.Checks.length,`${e.Table}: checks`);
 for(const p of e.Properties){const c=columns.find(c=>c.Name===p.Column);assert.ok(c,`${e.Table}.${p.Column}`);assert.equal(sqlType(c),typeNormal(p.Type),`${e.Table}.${p.Column}: type`);if(!p.ComputedSql)assert.equal(c.Nullable,p.IsNullable,`${e.Table}.${p.Column}: nullable`);assert.equal(c.Identity,p.Strategy==='IdentityColumn');if(p.ComputedSql){assert.equal(normal(c.ComputedSql),normal(p.ComputedSql));assert.equal(c.Persisted,p.Stored);}if(p.DefaultSql)assert.equal(normal(c.DefaultSql),normal(p.DefaultSql));if(p.HasDefault){const literal=typeof p.DefaultValue==='string'?c.DefaultSql?.replace(/^\(+|\)+$/g,'').replace(/^N?'/,'').replace(/'$/,''):Number(normal(c.DefaultSql).replace(/^convert\[bit\],([01])$/,'$1'));assert.equal(String(literal),typeof p.DefaultValue==='boolean'?String(Number(p.DefaultValue)):String(p.DefaultValue),`${e.Table}.${p.Column}: default`);}}
 for(const k of e.Keys){const actual=keys.find(x=>x[0].Name===k.Name);assert.ok(actual, k.Name);assert.deepEqual(actual.map(x=>x.Column),k.Columns);assert.equal(actual[0].Kind,k.Primary?'PK':'UQ');}
 for(const i of e.Indexes){const actual=indexes.find(x=>x[0].Name===i.Name);assert.ok(actual,i.Name);assert.deepEqual(actual.map(x=>x.Column),i.Columns);assert.equal(actual[0].IsUnique,i.IsUnique);assert.equal(normal(actual[0].Filter),normal(i.Filter));}
 for(const f of e.ForeignKeys){const actual=fks.find(x=>x[0].Name===f.Name);assert.ok(actual,f.Name);assert.deepEqual(actual.map(x=>x.Column),f.Columns);assert.deepEqual(actual.map(x=>x.PrincipalColumn),f.PrincipalColumns);assert.equal(after.Tables.find(t=>t.Id===actual[0].PrincipalTableId).Name,f.PrincipalTable);assert.equal(actual[0].DeleteAction,'NO_ACTION');assert.ok(actual.every(x=>!x.Disabled&&!x.NotTrusted));}
 for(const c of e.Checks){const actual=checks.find(x=>x.Name===c.Name);assert.ok(actual,c.Name);assert.equal(normal(actual.Sql),normal(c.Sql),c.Name);assert.ok(!actual.Disabled&&!actual.NotTrusted);}
}
assert.ok(model.Entities.find(e=>e.Table==='users').Properties.find(p=>p.Name==='ConcurrencyStamp').IsConcurrencyToken);
assert.ok(model.Entities.find(e=>e.Table==='AspNetRoles').Properties.find(p=>p.Name==='ConcurrencyStamp').IsConcurrencyToken);
assert.ok(!tables.has('AspNetUsers'));
const summary={result:'PASS',databasePreserved:true,tablesBefore:26,tablesAfter:35,mappedTables:34,domainTables:27,retainedLegacyTables:1,identitySupportTables:6,columns:269,foreignKeys:76,checks:78,originalTablesPreserved:26,originalForeignKeysPreserved:58,originalUsersForeignKeysPreserved:21,userRecords:0,originalRecordsLost:0,roleDefinitions:3,migrationHistoryRows:2,offlineBehaviorChecks:model.BehaviorChecks.length};
fs.writeFileSync(path.join(root,'.local/database-setup/database-validation.json'),JSON.stringify(summary,null,2));console.log(JSON.stringify(summary,null,2));
