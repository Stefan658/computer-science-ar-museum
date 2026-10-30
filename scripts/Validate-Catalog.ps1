param(
    [ValidateSet("Draft", "Release")]
    [string]$Mode = "Draft"
)

$ErrorActionPreference = "Stop"
$catalogPath = Join-Path $PSScriptRoot "..\content\catalog\museum_catalog.json"

$errors = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

function Add-Error([string]$message) {
    $script:errors.Add($message)
}

function Check-Unique($items, [string]$field) {
    $values = @($items | ForEach-Object { $_.$field })
    foreach ($group in ($values | Group-Object)) {
        if ($group.Count -gt 1) {
            Add-Error "Duplicate ${field}: $($group.Name)"
        }
    }
}

try {
    $data = Get-Content -LiteralPath $catalogPath -Raw -Encoding UTF8 |
        ConvertFrom-Json
} catch {
    Write-Host "FAIL: Invalid JSON or missing catalog" -ForegroundColor Red
    exit 1
}

if ($data.schemaVersion -ne 1) {
    Add-Error "Unsupported schemaVersion"
}

$categories = @($data.categories)
$exhibits = @($data.exhibits)

if ($categories.Count -ne 3) {
    Add-Error "Expected 3 categories"
}
if ($exhibits.Count -ne 8) {
    Add-Error "Expected 8 exhibits"
}

Check-Unique $categories "categoryId"
Check-Unique $exhibits "exhibitId"
Check-Unique $exhibits "markerId"
Check-Unique $exhibits "modelKey"

$categoryIds = @($categories | ForEach-Object { $_.categoryId })

foreach ($exhibit in $exhibits) {
    $id = $exhibit.exhibitId

    foreach ($field in @("exhibitId","categoryId","markerId","modelKey","title")) {
        if ([string]::IsNullOrWhiteSpace([string]$exhibit.$field)) {
            Add-Error "${id}: missing $field"
        }
    }

    if ($exhibit.categoryId -notin $categoryIds) {
        Add-Error "${id}: unknown category"
    }

    if ($null -eq $exhibit.scaleMultiplier -or
        ([double]::IsNaN([double]$exhibit.scaleMultiplier) -or [double]::IsInfinity([double]$exhibit.scaleMultiplier)) -or
        [double]$exhibit.scaleMultiplier -le 0) {
        Add-Error "${id}: invalid scaleMultiplier"
    }

    if (@($exhibit.orientationEuler).Count -ne 3) {
        Add-Error "${id}: orientationEuler must have 3 values"
    } else {
        foreach ($angle in $exhibit.orientationEuler) {
            if ($null -eq $angle -or
                ([double]::IsNaN([double]$angle) -or [double]::IsInfinity([double]$angle))) {
                Add-Error "${id}: invalid orientationEuler value"
            }
        }
    }

    $hotspotIds = @($exhibit.hotspots | ForEach-Object { $_.hotspotId })
    if ($hotspotIds.Count -ne @($hotspotIds | Select-Object -Unique).Count) {
        Add-Error "${id}: duplicate hotspotId"
    }

    foreach ($field in @("summary","historicalContext","howItWorks","impact","yearLabel","yearMeaning")) {
        if ([string]::IsNullOrWhiteSpace([string]$exhibit.$field)) {
            if ($Mode -eq "Release") {
                Add-Error "${id}: required editorial field missing: $field"
            } else {
                $warnings.Add("${id}: incomplete $field")
            }
        }
    }

    if ($Mode -eq "Release") {
        if (@($exhibit.sources).Count -eq 0) {
            Add-Error "${id}: sources missing"
        }
        if (@($exhibit.credits).Count -eq 0) {
            Add-Error "${id}: credits missing"
        }
    }
}

$demoCount = @($exhibits |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_.demoKey) }).Count

if ($demoCount -ne 4) {
    Add-Error "Expected 4 configured demonstrations"
}

Write-Host "`n===== CATALOG VALIDATION =====" -ForegroundColor Cyan
Write-Host "Mode: $Mode"
Write-Host "Categories: $($categories.Count)"
Write-Host "Exhibits: $($exhibits.Count)"
Write-Host "Demonstrations: $demoCount"
Write-Host "Errors: $($errors.Count)"
Write-Host "Warnings: $($warnings.Count)"

foreach ($msg in $errors) {
    Write-Host "ERROR: $msg" -ForegroundColor Red
}

foreach ($msg in $warnings) {
    Write-Host "WARNING: $msg" -ForegroundColor Yellow
}

if ($errors.Count -gt 0) {
    Write-Host "RESULT: FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "RESULT: PASSED ($Mode)" -ForegroundColor Green
exit 0