param([string]$Python)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if ($Python) { $env:Ai__PythonPath = (Get-Command $Python -ErrorAction Stop).Source }
Write-Host 'Starting backend. Enable an AI on the web page to start its worker; OFF stops it.'
dotnet run --project (Join-Path $projectRoot 'backend/KBS.API') --launch-profile http
if ($LASTEXITCODE -ne 0) { throw "Backend exited with code $LASTEXITCODE." }
