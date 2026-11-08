param(
    [Parameter(Mandatory)][ValidatePattern('^auth-browser-[a-f0-9]{32}$')][string]$Marker,
    [Parameter(Mandatory)][ValidateSet('Inspect','Approve','Deactivate','Cleanup')][string]$Action
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
$connection = New-Object System.Data.SqlClient.SqlConnection 'Server=localhost;Database=Kicks;Trusted_Connection=True;TrustServerCertificate=True;'
$connection.Open()
try {
    $command = $connection.CreateCommand()
    $command.Parameters.Add('@Player',[System.Data.SqlDbType]::NVarChar,254).Value = "$Marker-player@example.test"
    $command.Parameters.Add('@Owner',[System.Data.SqlDbType]::NVarChar,254).Value = "$Marker-owner@example.test"
    $command.CommandText = switch ($Action) {
        'Inspect' { 'SELECT u.id,u.email,u.status,u.requested_role,u.reviewed_by,u.reviewed_at,r.Name AS role,(SELECT COUNT(*) FROM dbo.AspNetUserRoles allRoles WHERE allRoles.UserId=u.id) AS role_count,CASE WHEN u.password_hash IS NOT NULL AND LEN(u.password_hash)>0 THEN 1 ELSE 0 END AS has_password_hash,d.id AS document_id,d.storage_url FROM dbo.users u LEFT JOIN dbo.AspNetUserRoles ur ON ur.UserId=u.id LEFT JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId LEFT JOIN dbo.ownership_documents d ON d.user_id=u.id WHERE u.email IN (@Player,@Owner) ORDER BY u.email' }
        'Approve' { @'
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF EXISTS(SELECT 1 FROM dbo.users WHERE email IN (@Player,@Owner) AND status <> 'Pending') THROW 51000,'Test fixture must be pending.',1;
UPDATE dbo.users SET status='Active',concurrency_stamp=CONVERT(nvarchar(36),NEWID()),security_stamp=CONVERT(nvarchar(36),NEWID()) WHERE email IN (@Player,@Owner);
INSERT dbo.AspNetUserRoles(UserId,RoleId) SELECT u.id,r.Id FROM dbo.users u JOIN dbo.AspNetRoles r ON r.Name=u.requested_role WHERE u.email IN (@Player,@Owner);
COMMIT;
'@ }
        'Deactivate' { "UPDATE dbo.users SET status='Inactive',concurrency_stamp=CONVERT(nvarchar(36),NEWID()),security_stamp=CONVERT(nvarchar(36),NEWID()) WHERE email=@Owner" }
        'Cleanup' { @'
SET XACT_ABORT ON;
BEGIN TRANSACTION;
SELECT storage_url FROM dbo.ownership_documents WHERE user_id IN (SELECT id FROM dbo.users WHERE email IN (@Player,@Owner));
DELETE FROM dbo.AspNetUserRoles WHERE UserId IN (SELECT id FROM dbo.users WHERE email IN (@Player,@Owner));
DELETE FROM dbo.ownership_documents WHERE user_id IN (SELECT id FROM dbo.users WHERE email IN (@Player,@Owner));
DELETE FROM dbo.users WHERE email IN (@Player,@Owner);
COMMIT;
'@ }
    }
    if ($Action -in 'Inspect','Cleanup') {
        $reader=$command.ExecuteReader()
        $rows=New-Object 'System.Collections.Generic.List[object]'
        try {
            while($reader.Read()) {
                $row=[ordered]@{}
                for($i=0;$i -lt $reader.FieldCount;$i++) { $row[$reader.GetName($i)]=if($reader.IsDBNull($i)){$null}else{$reader.GetValue($i)} }
                $rows.Add([pscustomobject]$row)
            }
        } finally { $reader.Dispose() }
        if($Action -eq 'Cleanup') {
            $privateRoot=[System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../App_Data/ownership'))
            foreach($row in $rows) {
                if($row.storage_url -notmatch '^[a-f0-9]{32}\.bin$') { throw 'Unexpected test document key.' }
                Remove-Item -LiteralPath (Join-Path $privateRoot $row.storage_url) -ErrorAction Stop
            }
        }
        ConvertTo-Json -InputObject @($rows.ToArray()) -Depth 5 -Compress
    } else { $command.ExecuteNonQuery() | Out-Null }
} finally { $connection.Dispose() }
