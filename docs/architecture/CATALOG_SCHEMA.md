# Catalog schema 2

The source of truth is [museum_catalog.json](../../content/catalog/museum_catalog.json). Its byte-identical Unity TextAsset is [the bundled catalog](../../ComputerScienceARMuseum/Assets/IARA/Content/Resources/museum_catalog.json). Preserve the existing TextAsset `.meta` and GUID when copying data. No network request is needed to load content or display citations.

## Contract

The root contains `schemaVersion: 2`, three `categories`, and eight `exhibits`. Version 1 is intentionally unsupported by the updated loader. This is a coordinated source/bundle/loader migration, not an external data upgrade service.

Category IDs and the eight exhibit identity tuples (`exhibitId`, `categoryId`, `markerId`, `modelKey`, `demoKey`) are fixed to the approved catalog. Personal Computing entries retain null `demoKey`. Existing scale, orientation, fidelity labels, credits and hotspot fields remain in JSON; this task does not turn those keys into Unity assets.

| Required historical field | Constraint |
| --- | --- |
| `yearLabel` | Four-digit year, 1800 through the current year |
| `yearMeaning` | Nonblank, at most 250 characters; identifies what the year represents |
| `creators` | Nonempty array of nonblank strings, each at most 200 characters; roles may be included |
| `summary` | Nonblank, at most 240 characters |
| `historicalContext` | Nonblank, at most 600 characters |
| `howItWorks` | Nonblank, at most 500 characters |
| `impact` | Nonblank, at most 400 characters |

Every exhibit has a nonempty `sources` array. Each source contains:

- `sourceId`: unique within the exhibit, formatted `EXHIBIT-ID-S1` with a positive integer suffix, at most 64 characters.
- `title` (200), `publisher` (150), `attribution` (300), `locator` (300): nonblank locally stored strings; parentheses give character limits.
- `url`: absolute HTTPS URL, at most 2000 characters, no credentials or loopback address; unique within an exhibit. The same publication may support multiple exhibits.
- `sourceType`: `museum`, `primary`, `manufacturer`, or `academic-archive`.
- `accessedOn`: valid `yyyy-MM-dd`, no later than the current UTC date.
- `supports`: nonempty array naming supported historical fields from the table. The union across sources must cover all seven fields.

Every exhibit also has nonempty `editorialNotes`. Each note has `kind` (`scope`, `simplification`, or `uncertainty`), nonblank `text` (400 characters maximum), and nonempty `sourceIds` resolving inside that exhibit. These distinguish the educational outline and selected variant from stronger historical claims.

Source associations represent editorial decisions. A schema check cannot establish that a URL is reachable or that its text supports a claim. External verification and editorial review are separate; see the [research ledger](../../content/sources/HISTORICAL_SOURCES.md).

## Validation and editing workflow

1. Edit the source JSON, retaining identifiers and existing nonhistorical fields.
2. Validate prospective content with `powershell -NoProfile -File scripts/Validate-Catalog.ps1 -Mode Content -SkipBundledCheck`.
3. Copy the source bytes to the existing bundled `.json` path, retaining its `.meta` file. Do not reimport as a new asset.
4. Run `Draft` and `Content` validation without switches, then `scripts/Test-CatalogValidation.ps1` and Unity tests.

`Draft` permits missing editorial data as warnings. `Content` requires historical fields, complete source associations and scope notes. `Release` additionally requires asset credits and retains existing release checks. All modes compare source/bundle hashes by default. `-SkipBundledCheck` is for pre-sync checks and isolated negative fixtures, not final verification.

The Content assembly validates bundled historical data before Presentation binds it; Application still owns selection and expansion state. The loader is not a substitute for release asset validation. Empty credits and hotspots remain explicit unfinished asset/interaction work. Passing Content mode does not certify media rights, model fidelity, demonstrations, Android deployment or historical completeness.
