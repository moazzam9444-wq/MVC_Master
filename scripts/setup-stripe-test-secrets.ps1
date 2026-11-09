#requires -Version 7.0
[CmdletBinding()]
param([switch]$CheckPrerequisites)

# Run in a separate, local interactive PowerShell window. Never supply keys as arguments.
# Both values are hidden at entry, held only in memory here, and sent to Secret Manager over stdin.
# No secret-store readback, transcript, key logging, plaintext temporary file or SQL operation.
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$projectPath=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\KicksMvc.csproj'))
$projectRoot=Split-Path -Parent $projectPath
$statusDirectory=Join-Path $projectRoot '.local\stripe-phase'
$statusPath=Join-Path $statusDirectory 'credential-setup-status.json'
$stripeHandler=$null;$stripeClient=$null;$stripeRequest=$null;$stripeResponse=$null
$publishableSecure=$null;$secretSecure=$null;$publishableText=$null;$secretText=$null
$payload=$null;$responseText=$null;$secretProcess=$null;$status='Failed';$success=$false

function Write-SetupStatus([string]$value,[bool]$apiVerified=$false) {
    [void][IO.Directory]::CreateDirectory($statusDirectory)
    # Deliberately contains no values, partial values, lengths, fingerprints, or account data.
    [ordered]@{
        Status=$value
        SecretApiTestModeVerified=$apiVerified
        StoredInUserSecrets=($value -eq 'Succeeded')
        ReadBackStoredValues=$false
        Project='KicksMvc'
        UpdatedAtUtc=[DateTimeOffset]::UtcNow.ToString('O')
    }|ConvertTo-Json|Set-Content -LiteralPath $statusPath -Encoding utf8
}

function Convert-InMemory([Security.SecureString]$value) {
    $pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($value)
    try{return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)}
    finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)}
}

