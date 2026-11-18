$ErrorActionPreference = 'Stop'
$configPath = Join-Path $PSScriptRoot 'backend/KBS.API/appsettings.Local.json'
if (!(Test-Path -LiteralPath $configPath)) {
    throw 'Missing appsettings.Local.json. Run this script from the KBS checkout.'
}

# Read-Host -AsSecureString works in both Windows PowerShell 5.1 and PowerShell 7.
# Never write the token to the console or pass it as a command-line argument.
$secureToken = Read-Host 'Enter SePay Live API token (hidden)' -AsSecureString
$token = [System.Net.NetworkCredential]::new('', $secureToken).Password.Trim()
try {
    if ([string]::IsNullOrWhiteSpace($token)) { throw 'Token cannot be empty.' }
    if ($token.StartsWith('spsk_')) { throw 'Use the SePay bank API token, not a Payment Gateway merchant secret.' }
    $config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
    if (!$config.Payments) {
        $config | Add-Member NoteProperty Payments ([pscustomobject]@{}) -Force
    }
    if (!$config.Payments.SePay) {
        $config.Payments | Add-Member NoteProperty SePay ([pscustomobject]@{}) -Force
    }
    $config.Payments.SePay | Add-Member NoteProperty ApiToken $token -Force
    $config | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $configPath -Encoding UTF8
    Write-Host 'Saved to the Git-ignored local configuration. Running Development backend reloads it automatically.'
    Write-Host 'Wait 30 seconds, then refresh Admin > Payments. No token is printed.'
}
finally {
    $token = $null
    $config = $null
    $secureToken.Dispose()
}
