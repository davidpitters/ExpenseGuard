#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$directory = Join-Path $root '.local'
New-Item -ItemType Directory -Path $directory -Force | Out-Null
& dotnet dev-certs https --export-path (Join-Path $directory 'localhost.pem') --format Pem --no-password
if ($LASTEXITCODE -ne 0) { throw 'Development certificate export failed.' }
Write-Host 'Exported the local development certificate into ignored .local. No trust settings were changed.'
Write-Host 'Before using sign-in, trust this certificate with dotnet dev-certs https --trust, then restart the web host.'
