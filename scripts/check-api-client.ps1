#Requires -Version 7.0
param([switch]$Update)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
  $contract = Join-Path $root 'docs/api/openapi.json'
  $client = Join-Path $root 'web/src/api/schema.d.ts'
  $tempDirectory = Join-Path $root '.local/contracts'
  New-Item -ItemType Directory -Path $tempDirectory -Force | Out-Null
  $generatedContract = Join-Path $tempDirectory 'openapi.json'
  $generatedClient = Join-Path $tempDirectory 'schema.d.ts'
  & dotnet run --project tools/ExpenseGuard.ContractGenerator --no-build --no-restore -- $generatedContract
  if ($LASTEXITCODE -ne 0) { throw 'OpenAPI generation failed.' }
  & node (Join-Path $root 'web/node_modules/openapi-typescript/bin/cli.js') $generatedContract -o $generatedClient
  if ($LASTEXITCODE -ne 0) { throw 'Client generation failed.' }
  if ($Update) {
    Copy-Item -LiteralPath $generatedContract -Destination $contract
    Copy-Item -LiteralPath $generatedClient -Destination $client
    Write-Host 'Updated OpenAPI and generated TypeScript contract.'
  }
  else {
    foreach ($pair in @(@($contract, $generatedContract), @($client, $generatedClient))) {
      if (-not (Test-Path -LiteralPath $pair[0])) { throw "Missing generated file: $($pair[0])" }
      if ((Get-Content -LiteralPath $pair[0] -Raw).Replace("`r`n", "`n") -cne (Get-Content -LiteralPath $pair[1] -Raw).Replace("`r`n", "`n")) {
        throw "Generated file is stale: $($pair[0]). Run scripts/check-api-client.ps1 -Update."
      }
    }
    Write-Host 'PASS: OpenAPI and generated client are current.'
  }
}
finally { Pop-Location }