try {
    if(!(Test-Path -LiteralPath $projectPath -PathType Leaf)){throw 'Project prerequisite failed.'}
    [xml]$projectXml=Get-Content -LiteralPath $projectPath -Raw
    $idNodes=@($projectXml.SelectNodes('/Project/PropertyGroup/UserSecretsId'))
    if($idNodes.Count -ne 1 -or [string]::IsNullOrWhiteSpace($idNodes[0].InnerText)){throw 'User Secrets is not initialized.'}
    $dotnetPath=(Get-Command dotnet.exe -CommandType Application -ErrorAction Stop).Source
    if($CheckPrerequisites){Write-Output 'PASS: initialized project and .NET CLI found; no credential input, store read or network call performed.';exit 0}
    if(![Environment]::UserInteractive -or [Console]::IsInputRedirected){throw 'A local interactive console is required.'}
    [Console]::Title='KICKS - Stripe TEST Credentials'
    Write-Host 'KICKS - Stripe sandbox credential setup'
    Write-Host 'Copy BOTH keys from the same Stripe sandbox/test-mode account.'
    Write-Host 'Both inputs are hidden. Never paste these keys into chat or commands.'
    Write-Host 'Storage: .NET User Secrets in your Windows profile, outside the repository.'
    Write-Host 'User Secrets is development storage; it does not encrypt values at rest.'
    Write-SetupStatus 'AwaitingInput'
    $publishableSecure=Read-Host 'Publishable TEST key (hidden)' -AsSecureString
    $secretSecure=Read-Host 'Secret TEST key (hidden)' -AsSecureString
    $publishableText=Convert-InMemory $publishableSecure
    $secretText=Convert-InMemory $secretSecure
    if($publishableText -cnotmatch '^pk_test_[A-Za-z0-9_]{16,512}$' -or $secretText -cnotmatch '^sk_test_[A-Za-z0-9_]{16,512}$'){
        $status='InvalidTestKeyFormat';throw 'Only test-mode key formats are accepted.'
    }

    # Read-only authentication check. Live/invalid keys were rejected before any network request.
    # No redirects, certificate bypass, account mutation, Checkout Session or payment creation.
    $status='TestApiValidationFailed'
    $stripeHandler=[Net.Http.HttpClientHandler]::new();$stripeHandler.AllowAutoRedirect=$false
    $stripeClient=[Net.Http.HttpClient]::new($stripeHandler);$stripeClient.Timeout=[TimeSpan]::FromSeconds(20)
    $stripeRequest=[Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::Get,'https://api.stripe.com/v1/balance')
    $stripeRequest.Headers.Authorization=[Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$secretText)
    Write-Host 'Validating the secret key with a read-only Stripe test-mode request...'
    $stripeResponse=$stripeClient.SendAsync($stripeRequest).GetAwaiter().GetResult()
    if(!$stripeResponse.IsSuccessStatusCode){throw 'Stripe test credential validation failed.'}
    $responseText=$stripeResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $balance=$responseText|ConvertFrom-Json
    if($balance.object -cne 'balance' -or $balance.livemode -isnot [bool] -or $balance.livemode -ne $false){throw 'Stripe test mode was not confirmed.'}
    $responseText=$null;$balance=$null

    $status='SecretStorageFailed'
    $payload=[ordered]@{'Stripe:PublishableKey'=$publishableText;'Stripe:SecretKey'=$secretText}|ConvertTo-Json -Compress
    $start=[Diagnostics.ProcessStartInfo]::new()
    $start.FileName=$dotnetPath;$start.WorkingDirectory=$projectRoot
    $start.UseShellExecute=$false;$start.CreateNoWindow=$true
    $start.RedirectStandardInput=$true;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
    foreach($argument in @('user-secrets','set','--project',$projectPath,'--configuration','Debug')){[void]$start.ArgumentList.Add($argument)}
    $start.StandardInputEncoding=[Text.UTF8Encoding]::new($false)
    $secretProcess=[Diagnostics.Process]::new();$secretProcess.StartInfo=$start
    [void]$secretProcess.Start()
    # Drain privately, suppress all CLI output, and never include its text in an exception/report.
    $discardOutput=$secretProcess.StandardOutput.ReadToEndAsync()
    $discardError=$secretProcess.StandardError.ReadToEndAsync()
    $secretProcess.StandardInput.WriteLine($payload);$secretProcess.StandardInput.Close();$payload=$null
    if(!$secretProcess.WaitForExit(30000)){$secretProcess.Kill($true);throw 'Secret Manager timed out.'}
    $null=$discardOutput.GetAwaiter().GetResult();$null=$discardError.GetAwaiter().GetResult()
    if($secretProcess.ExitCode -ne 0){throw 'Secret Manager could not save the credentials.'}
    Write-SetupStatus 'Succeeded' $true
    $success=$true
    Write-Host 'SUCCESS: test-mode secret verified; both keys saved to .NET User Secrets.' -ForegroundColor Green
    Write-Host 'The publishable key prefix was validated; account-pair matching has not been tested.'
    Write-Host 'No stored secret values were read back. No SQL records or payments were changed.'
    Write-Host 'Return to Codex and report only: Setup succeeded.'
}
catch {
    if($CheckPrerequisites){Write-Error 'Credential setup prerequisites failed.';exit 1}
    try{Write-SetupStatus $status}catch{}
    # Do not render $_, exceptions, request/response bodies, or captured CLI output.
    Write-Host 'Setup did not complete. No credential values will be displayed.' -ForegroundColor Red
    if($status -eq 'InvalidTestKeyFormat'){Write-Host 'Use complete pk_test_ and sk_test_ keys from the same sandbox. Live keys are refused.'}
    elseif($status -eq 'TestApiValidationFailed'){Write-Host 'Stripe did not confirm a valid test-mode secret. Check the sandbox key and internet access, then rerun.'}
    elseif($status -eq 'SecretStorageFailed'){Write-Host 'Secret Manager could not confirm storage. Report this generic status to Codex.'}
    else{Write-Host 'Report only that setup failed; do not share the keys or a terminal transcript.'}
}
finally {
    if($null -ne $stripeRequest){$stripeRequest.Headers.Authorization=$null;$stripeRequest.Dispose()}
    if($null -ne $stripeResponse){$stripeResponse.Dispose()}
    if($null -ne $stripeClient){$stripeClient.Dispose()}elseif($null -ne $stripeHandler){$stripeHandler.Dispose()}
    if($null -ne $secretProcess){$secretProcess.Dispose()}
    if($null -ne $publishableSecure){$publishableSecure.Dispose()}
    if($null -ne $secretSecure){$secretSecure.Dispose()}
    $publishableText=$null;$secretText=$null;$payload=$null;$responseText=$null
}
Write-Host 'Press any key to close this window.'
$null=[Console]::ReadKey($true)
if(!$success){exit 1}
