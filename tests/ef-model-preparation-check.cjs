// Offline SQL-design/model parity checks. Requires an existing Release build; never builds or connects to SQL Server.
'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { spawnSync } = require('node:child_process');
const root = path.resolve(__dirname, '..');
const sql = fs.readFileSync(path.join(root, 'database/KICKS-schema.sql'), 'utf8');
const run = spawnSync('dotnet', [path.join(root, 'bin/Release/net10.0/KicksMvc.dll'), '--verify-model-details', '--verify-model-behavior'], {
  cwd: root, encoding: 'utf8', timeout: 60000
});
assert.equal(run.status, 0, `Offline model inspection failed: ${run.stderr || run.stdout}`);
const model = JSON.parse(run.stdout);
const columnsIn = text => [...text.matchAll(/\[([^\]]+)\]/g)].map(m => m[1]);
const same = (a, b) => JSON.stringify(a) === JSON.stringify(b);
const tables = [...sql.matchAll(/CREATE TABLE \[dbo\]\.\[(\w+)\] \(\r?\n([\s\S]*?)\r?\n\);/g)];
const entities = new Map(model.Entities.map(e => [e.Table, e]));
const totals = { columns: 0, primaryKeys: 0, foreignKeys: 0, checks: 0, uniqueGuarantees: 0, indexesCovered: 0, computed: 0, literalDefaults: 0, generatedDefaults: 0 };
assert.equal(model.Tables, 34);
assert.equal(tables.length, 27);
assert.equal(model.Columns, 270);
assert.equal(model.Relationships, 76);
assert.equal(model.DbSets.length, 34);
assert.deepEqual([...model.DbSets].sort(), model.Entities.map(e => e.Entity).sort());
for (const [_, name, body] of tables) {
  const e = entities.get(name);
  assert.ok(e, `Missing table mapping ${name}`);
  assert.equal(e.Schema, 'dbo');
  const properties = new Map(e.Properties.map(p => [p.Column, p]));
  // Approved Identity transition: retired custom credentials/role; inherited standard hash column.
  const retired = new Set(['password_salt','password_algorithm','password_iterations','role']);
  const columns = [...body.matchAll(/^    \[([^\]]+)\] ([^\r\n]+)/gm)]
    .filter(c => name !== 'users' || !retired.has(c[1]));
  assert.equal(properties.size, columns.length + (name === 'users' ? 11 : name === 'bookings' ? 1 : 0), `${name}: unexpected property/column count`);
  // The SQL export remains the historical baseline. Check the reviewed additive migration explicitly.
  if(name==='bookings'){
    const duration=properties.get('duration_minutes');
    assert(duration && duration.Type==='int' && !duration.IsNullable && duration.DefaultValue===60 && duration.InitialValue===60 && duration.Generated==='Never');
    totals.columns++;totals.literalDefaults++;
    for(const [check,expression] of [
      ['CK_bookings_duration_minutes','[duration_minutes] IN (60,120)'],
      ['CK_bookings_closing_time','[ends_at] <= TODATETIMEOFFSET(DATEADD(HOUR,23,CONVERT(datetime2(0),CONVERT(date,[starts_at]))),180)']
    ]){assert(e.Checks.some(c=>c.Name===check&&c.Sql===expression));totals.checks++;}
  }
  assert.ok(e.Properties.every(p => !p.Shadow), `${name}: accidental shadow property`);
  for (const [__, column, originalDefinition] of columns) {
    const definition = name === 'users' && column === 'password_hash' ? 'nvarchar(max) NULL' : originalDefinition;
    totals.columns++;
    const p = properties.get(column);
    assert.ok(p, `${name}.${column}: missing property`);
    if (definition.startsWith('AS ')) {
      totals.computed++;
      assert.equal(p.ComputedSql, name==='bookings'&&column==='ends_at'?'DATEADD(MINUTE, [duration_minutes], [starts_at])':/^AS \((.+)\) PERSISTED/.exec(definition)[1]);
      assert.equal(p.Stored, true);
      assert.equal(p.Generated, 'OnAddOrUpdate');
      assert.equal(p.ClrType, 'System.DateTimeOffset');
      continue;
    }
    const sqlType = /^\w+(?:\([^)]+\))?/.exec(definition)[0];
    assert.equal(p.Type, sqlType, `${name}.${column}: SQL type`);
    const nullable = !definition.includes('NOT NULL');
    assert.equal(p.IsNullable, nullable, `${name}.${column}: nullability`);
    const clrType = sqlType === 'int' ? 'System.Int32' : sqlType === 'bit' ? 'System.Boolean'
      : sqlType.startsWith('decimal') ? 'System.Decimal' : sqlType.startsWith('datetimeoffset') ? 'System.DateTimeOffset' : 'System.String';
    assert.ok(nullable && clrType !== 'System.String' ? p.ClrType.startsWith('System.Nullable`1') && p.ClrType.includes(clrType) : p.ClrType === clrType,
      `${name}.${column}: CLR type`);
    assert.equal(p.Strategy === 'IdentityColumn', /\bIDENTITY\(/.test(definition), `${name}.${column}: identity strategy`);
    const def = /\bDEFAULT \((.+)\),?$/.exec(definition);
    if (def) {
      if (def[1] === 'SYSUTCDATETIME()') {
        totals.generatedDefaults++;
        assert.equal(p.DefaultSql, def[1]);
        assert.equal(p.Generated, 'OnAdd');
      } else {
        totals.literalDefaults++;
        const expected = def[1].startsWith("'") ? def[1].slice(1, -1) : sqlType === 'bit' ? def[1] === '1' : Number(def[1]);
        assert.equal(p.HasDefault, true);
        assert.equal(p.DefaultValue, expected, `${name}.${column}: database default`);
        assert.equal(p.InitialValue, expected, `${name}.${column}: CLR initial value`);
        assert.equal(p.DefaultSql, null);
        assert.equal(p.Generated, 'Never', `${name}.${column}: explicit zero/false must not be omitted`);
      }
    } else {
      assert.equal(p.HasDefault, false, `${name}.${column}: invented database default`);
      assert.equal(p.DefaultSql, null);
    }
  }
  const primaryKeys = [...body.matchAll(/CONSTRAINT \[([^\]]+)\] PRIMARY KEY \(([^\n]+)\)/g)];
  assert.equal(e.Keys.filter(k => k.Primary).length, primaryKeys.length);
  for (const [__, keyName, cols] of primaryKeys) {
    totals.primaryKeys++;
    assert.ok(e.Keys.some(k => k.Primary && k.Name === keyName && same(k.Columns, columnsIn(cols))), `${keyName}: primary key mismatch`);
  }
  const checks = [...body.matchAll(/CONSTRAINT \[([^\]]+)\] CHECK \(([^\n]+)\)/g)]
    .filter(c => !['CK_users_password_iterations','CK_users_role_enum'].includes(c[1]));
  assert.equal(e.Checks.length, checks.length+(name==='bookings'?2:0));
  for (const [__, checkName, expression] of checks) {
    totals.checks++;
    const expected=checkName==='CK_bookings_rule_4'?'DATEPART(TZOFFSET,[starts_at]) = 180 AND DATEPART(HOUR,[starts_at]) BETWEEN 13 AND 22':expression;
    assert.ok(e.Checks.some(c => c.Name === checkName && c.Sql === expected), `${checkName}: weakened/missing check`);
  }
  const foreignKeys = [...body.matchAll(/CONSTRAINT \[([^\]]+)\] FOREIGN KEY \(([^\n]+?)\) REFERENCES \[dbo\]\.\[(\w+)\] \(([^\n]+?)\) ON DELETE NO ACTION/g)];
  assert.equal(e.ForeignKeys.length, foreignKeys.length, `${name}: duplicated/missing FK`);
  for (const [__, fkName, cols, principal, principalCols] of foreignKeys) {
    totals.foreignKeys++;
    // Existing SQL Server FK names were retained where the old schema used another suffix.
    const fk = e.ForeignKeys.find(f => f.Name === fkName) || e.ForeignKeys.find(f =>
      f.PrincipalTable === principal && same(f.Columns, columnsIn(cols)) && same(f.PrincipalColumns, columnsIn(principalCols)));
    assert.ok(fk, `${fkName}: missing foreign key`);
    assert.deepEqual(fk.Columns, columnsIn(cols));
    assert.equal(fk.PrincipalTable, principal);
    assert.deepEqual(fk.PrincipalColumns, columnsIn(principalCols));
    assert.equal(fk.DeleteBehavior, 'NoAction');
    assert.equal(fk.IsRequired, columnsIn(cols).every(c => !properties.get(c).IsNullable));
  }
  for (const [__, keyName, cols] of body.matchAll(/CONSTRAINT \[([^\]]+)\] UNIQUE \(([^\n]+)\)/g)) {
    totals.uniqueGuarantees++;
    const expected = columnsIn(cols);
    assert.ok(e.Keys.some(k => k.Name === keyName && same(k.Columns, expected))
      || e.Indexes.some(i => i.Name === keyName && i.IsUnique && !i.Filter && same(i.Columns, expected)), `${keyName}: uniqueness lost`);
  }
  const namedIndexes = new Set();
  for (const index of e.Indexes) {
    assert.ok(!namedIndexes.has(index.Name), `${name}: duplicate physical index name`);
    namedIndexes.add(index.Name);
    assert.ok(index.Columns.every(c => properties.has(c)));
  }
  for (const navigation of e.Navigations) {
    const target = model.Entities.find(t => t.Entity === navigation.Target);
    assert.ok(target, `${name}.${navigation.Name}: missing target`);
    if (navigation.Inverse) assert.ok(target.Navigations.some(n => n.Name === navigation.Inverse && n.Inverse === navigation.Name), `${name}.${navigation.Name}: inverse mismatch`);
  }
}
const sqlIndexes = [...sql.matchAll(/CREATE (UNIQUE )?INDEX \[([^\]]+)\] ON \[dbo\]\.\[([^\]]+)\] \(([^\r\n]+?)\)(?: WHERE ([^;]+))?;/g)];
const coveredInsteadOfSeparate = [];
for (const [_, unique, name, table, cols, filter] of sqlIndexes) {
  const e = entities.get(table), columns = columnsIn(cols);
  const exact = e.Indexes.find(i => same(i.Columns, columns) && i.IsUnique === !!unique && (i.Filter || null) === (filter || null));
  const covering = !unique && !filter && [...e.Keys, ...e.Indexes.filter(i => !i.Filter)].find(k => columns.every((c, n) => k.Columns[n] === c));
  assert.ok(exact || covering, `${name}: important index/filter missing`);
  if (exact) assert.equal(exact.Name, name, `${name}: unexpected SQL index naming drift`);
  else coveredInsteadOfSeparate.push({ table, index: name, columns, coveredBy: covering.Name });
  totals.indexesCovered++;
}
const alternateKeys = model.Entities.flatMap(e => e.Keys.filter(k => !k.Primary).map(k => k.Name)).sort();
assert.deepEqual(alternateKeys, [
  'UQ_booking_teams_booking_id_team_id_captain_id',
  'UQ_tournament_matches_tournament_id_id',
  'UQ_tournament_registrations_tournament_id_id'
]);
assert.deepEqual(totals, {columns:227,primaryKeys:27,foreignKeys:68,checks:77,uniqueGuarantees:15,indexesCovered:60,computed:3,literalDefaults:37,generatedDefaults:16});
const user = entities.get('users');
for (const retired of ['password_salt','password_algorithm','password_iterations','role'])
  assert.ok(!user.Properties.some(p => p.Column === retired));
