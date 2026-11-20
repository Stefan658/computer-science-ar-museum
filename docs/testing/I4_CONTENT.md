# I4 — Catalog content and data integration

## Result and boundaries

I4 replaces empty historical fields with concise English text for all eight exhibits. The existing IDs, category mappings, marker/model/demo keys, titles, scale/orientation, fidelity labels, empty credits and empty hotspots are retained. The source JSON and existing Unity TextAsset are synchronized without replacing the TextAsset metadata.

No AR configuration, scene, prefab, package, gesture behavior or responsive layout rule is intentionally changed. AR actions remain disabled. Models, markers, educational demonstrations, Bootstrap flow, Android builds and device tests are outside this task. The I3 reports retain their original results; this report records I4 evidence separately.

## Audit and implementation

Baseline: developer I3 commit `60f756d`, clean working tree at initial inspection. Schema 1 had blank historical fields and empty sources. The existing loader exposed only the fields needed for I3 placeholder cards; it did not bind years, contributors or provenance.

- **Content:** `MuseumCatalog` now parses schema 2 and rejects incomplete historical fields, changed identity mappings, malformed source metadata, uncovered historical fields and unresolved editorial note references. See the [schema contract](../architecture/CATALOG_SCHEMA.md).
- **Application:** existing pavilion and expansion state logic is retained; no historical prose or source parsing is moved into application state.
- **Presentation:** each card shows ID/year and its JSON summary. Expanded content shows date/event, contributors, historical context, mechanism, impact, editorial scope/simplification, numbered local source titles/publishers and an explicit unavailable-feature message. Source numbers derive from each exhibit's stored associations. URLs are metadata, not resources fetched at runtime.
- **Tooling:** `Validate-Catalog.ps1` adds Content mode, bounded fields, source/notes integrity, immutable mapping checks and source/bundle byte comparison. Draft and Release remain available. Negative fixtures stay under ignored `Library/I4/ValidatorFixtures`.
- **Tests:** new catalog tests cover every exhibit, preserved TextAsset GUID/bytes, eight invalid-content cases, and complete card binding in Play Mode. Existing I3 interaction/layout tests remain, with their invalid schema fixture updated to version 2.

The preserved TextAsset GUID is `54be6c30bbd06c548adc0d41c9530d2d`. Historical attribution does not populate asset credits. No runtime source fetching, external media, or dependencies are added.

## Historical evidence

Research tools opened 16 distinct institutional/primary-document URLs, represented by 17 per-exhibit source records. The [research ledger](../../content/sources/HISTORICAL_SOURCES.md) lists sources, locators, access date, editorial choices and excluded inaccessible candidates. Field associations are stored in the catalog itself.

| Exhibit | Display year | Meaning |
| --- | --- | --- |
| ENC-01 | 1918 | Early Enigma development; not a specified wartime variant's manufacture |
| ENC-02 | 1940 | First British Bombe operation; diagonal-board version followed that year |
| EAR-01 | 1944 | First Colossus Mk I operation |
| EAR-02 | 1946 | Public unveiling, distinct from earlier operational work |
| PC-01 | 1976 | Commercial Apple I boards, distinct from initial design |
| PC-02 | 1977 | Original Apple II introduction |
| PC-03 | 1981 | IBM 5150 announcement |
| PC-04 | 1984 | Original Macintosh introduction |

## Verification record

Executed on 2026-11-20. Results below are specific to this content revision; catalog checks cannot prove historical truth or Android behavior.

