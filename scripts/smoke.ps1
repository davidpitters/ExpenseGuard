#Requires -Version 7.0
param([switch]$RequireDependencies, [string]$WebUrl = 'http://127.0.0.1:5173')
$ErrorActionPreference = 'Stop'
foreach ($port in @(5100, 5101, 5102)) {
  $live = Invoke-WebRequest "http://localhost:$port/health/live" -TimeoutSec 10 -SkipHttpErrorCheck
  if ($live.StatusCode -ne 200) { throw "Host $port is not alive." }
  $ready = Invoke-WebRequest "http://localhost:$port/health/ready" -TimeoutSec 10 -SkipHttpErrorCheck
  if ($ready.StatusCode -notin @(200, 503)) { throw "Unexpected readiness response on $port." }
  if ($RequireDependencies -and $ready.StatusCode -ne 200) { throw "Host $port is not ready." }
  Write-Host "Host $port live=$($live.StatusCode) ready=$($ready.StatusCode)"
}
$mcp = Invoke-WebRequest 'http://localhost:5102/mcp' -Method Post -ContentType 'application/json' -Headers @{ Accept='application/json, text/event-stream' } -Body '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' -SkipHttpErrorCheck -TimeoutSec 10
if ($mcp.StatusCode -ne 401) { throw 'MCP must be locked at this milestone.' }
$summary = Invoke-RestMethod 'http://localhost:5100/api/system' -TimeoutSec 10
if ($summary.components.Count -ne 6) { throw 'System contract mismatch.' }
$web = Invoke-WebRequest "$WebUrl/" -TimeoutSec 10
if ($web.StatusCode -ne 200 -or $web.Content -notmatch 'ExpenseGuard') { throw 'Web shell unavailable.' }
$proxied = Invoke-RestMethod "$WebUrl/api/system" -TimeoutSec 10
if ($proxied.product -ne 'ExpenseGuard') { throw 'Vite API proxy failed.' }
Write-Host 'PASS: three hosts, MCP denial, system contract, web shell and API proxy.'
if (-not $RequireDependencies) { Write-Host 'Dependency readiness was observed, not required. Use -RequireDependencies for full milestone acceptance.' }
