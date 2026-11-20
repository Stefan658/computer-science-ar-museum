param()
$ErrorActionPreference = 'Stop'
$validator = Join-Path $PSScriptRoot 'Validate-Catalog.ps1'
$source = Join-Path $PSScriptRoot '..\content\catalog\museum_catalog.json'
$fixtures = Join-Path $PSScriptRoot '..\ComputerScienceARMuseum\Library\I4\ValidatorFixtures'
New-Item -ItemType Directory -Path $fixtures -Force | Out-Null
$original = Get-Content -LiteralPath $source -Raw -Encoding UTF8
$cases = @(
    @{ Name='missing-history'; Change={ param($data) $data.exhibits[0].historicalContext='' } },
    @{ Name='missing-creators'; Change={ param($data) $data.exhibits[0].creators=@() } },
    @{ Name='missing-attribution'; Change={ param($data) $data.exhibits[0].sources[0].attribution='' } },
    @{ Name='bad-url'; Change={ param($data) $data.exhibits[0].sources[0].url='javascript:invalid' } },
    @{ Name='changed-marker'; Change={ param($data) $data.exhibits[0].markerId='other-marker' } },
    @{ Name='unknown-note-source'; Change={ param($data) $data.exhibits[0].editorialNotes[0].sourceIds=@('missing') } },
    @{ Name='missing-field-association'; Change={ param($data) foreach ($item in $data.exhibits[0].sources) { $item.supports=@($item.supports | Where-Object {$_ -ne 'impact'}) } } }
)
$output = & powershell -NoProfile -File $validator -Mode Content 2>&1
if ($LASTEXITCODE -ne 0) { throw "Valid catalog rejected: $output" }
Write-Output 'PASS: valid content and bundle'
foreach ($case in $cases) {
    $data = $original | ConvertFrom-Json
    & $case.Change $data
    $path = Join-Path $fixtures ($case.Name + '.json')
    [IO.File]::WriteAllText($path, ($data | ConvertTo-Json -Depth 16), [Text.UTF8Encoding]::new($false))
    $output = & powershell -NoProfile -File $validator -Mode Content -CatalogPath $path -SkipBundledCheck 2>&1
    if ($LASTEXITCODE -eq 0) { throw "Invalid fixture accepted: $($case.Name)" }
    Write-Output "PASS: rejects $($case.Name)"
}
$mismatch = Join-Path $fixtures 'mismatched-bundle.json'
[IO.File]::WriteAllText($mismatch, $original + ' ', [Text.UTF8Encoding]::new($false))
$output = & powershell -NoProfile -File $validator -Mode Content -BundledPath $mismatch 2>&1
if ($LASTEXITCODE -eq 0) { throw 'Mismatched bundled bytes accepted' }
Write-Output 'PASS: rejects mismatched bundle'
Write-Output 'RESULT: 9 validator checks passed; fixtures remain only in ignored Library/I4.'
exit 0
