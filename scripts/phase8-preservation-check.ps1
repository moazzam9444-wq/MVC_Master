$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$evidence=Join-Path $root '.local/phase8'
& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'phase8-database-safety.ps1') -VerifyOriginal
if($LASTEXITCODE -ne 0){throw 'Original database preservation check failed.'}
function Verify-Files([string]$manifest,[string[]]$roots,[switch]$Force,[switch]$AllowHostCaptures) {
    $before=@(Get-Content -LiteralPath (Join-Path $evidence $manifest) -Raw | ConvertFrom-Json)
    $files=@(Get-ChildItem -LiteralPath $roots -Recurse -File -Force:$Force -ErrorAction Stop)
    $known=@{};foreach($item in $before){$known[$item.Path]=$true}
    $added=@($files | Where-Object {!$known.ContainsKey($_.FullName)})
    if($added.Count){
        $captureRoot=[IO.Path]::GetFullPath((Join-Path $roots[0] '.git/refs/codex/turn-diffs/captures'))+[IO.Path]::DirectorySeparatorChar
        if(!$AllowHostCaptures -or @($added|Where-Object{!$_.FullName.StartsWith($captureRoot,[StringComparison]::OrdinalIgnoreCase)}).Count){throw "Unexpected additional file: $manifest"}
        $script:hostMetadata=@($added|ForEach-Object FullName)
    }
    if($files.Count -ne $before.Count+$added.Count){throw "Protected file removed: $manifest"}
    foreach($item in $before){if(!(Test-Path -LiteralPath $item.Path) -or (Get-FileHash -LiteralPath $item.Path -Algorithm SHA256).Hash -cne $item.Hash){throw "Protected file changed: $($item.Path)"}}
    return $before.Count
}
$hostMetadata=@()
$staticCount=Verify-Files 'static-before.json' @((Join-Path (Split-Path $root -Parent) 'Kicks')) -Force -AllowHostCaptures
$privateCount=Verify-Files 'private-files-before.json' @((Join-Path $root 'appsettings.Development.json'),(Join-Path $root 'App_Data'))
$s=Get-Content -LiteralPath (Join-Path $evidence 'database-safety.json') -Raw | ConvertFrom-Json
if($s.Database -cnotmatch '^Kicks_BookingTest_\d{8}_[a-f0-9]{8}$'){throw 'Unexpected isolated database name.'}
if((Get-FileHash -LiteralPath $s.Backup -Algorithm SHA256).Hash -cne $s.SHA256){throw 'Backup SHA-256 mismatch.'}
Add-Type -AssemblyName System.Data
function Schema-Hash([string]$database){
    $c=[Data.SqlClient.SqlConnection]::new("Server=localhost;Database=$database;Integrated Security=True;TrustServerCertificate=True");$c.Open()
    try{
        $parts=@()
        $queries=@(
            'SELECT t.name AS [Table],i.name,i.type,i.is_unique,i.filter_definition,ic.index_column_id,c.name AS [Column],ic.is_included_column,ic.is_descending_key FROM sys.tables t JOIN sys.indexes i ON i.object_id=t.object_id LEFT JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id LEFT JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id ORDER BY t.name,i.name,ic.index_column_id FOR JSON PATH,INCLUDE_NULL_VALUES',
            'SELECT OBJECT_NAME(parent_object_id) AS [Table],name,definition,is_disabled,is_not_trusted FROM sys.check_constraints ORDER BY OBJECT_NAME(parent_object_id),name FOR JSON PATH,INCLUDE_NULL_VALUES',
            'SELECT fk.name,OBJECT_NAME(fkc.parent_object_id) AS [Table],fkc.parent_column_id,OBJECT_NAME(fkc.referenced_object_id) AS PrincipalTable,fkc.referenced_column_id,fkc.constraint_column_id,fk.delete_referential_action,fk.update_referential_action FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id=fk.object_id ORDER BY fk.name,fkc.constraint_column_id FOR JSON PATH,INCLUDE_NULL_VALUES'
        )
        foreach($sql in $queries){$cmd=$c.CreateCommand();$cmd.CommandText=$sql;$reader=$cmd.ExecuteReader();$text=[Text.StringBuilder]::new();try{while($reader.Read()){[void]$text.Append($reader.GetString(0))}}finally{$reader.Dispose();$cmd.Dispose()};$parts+=$text.ToString()}
        $cmd=$c.CreateCommand();$cmd.CommandTimeout=300;$cmd.CommandText='DBCC CHECKDB WITH NO_INFOMSGS, ALL_ERRORMSGS';[void]$cmd.ExecuteNonQuery();$cmd.Dispose()
        return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes(($parts -join '|'))))
    }finally{$c.Dispose()}
}
$originalSchema=Schema-Hash 'Kicks';$testSchema=Schema-Hash $s.Database
if($originalSchema -cne $testSchema){throw 'Restored index/check/FK-column schema differs from original.'}
$record=[ordered]@{OriginalDatabaseUnchanged=$true;OriginalStaticFilesUnchanged=$staticCount;AdditionalHostGitMetadataFiles=$hostMetadata;PrivateApplicationFilesUnchanged=$privateCount;BackupSHA256Verified=$true;OriginalCheckDb=$true;IsolatedCheckDb=$true;IndexesChecksAndForeignKeyColumnsMatch=$true;AdditionalSchemaSHA256=$originalSchema}
$record|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $evidence 'preservation-final.json')
$record|ConvertTo-Json
