#Requires -Version 7.0
param([switch]$SkipDependencies, [switch]$SkipInstall, [switch]$SeedDemo)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
$children = [System.Collections.Generic.List[System.Diagnostics.Process]]::new()
try {
  foreach ($required in @('dotnet', 'node', 'pnpm')) {
    if (-not (Get-Command $required -ErrorAction SilentlyContinue)) { throw "$required must be on PATH." }
  }
  if (-not $SkipInstall) {
    & dotnet restore ExpenseGuard.slnx --locked-mode --configfile NuGet.Config
    if ($LASTEXITCODE -ne 0) { throw 'Backend restore failed.' }
    & pnpm --dir web install --frozen-lockfile
    if ($LASTEXITCODE -ne 0) { throw 'Frontend install failed.' }
  }
  & dotnet build ExpenseGuard.slnx --no-restore
  if ($LASTEXITCODE -ne 0) { throw 'Backend build failed.' }
  if (-not $SkipDependencies) {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
      throw 'Docker is required for dependency startup. Use -SkipDependencies to inspect the foundation with unavailable dependencies.'
    }
    & "$PSScriptRoot/init-local.ps1"
    & docker compose up -d --build --wait --wait-timeout 120
    if ($LASTEXITCODE -ne 0) { throw 'Dependency startup failed.' }
    $values = @{}
    foreach ($line in Get-Content -LiteralPath (Join-Path $root '.env')) {
      if ($line -match '^(POSTGRES_PASSWORD|REDIS_PASSWORD|MINIO_ROOT_PASSWORD)=(.+)$') {
        $values[$Matches[1]] = $Matches[2]
      }
    }
    foreach ($key in @('POSTGRES_PASSWORD', 'REDIS_PASSWORD', 'MINIO_ROOT_PASSWORD')) {
      if ([string]::IsNullOrWhiteSpace($values[$key])) { throw "Missing $key in local .env." }
    }
    $env:ConnectionStrings__Postgres = "Host=localhost;Port=5432;Database=expenseguard;Username=expenseguard;Password=$($values['POSTGRES_PASSWORD']);Timeout=2"
    $env:ConnectionStrings__Redis = "localhost:6379,password=$($values['REDIS_PASSWORD'])"
    $env:ObjectStorage__Endpoint = 'http://localhost:9000'
  }
  $env:ASPNETCORE_ENVIRONMENT = 'Development'
  $env:Demo__Seed = $SeedDemo.IsPresent.ToString()
  $env:Database__Initialize = $SeedDemo.IsPresent.ToString()
  $env:Services__Worker = 'http://localhost:5101'
  $env:Services__Mcp = 'http://localhost:5102'
  $logDir = Join-Path $root '.local/logs'
  New-Item -ItemType Directory -Path $logDir -Force | Out-Null
  $hosts = @(@{ Name='Api'; Port=5100 }, @{ Name='Worker'; Port=5101 }, @{ Name='McpServer'; Port=5102 })
  foreach ($hostInfo in $hosts) {
    $dll = Join-Path $root "src/ExpenseGuard.$($hostInfo.Name)/bin/Debug/net10.0/ExpenseGuard.$($hostInfo.Name).dll"
    $options = @{
      FilePath = (Get-Command dotnet).Source
      ArgumentList = @('"' + $dll + '"', '--urls', "http://localhost:$($hostInfo.Port)")
      WorkingDirectory = (Join-Path $root "src/ExpenseGuard.$($hostInfo.Name)")
      RedirectStandardOutput = (Join-Path $logDir "$($hostInfo.Name).log")
      RedirectStandardError = (Join-Path $logDir "$($hostInfo.Name).error.log")
      PassThru = $true
    }
    if ($IsWindows) { $options.WindowStyle = 'Hidden' }
    $children.Add((Start-Process @options))
  }
  $webOptions = @{
    FilePath = (Get-Command node).Source
    ArgumentList = @('"' + (Join-Path $root 'web/node_modules/vite/bin/vite.js') + '"', '--host', '127.0.0.1')
    WorkingDirectory = (Join-Path $root 'web')
    RedirectStandardOutput = (Join-Path $logDir 'Web.log')
    RedirectStandardError = (Join-Path $logDir 'Web.error.log')
    PassThru = $true
  }
  if ($IsWindows) { $webOptions.WindowStyle = 'Hidden' }
  $children.Add((Start-Process @webOptions))
  $webScheme = if (Test-Path -LiteralPath (Join-Path $root '.local/localhost.pem')) { 'https' } else { 'http' }
  Write-Host "Starting ExpenseGuard at ${webScheme}://localhost:5173. Logs: .local/logs"
  Write-Host 'Ctrl+C stops application hosts. Dependency containers remain available; docker compose down stops them without deleting volumes.'
  while ($true) {
    foreach ($child in $children) {
      if ($child.HasExited) { throw 'An application process exited. Inspect .local/logs.' }
    }
    Start-Sleep -Seconds 1
  }
}
finally {
  foreach ($child in $children) {
    if (-not $child.HasExited) { $child.Kill($true); $child.WaitForExit() }
    $child.Dispose()
  }
  Pop-Location
}