assert.ok(!entities.has('AspNetUsers'));
assert.ok(entities.has('meetings'));
assert.equal(user.Properties.find(p=>p.Name==='Id').ClrType,'System.Int32');
for (const table of ['AspNetRoles','AspNetUserRoles','AspNetUserClaims','AspNetRoleClaims','AspNetUserLogins','AspNetUserTokens']) {
  assert.ok(entities.has(table));
  assert.ok(entities.get(table).ForeignKeys.every(f=>f.DeleteBehavior==='NoAction'));
}
assert.ok(entities.get('AspNetUserRoles').Indexes.some(i=>i.IsUnique&&same(i.Columns,['UserId'])));
assert.ok(model.Entities.every(e=>e.Properties.every(p=>!p.Shadow)));
assert.equal(model.BehaviorChecks.length, 22);
const summary = {
  result: 'PASS: domain SQL parity with approved Identity transition and legacy preservation', tables: model.Tables, dbSets: model.DbSets.length, ...totals,
  alternateKeys: alternateKeys.length, uniqueIndexes: model.Entities.reduce((n,e) => n+e.Indexes.filter(i=>i.IsUnique).length,0),
  modelIndexes: model.Entities.reduce((n,e)=>n+e.Indexes.length,0),
  navigations: model.Entities.reduce((n,e)=>n+e.Navigations.length,0), behaviorChecks: model.BehaviorChecks.length,
  shadowProperties: 0, coveredInsteadOfSeparate,
  limitation: 'No SQL connection, SQL execution, migration generation/application, or SaveChanges. SQL Server runtime behavior remains untested.'
};
const outputDir = path.join(root, '.local/ef-preparation');
fs.mkdirSync(outputDir,{recursive:true});
fs.writeFileSync(path.join(outputDir,'model-after.json'), JSON.stringify(model,null,2));
fs.writeFileSync(path.join(outputDir,'validation.json'), JSON.stringify(summary,null,2));
console.log(JSON.stringify(summary,null,2));
