param([switch]$VerifyOriginal)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
$root = Split-Path $PSScriptRoot -Parent
$evidence = Join-Path $root '.local/phase8'
[void][IO.Directory]::CreateDirectory($evidence)
function Connect([string]$database) {
    $c = [System.Data.SqlClient.SqlConnection]::new("Server=localhost;Database=$database;Integrated Security=True;TrustServerCertificate=True")
    $c.Open(); return $c
}
function Rows($c, [string]$sql) {
    $cmd=$c.CreateCommand(); $cmd.CommandText=$sql; $cmd.CommandTimeout=300
    $reader=$cmd.ExecuteReader(); $rows=[Collections.Generic.List[object]]::new()
    try { while($reader.Read()) { $row=[ordered]@{}; for($i=0;$i -lt $reader.FieldCount;$i++){ $row[$reader.GetName($i)]=if($reader.IsDBNull($i)){$null}else{$reader.GetValue($i)} }; $rows.Add([pscustomobject]$row) } }
    finally { $reader.Dispose(); $cmd.Dispose() }; return ,$rows.ToArray()
}
function Hash([string]$value) { return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($value))) }
function Snapshot([string]$database) {
    $c=Connect $database
    try {
        $tables=Rows $c "SELECT QUOTENAME(s.name)+'.'+QUOTENAME(t.name) AS Qualified FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id ORDER BY s.name,t.name"
        $data=@(foreach($t in $tables){
            $keys=Rows $c "SELECT QUOTENAME(c.name) AS Name FROM sys.indexes i JOIN sys.index_columns ic ON i.object_id=ic.object_id AND i.index_id=ic.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.is_primary_key=1 AND i.object_id=OBJECT_ID(N'$($t.Qualified)') ORDER BY ic.key_ordinal"
            $order=if($keys.Count){' ORDER BY '+(($keys | ForEach-Object Name)-join ',')}else{''}
            $json=((Rows $c "SELECT * FROM $($t.Qualified)$order FOR JSON PATH, INCLUDE_NULL_VALUES") | ForEach-Object { $_.PSObject.Properties.Value }) -join ''
            $count=(Rows $c "SELECT COUNT_BIG(*) AS N FROM $($t.Qualified)")[0].N
            [pscustomobject]@{Table=$t.Qualified;Rows=$count;SHA256=(Hash $json)}
        })
        $schema=(Rows $c "SELECT t.name AS [Table],c.column_id,c.name,ty.name AS [Type],c.max_length,c.precision,c.scale,c.is_nullable,c.is_identity,dc.definition FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id LEFT JOIN sys.default_constraints dc ON dc.object_id=c.default_object_id ORDER BY t.name,c.column_id FOR JSON PATH,INCLUDE_NULL_VALUES" | ForEach-Object { $_.PSObject.Properties.Value }) -join ''
        $relations=(Rows $c "SELECT name,parent_object_id,referenced_object_id,delete_referential_action,update_referential_action,is_disabled,is_not_trusted FROM sys.foreign_keys ORDER BY name FOR JSON PATH,INCLUDE_NULL_VALUES" | ForEach-Object { $_.PSObject.Properties.Value }) -join ''
        $identity=Rows $c 'SELECT OBJECT_NAME(object_id) AS [Table],name,CONVERT(nvarchar(100),last_value) AS LastValue FROM sys.identity_columns ORDER BY OBJECT_NAME(object_id),name'
        return [pscustomobject]@{Tables=$data;SchemaSHA256=(Hash $schema);RelationshipsSHA256=(Hash $relations);Identity=$identity}
    } finally {$c.Dispose()}
}
$beforeFile=Join-Path $evidence 'original-before.json'
if($VerifyOriginal){
    if(!(Test-Path -LiteralPath $beforeFile)){throw 'Missing original preservation checkpoint.'}
    $before=Get-Content -LiteralPath $beforeFile -Raw | ConvertFrom-Json
    $after=Snapshot 'Kicks'; $after | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $evidence 'original-after.json')
    if(($before | ConvertTo-Json -Depth 10 -Compress) -cne ($after | ConvertTo-Json -Depth 10 -Compress)){throw 'Original database fingerprint changed; inspect private evidence.'}
    Write-Output 'PASS: Original Kicks schema, every table SHA-256, relationships and identity counters unchanged.'; exit
}
if(Test-Path -LiteralPath (Join-Path $evidence 'database-safety.json')){throw 'A verified Phase 8 backup/restore checkpoint already exists. Use -VerifyOriginal; do not overwrite its evidence.'}
if(Test-Path -LiteralPath $beforeFile){
    $before=Get-Content -LiteralPath $beforeFile -Raw | ConvertFrom-Json
    $current=Snapshot 'Kicks'
    if(($before | ConvertTo-Json -Depth 10 -Compress) -cne ($current | ConvertTo-Json -Depth 10 -Compress)){throw 'Original changed since checkpoint.'}
}else{$before=Snapshot 'Kicks'; $before | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $beforeFile}
$id=(Get-Date -Format yyyyMMdd)+'_'+[Guid]::NewGuid().ToString('N').Substring(0,8)
$testDb='Kicks_BookingTest_'+$id
$master=Connect 'master'
try {
    $paths=(Rows $master "SELECT CONVERT(nvarchar(260),SERVERPROPERTY('InstanceDefaultBackupPath')) AS BackupPath,CONVERT(nvarchar(260),SERVERPROPERTY('InstanceDefaultDataPath')) AS DataPath")[0]
    $safety=Join-Path (Split-Path $root -Parent) "KicksMvc-safety/phase8-$id"
    [void][IO.Directory]::CreateDirectory($safety)
    & icacls $safety /grant 'NT SERVICE\MSSQLSERVER:(OI)(CI)F' | Out-Null
    if($LASTEXITCODE -ne 0){throw 'Could not grant SQL service access to new backup directory.'}
    $backup=Join-Path $safety "Kicks-Phase8-$id.bak"
    $cmd=$master.CreateCommand(); $cmd.CommandTimeout=300
    $literal=$backup.Replace("'","''")
    $cmd.CommandText="BACKUP DATABASE [Kicks] TO DISK=N'$literal' WITH COPY_ONLY,CHECKSUM,COMPRESSION; RESTORE VERIFYONLY FROM DISK=N'$literal' WITH CHECKSUM;"
    [void]$cmd.ExecuteNonQuery()
    $digest=(Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash
    $files=Rows $master "SELECT name,type FROM Kicks.sys.database_files ORDER BY file_id"
    $moves=@(foreach($f in $files){$extension=if($f.type -eq 1){'.ldf'}else{'.mdf'}; $target=Join-Path $paths.DataPath ($testDb+'_'+$f.name+$extension); if(Test-Path -LiteralPath $target){throw 'Restore target already exists.'}; "MOVE N'$($f.name.Replace("'","''"))' TO N'$($target.Replace("'","''"))'"})
    $cmd.CommandText="RESTORE DATABASE [$testDb] FROM DISK=N'$literal' WITH CHECKSUM,"+($moves -join ',')+"; DBCC CHECKDB ([$testDb]) WITH NO_INFOMSGS, ALL_ERRORMSGS;"
    [void]$cmd.ExecuteNonQuery(); $cmd.Dispose()
    $restored=Snapshot $testDb
    if(($before | ConvertTo-Json -Depth 10 -Compress) -cne ($restored | ConvertTo-Json -Depth 10 -Compress)){throw 'Restored data/schema/relationships mismatch.'}
    $record=[ordered]@{Database=$testDb;Connection="Server=localhost;Database=$testDb;Integrated Security=True;TrustServerCertificate=True";Backup=$backup;SHA256=$digest;VerifyOnly=$true;CheckDb=$true;RestoredFingerprintsMatch=$true}
    $record | ConvertTo-Json | Set-Content (Join-Path $evidence 'database-safety.json')
    $record | ConvertTo-Json
} finally {$master.Dispose()}
