#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$target = Join-Path $root '.env'
if (Test-Path -LiteralPath $target) {
  Write-Host 'Existing .env preserved.'
  return
}
function New-LocalSecret {
  return [Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(24)).ToLowerInvariant()
}
$lines = @(
  '# Generated local infrastructure credentials. Never commit or reuse in production.'
  "POSTGRES_PASSWORD=$(New-LocalSecret)"
  "REDIS_PASSWORD=$(New-LocalSecret)"
  "MINIO_ROOT_PASSWORD=$(New-LocalSecret)"
)
[System.IO.File]::WriteAllLines($target, $lines)
if (-not $IsWindows) { & chmod 600 $target }
Write-Host 'Created ignored .env with unique local credentials. Values were not printed.'
