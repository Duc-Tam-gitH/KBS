param([string]$Python = 'python')

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$pythonPath = (Get-Command $Python -ErrorAction Stop).Source
if (-not $env:KBS_AI_WORKER_KEY) {
    $env:KBS_AI_WORKER_KEY = [Convert]::ToHexString(
        [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
}
$env:Ai__WorkerKey = $env:KBS_AI_WORKER_KEY
$env:KBS_API_URL = 'http://localhost:5231'
$worker = $null
try {
    $worker = Start-Process -FilePath $pythonPath -ArgumentList '-u', 'ai_auto.py' `
        -WorkingDirectory $PSScriptRoot -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $PSScriptRoot 'worker.log') `
        -RedirectStandardError (Join-Path $PSScriptRoot 'worker-error.log')
    dotnet run --project (Join-Path $projectRoot 'backend/KBS.API') --launch-profile http
} finally {
    if ($worker -and -not $worker.HasExited) { Stop-Process -Id $worker.Id }
}