| Check | Actual result / evidence |
| --- | --- |
| Draft catalog | Passed: 0 errors, 0 warnings |
| Content catalog | Passed: 0 errors, 0 warnings; three categories, eight exhibits, four demo identifiers |
| Validator regression checks | Nine passed: valid catalog, seven invalid-content fixtures, mismatched bundle rejection |
| Release catalog | Failed as expected: eight missing asset-credit errors, zero warnings; release readiness is not claimed |
| Unity import/C# compilation | Final batch import and compilation completed; no C# compiler errors or warnings found in the final log |
| Unity Test Runner | Final run: 22 passed, 0 failed, 0 skipped; Unity exit 0 |
| Eight historical records | All eight exhibit/year cases passed, including required fields, source coverage and local note references |
| Invalid runtime content | Eight negative cases passed: missing summary/creators/attribution, bad URL, duplicate source ID, unknown note source, missing impact association and changed marker |
| Bundled data and identity | Byte equality and original TextAsset GUID test passed; SHA-256 `3A121C962A1A64639E50DD4B7F79F90AD162FA7D724475D9C003A917A2B09D5B` for both copies |
| Play Mode content binding | All eight expanded cards bind the correct text, contributors, source titles, notes and unavailable-feature message; AR action remains disabled |
| Existing interactions | All four retained I3 tests rerun and passed, including filters, expansion persistence, scrolling and About |
| Expanded portrait geometry | 24 filter/resolution/inset combinations passed with populated content; details below |
| Preserved data | Compared against HEAD: all categories and nonhistorical exhibit fields unchanged |
| Repository conventions | Markdown headings/fences/local references, repository ignore coverage and source whitespace checks passed |
| Actual simulator/GPU screenshots and physical touch | Not run for I4 |
| Android build/device/ADB | Not run, explicitly outside scope |

Ignored evidence paths: `ComputerScienceARMuseum/Library/I4/catalog-draft.log`, `catalog-content.log`, `catalog-release.log`, `validator-tests.log`, `tests.xml`, and root `unity-i4-tests.log`. Initial failed-run evidence is retained as `ComputerScienceARMuseum/Library/I4/tests-initial.xml` and `unity-i4-tests-initial.log`. Standalone compiler artifacts under `ComputerScienceARMuseum/Library/I4/Compile/` are supplementary and do not substitute for a Unity import or Test Runner result.

The first run had 2 passes and 20 failures: validation rejected PC entries because Unity `JsonUtility` represents JSON null string values as empty strings. The Content loader now accepts that representation as no demo; PowerShell still enforces literal null in the source JSON. No catalog mapping was changed to work around serialization. All 22 tests passed after correction. The initial downstream UI failures therefore did not establish a layout regression.

The import log contains native-extension diagnostics for Android/WindowsStandalone, two IL postprocessor duplicate-hint-path warnings, and Mono shutdown/debugger-listener diagnostics. These environment/tooling messages are not claimed fixed. Read-only Git also reports restricted access to the user global ignore file and LF/CRLF notices; repository-specific ignore checks and source whitespace validation are separate.

## Portrait test coverage

The retained `PortraitLayoutStaysInsideSafeArea` test passed with populated content. It expands all visible cards for four filter states at 1440×3088, 1080×2400 and 480×800, with full area and synthetic 3.5% insets: 24 geometry combinations. It checks horizontal card/text/filter bounds, preferred text height, 144-unit navigation/button height, 216-unit header, remaining scroll height and no footer overlap. The other passing interaction test additionally uses 720×1600 geometry. These are temporary world-space layout equivalents, not measured Samsung insets or rendered Device Simulator screenshots.

The final run used the installed Unity executable after the developer closed the Editor, with these arguments:

```text
-batchmode -nographics -buildTarget Android -projectPath <repository>/ComputerScienceARMuseum
-runTests -testPlatform EditMode -testFilter Iara.Tests
-testResults <repository>/ComputerScienceARMuseum/Library/I4/tests.xml
-logFile <repository>/unity-i4-tests.log
```

Omit `-quit`; the Test Runner owns shutdown. No Android build is requested by selecting the Android import target.

Manual checks still required: open Museum in the Punch Hole Center 1440×3088 portrait simulator, then 1080×2400 and a smaller portrait viewport. Expand all eight entries, scroll to the last source/availability paragraph, verify wrapping and reachable collapse/navigation controls, filter away/back, About/return and scroll reset. Confirm the cutout-safe header remains intact. Review actual Console messages and record screenshots; do not infer visual acceptance from geometry assertions.

## Remaining editorial and release work

No unresolved selected date is knowingly asserted without a supporting source. Remaining uncertainty concerns variant-specific future reconstructions, completeness of contributor lists, media rights and educational demo detail. No priority claim of “first computer”, quantified wartime benefit or exclusive individual invention is introduced. Enigma's year is not represented as a separately verified patent date.

Future model/marker choices and demo scripts require their own research and approval. Credits/licenses remain absent, so EDU-04/05 and offline AR acceptance are incomplete. Offline citation display is implemented in code; full offline Android operation remains untested.

See the [complete change inventory and human staging paths](I4_FILES.md). No files are deleted and no state-changing Git command is authorized or performed.
