param([string]$OutputPath = '.local/database-setup/schema-before.json')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
$connection = New-Object System.Data.SqlClient.SqlConnection 'Server=localhost;Database=Kicks;Trusted_Connection=True;TrustServerCertificate=True;'
$connection.Open()
try {
    function Read-Rows([string]$Sql) {
        $command = $connection.CreateCommand()
        $command.CommandText = $Sql
        $reader = $command.ExecuteReader()
        $rows = New-Object 'System.Collections.Generic.List[object]'
        try {
            while ($reader.Read()) {
                $row = [ordered]@{}
                for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                    $row[$reader.GetName($i)] = if ($reader.IsDBNull($i)) { $null } else { $reader.GetValue($i) }
                }
                $rows.Add([pscustomobject]$row)
            }
        } finally { $reader.Dispose(); $command.Dispose() }
        return ,$rows.ToArray()
    }
    $snapshot = [ordered]@{}
    $snapshot.Database = Read-Rows "SELECT DB_ID() AS Id, DB_NAME() AS Name, CONVERT(nvarchar(128),DATABASEPROPERTYEX(DB_NAME(),'Collation')) AS Collation"
    $snapshot.Tables = Read-Rows 'SELECT t.object_id AS Id,s.name AS [Schema],t.name AS Name FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE t.is_ms_shipped=0 ORDER BY s.name,t.name'
    foreach ($table in $snapshot.Tables) {
        $qualified = '[' + $table.Schema.Replace(']',']]') + '].[' + $table.Name.Replace(']',']]') + ']'
        $count = Read-Rows "SELECT COUNT_BIG(*) AS Records FROM $qualified"
        $table | Add-Member -NotePropertyName Records -NotePropertyValue $count[0].Records
    }
    $snapshot.Columns = Read-Rows 'SELECT c.object_id AS TableId,c.column_id AS Ordinal,c.name AS Name,ty.name AS SqlType,c.max_length AS MaxLength,c.precision AS [Precision],c.scale AS Scale,c.is_nullable AS Nullable,c.is_identity AS [Identity],CONVERT(bigint,ic.seed_value) AS IdentitySeed,CONVERT(bigint,ic.increment_value) AS IdentityIncrement,dc.definition AS DefaultSql,cc.definition AS ComputedSql,cc.is_persisted AS Persisted FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id LEFT JOIN sys.default_constraints dc ON dc.object_id=c.default_object_id LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id LEFT JOIN sys.identity_columns ic ON ic.object_id=c.object_id AND ic.column_id=c.column_id WHERE t.is_ms_shipped=0 ORDER BY c.object_id,c.column_id'
    $snapshot.Keys = Read-Rows 'SELECT k.parent_object_id AS TableId,k.name AS Name,k.type AS Kind,c.name AS [Column],ic.key_ordinal AS Ordinal,i.type_desc AS IndexType FROM sys.key_constraints k JOIN sys.indexes i ON i.object_id=k.parent_object_id AND i.index_id=k.unique_index_id JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id ORDER BY k.parent_object_id,k.name,ic.key_ordinal'
    $snapshot.Indexes = Read-Rows 'SELECT i.object_id AS TableId,i.name AS Name,i.is_unique AS IsUnique,i.filter_definition AS Filter,i.type_desc AS IndexType,c.name AS [Column],ic.key_ordinal AS Ordinal,ic.is_descending_key AS Descending,ic.is_included_column AS Included FROM sys.indexes i JOIN sys.tables t ON t.object_id=i.object_id JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE t.is_ms_shipped=0 AND i.is_primary_key=0 AND i.is_unique_constraint=0 AND i.type>0 ORDER BY i.object_id,i.name,ic.key_ordinal'
    $snapshot.ForeignKeys = Read-Rows 'SELECT fk.object_id AS Id,fk.parent_object_id AS TableId,fk.name AS Name,c.name AS [Column],fk.referenced_object_id AS PrincipalTableId,pc.name AS PrincipalColumn,fkc.constraint_column_id AS Ordinal,fk.delete_referential_action_desc AS DeleteAction,fk.update_referential_action_desc AS UpdateAction,fk.is_disabled AS Disabled,fk.is_not_trusted AS NotTrusted FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id=fk.object_id JOIN sys.columns c ON c.object_id=fkc.parent_object_id AND c.column_id=fkc.parent_column_id JOIN sys.columns pc ON pc.object_id=fkc.referenced_object_id AND pc.column_id=fkc.referenced_column_id ORDER BY fk.parent_object_id,fk.name,fkc.constraint_column_id'
    $snapshot.Checks = Read-Rows 'SELECT parent_object_id AS TableId,name AS Name,definition AS Sql,is_disabled AS Disabled,is_not_trusted AS NotTrusted FROM sys.check_constraints ORDER BY parent_object_id,name'
    $absolute = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputPath))
    [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($absolute)) | Out-Null
    $snapshot | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $absolute -Encoding UTF8
    [pscustomobject]@{ Tables=$snapshot.Tables.Count; Records=($snapshot.Tables | Measure-Object Records -Sum).Sum; ForeignKeys=@($snapshot.ForeignKeys.Name | Select-Object -Unique).Count; Output=$absolute } | ConvertTo-Json
} finally { $connection.Dispose() }
