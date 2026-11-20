param(
    [ValidateSet("Draft", "Content", "Release")]
    [string]$Mode = "Draft",
    [string]$CatalogPath = (Join-Path $PSScriptRoot '..\content\catalog\museum_catalog.json'),
    [string]$BundledPath = (Join-Path $PSScriptRoot '..\ComputerScienceARMuseum\Assets\IARA\Content\Resources\museum_catalog.json'),
    [switch]$SkipBundledCheck
)

$ErrorActionPreference = "Stop"

$errors = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

function Add-Error([string]$message) {
    $script:errors.Add($message)
}

function Add-EditorialIssue([string]$message) {
    if ($Mode -eq 'Draft') { $script:warnings.Add($message) }
    else { Add-Error $message }
}

function Test-Text($value, [int]$limit) {
    return $value -is [string] -and -not [string]::IsNullOrWhiteSpace($value) -and $value.Length -le $limit
}

function Test-Number($value) {
    $number = 0.0
    return $null -ne $value -and $value -is [ValueType] -and $value -isnot [bool] -and
        [double]::TryParse([string]$value, [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$number) -and
        -not [double]::IsNaN($number) -and -not [double]::IsInfinity($number)
}

$expected = @{
    'ENC-01' = @('cryptography','marker_enigma','enigma','enigma_signal_path')
    'ENC-02' = @('cryptography','marker_bombe','bombe','bombe_drum_cycle')
    'EAR-01' = @('early-computing','marker_colossus','colossus','colossus_tape_flow')
    'EAR-02' = @('early-computing','marker_eniac','eniac','eniac_calculation_flow')
    'PC-01' = @('personal-computing','marker_apple_i','apple_i',$null)
    'PC-02' = @('personal-computing','marker_apple_ii','apple_ii',$null)
    'PC-03' = @('personal-computing','marker_ibm_5150','ibm_5150',$null)
    'PC-04' = @('personal-computing','marker_macintosh_128k','macintosh_128k',$null)
}
$historicalLimits = @{ yearLabel=4; yearMeaning=250; summary=240; historicalContext=600; howItWorks=500; impact=400 }
$supportedFields = @($historicalLimits.Keys) + 'creators'

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

if ($data.schemaVersion -ne 2) {
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
Check-Unique $categories "displayOrder"
Check-Unique $exhibits "exhibitId"
Check-Unique $exhibits "markerId"
Check-Unique $exhibits "modelKey"

$categoryIds = @($categories | ForEach-Object { $_.categoryId })
foreach ($category in $categories) {
    if ($category.categoryId -notin @('cryptography','early-computing','personal-computing') -or
        -not (Test-Text $category.title 150) -or -not (Test-Number $category.displayOrder) -or $category.displayOrder -lt 1) {
        Add-Error 'Invalid pavilion metadata'
    }
}

foreach ($exhibit in $exhibits) {
    $id = $exhibit.exhibitId
    if (-not $expected.ContainsKey([string]$id)) { Add-Error "Unknown exhibit: $id" }
    else {
        $mappingFields = @('categoryId','markerId','modelKey','demoKey')
        for ($index=0; $index -lt $mappingFields.Count; $index++) {
            $field = $mappingFields[$index]
            if ($exhibit.$field -cne $expected[$id][$index]) { Add-Error "${id}: changed $field mapping" }
        }
    }

    foreach ($field in @("exhibitId","categoryId","markerId","modelKey","title")) {
        if ([string]::IsNullOrWhiteSpace([string]$exhibit.$field)) {
            Add-Error "${id}: missing $field"
        }
    }

    if ($exhibit.categoryId -notin $categoryIds) {
        Add-Error "${id}: unknown category"
    }

    if (-not (Test-Number $exhibit.scaleMultiplier) -or
        [double]$exhibit.scaleMultiplier -le 0) {
        Add-Error "${id}: invalid scaleMultiplier"
    }

    if (@($exhibit.orientationEuler).Count -ne 3) {
        Add-Error "${id}: orientationEuler must have 3 values"
    } else {
        foreach ($angle in $exhibit.orientationEuler) {
            if (-not (Test-Number $angle)) {
                Add-Error "${id}: invalid orientationEuler value"
            }
        }
    }

    $hotspotIds = @($exhibit.hotspots | ForEach-Object { $_.hotspotId })
    if ($hotspotIds.Count -ne @($hotspotIds | Select-Object -Unique).Count) {
        Add-Error "${id}: duplicate hotspotId"
    }

    foreach ($field in $historicalLimits.Keys) {
        if (-not (Test-Text $exhibit.$field $historicalLimits[$field])) { Add-EditorialIssue "${id}: missing/oversized $field" }
    }
    $year = 0
    if (-not [int]::TryParse([string]$exhibit.yearLabel, [ref]$year) -or $year -lt 1800 -or $year -gt [DateTime]::UtcNow.Year) {
        Add-EditorialIssue "${id}: invalid historical year"
    }
    if ($exhibit.creators -isnot [Array] -or @($exhibit.creators).Count -eq 0) { Add-EditorialIssue "${id}: creators missing" }
    foreach ($creator in $exhibit.creators) { if (-not (Test-Text $creator 200)) { Add-EditorialIssue "${id}: invalid creator attribution" } }

    if ($exhibit.sources -isnot [Array] -or @($exhibit.sources).Count -eq 0) { Add-EditorialIssue "${id}: sources missing" }
    $sourceIds = @()
    $sourceUrls = @()
    $covered = @()
    foreach ($source in $exhibit.sources) {
        foreach ($field in @('sourceId','title','publisher','url','sourceType','accessedOn','attribution','locator')) {
            $limit = if ($field -eq 'sourceId') {64} elseif ($field -eq 'title') {200} elseif ($field -eq 'publisher') {150} elseif ($field -eq 'url') {2000} else {300}
            if (-not (Test-Text $source.$field $limit)) { Add-Error "${id}: missing/invalid source $field" }
        }
        if ([string]$source.sourceId -notmatch ('^' + [regex]::Escape([string]$id) + '-S[1-9][0-9]*$') -or $source.sourceId -cin $sourceIds) { Add-Error "${id}: invalid/duplicate sourceId" }
        $sourceIds += $source.sourceId
        $uri = $null
        if (-not [Uri]::TryCreate([string]$source.url, [UriKind]::Absolute, [ref]$uri) -or
            $uri.Scheme -ne 'https' -or $uri.IsLoopback -or $uri.UserInfo -ne '' -or $source.url -in $sourceUrls) { Add-Error "${id}: invalid/duplicate source URL" }
        $sourceUrls += $source.url
        $date = [DateTime]::MinValue
        if (-not [DateTime]::TryParseExact([string]$source.accessedOn, 'yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::None, [ref]$date) -or $date.Date -gt [DateTime]::UtcNow.Date) { Add-Error "${id}: invalid source access date" }
        if ($source.sourceType -cnotin @('museum','primary','manufacturer','academic-archive')) { Add-Error "${id}: unknown source type" }
        if ($source.supports -isnot [Array] -or @($source.supports).Count -eq 0) { Add-Error "${id}: source field associations missing" }
        foreach ($field in $source.supports) {
            if ($field -cnotin $supportedFields) { Add-Error "${id}: unknown supported field: $field" }
            $covered += $field
        }
    }
    foreach ($field in $supportedFields) { if ($field -notin $covered) { Add-EditorialIssue "${id}: no source associated with $field" } }
    if ($exhibit.editorialNotes -isnot [Array] -or @($exhibit.editorialNotes).Count -eq 0) { Add-EditorialIssue "${id}: editorial scope missing" }
    foreach ($note in $exhibit.editorialNotes) {
        if ($note.kind -cnotin @('scope','simplification','uncertainty') -or -not (Test-Text $note.text 400)) { Add-Error "${id}: invalid editorial note" }
        if ($note.sourceIds -isnot [Array] -or @($note.sourceIds).Count -eq 0) { Add-Error "${id}: note source associations missing" }
        foreach ($sourceId in $note.sourceIds) { if ($sourceId -cnotin $sourceIds) { Add-Error "${id}: unknown editorial source: $sourceId" } }
    }

    if ($Mode -eq "Release") {
        if (@($exhibit.credits).Count -eq 0) {
            Add-Error "${id}: release asset credits missing (historical references do not clear model/media rights)"
        }
    }
}

if (-not $SkipBundledCheck) {
    if (-not (Test-Path -LiteralPath $BundledPath)) { Add-Error 'Bundled Unity catalog missing' }
    elseif ((Get-FileHash -LiteralPath $CatalogPath).Hash -ne (Get-FileHash -LiteralPath $BundledPath).Hash) { Add-Error 'Source and bundled catalogs differ' }
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
