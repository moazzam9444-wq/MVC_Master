# Shared fail-closed gate; SELECTs only, before fixtures or cleanup.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Data
if(!$env:KICKS_TEST_CONNECTION){throw 'Set KICKS_TEST_CONNECTION explicitly. Mutating tests may never fall back to Kicks.'}
$testBuilder=New-Object System.Data.SqlClient.SqlConnectionStringBuilder $env:KICKS_TEST_CONNECTION
if(!$testBuilder.IntegratedSecurity -or $testBuilder.DataSource -ine 'localhost' -or $testBuilder.InitialCatalog -cnotmatch '^Kicks_BookingTest_\d{8}_[a-f0-9]{8}$'){throw 'Unsafe test connection. Only a local restored Kicks_BookingTest database is permitted; Kicks is forbidden.'}
$testGuardConnection=New-Object System.Data.SqlClient.SqlConnection $testBuilder.ConnectionString
$testGuardConnection.Open()
try{
 $testGuardCommand=$testGuardConnection.CreateCommand()
 $testGuardCommand.CommandText="SELECT CASE WHEN DB_NAME()=@name AND DB_NAME()<>N'Kicks' AND NOT EXISTS(SELECT 1 FROM sys.master_files test JOIN sys.master_files live ON live.physical_name=test.physical_name AND live.database_id=DB_ID(N'Kicks') WHERE test.database_id=DB_ID()) THEN 1 ELSE 0 END"
 [void]$testGuardCommand.Parameters.AddWithValue('@name',$testBuilder.InitialCatalog)
 if([int]$testGuardCommand.ExecuteScalar() -ne 1){throw 'Actual test database identity/file isolation check failed.'}
}finally{$testGuardConnection.Dispose()}
$TestConnectionString=$testBuilder.ConnectionString
$env:ConnectionStrings__DefaultConnection=$TestConnectionString
