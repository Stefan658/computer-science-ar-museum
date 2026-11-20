# I4 file inventory

Paths are repository-relative. The human developer exclusively stages, commits and pushes. This inventory excludes ignored compiler outputs, Unity caches, logs and fixture JSON. No file is intentionally deleted.

## Created files

| Path | Purpose |
| --- | --- |
| `ComputerScienceARMuseum/Assets/IARA/Tests/Editor/CatalogContentTests.cs` | Eight exhibit cases, invalid-content cases, byte/GUID preservation and all-card Play Mode binding |
| `ComputerScienceARMuseum/Assets/IARA/Tests/Editor/CatalogContentTests.cs.meta` | Unity-generated identity for the new test asset |
| `scripts/Test-CatalogValidation.ps1` | Nine validator regression checks with ignored local fixtures |
| `content/sources/HISTORICAL_SOURCES.md` | External research ledger, attribution boundaries and editorial decisions |
| `docs/architecture/CATALOG_SCHEMA.md` | Schema 2 contract and source/bundle editing workflow |
| `docs/testing/I4_CONTENT.md` | Implementation, verification, limitations and manual follow-up |
| `docs/testing/I4_FILES.md` | Complete I4 source/document inventory and staging recommendations |

## Modified files

| Path | Change |
| --- | --- |
| `.gitignore` | Ignore local root `unity-i4-*.log`; existing Unity/Android rules retained |
| `README.md` | Current content status, research/schema/report links and Content validation workflow |
| `content/catalog/museum_catalog.json` | Schema 2, eight historical records, source metadata/field associations and scope notes |
| `ComputerScienceARMuseum/Assets/IARA/Content/Resources/museum_catalog.json` | Byte-identical bundled copy; existing `.meta` retained |
| `ComputerScienceARMuseum/Assets/IARA/Content/MuseumCatalog.cs` | Historical DTOs and strict bundled-content validation |
| `ComputerScienceARMuseum/Assets/IARA/Presentation/ExhibitCardView.cs` | Year, historical sections, numbered source titles and explicit availability state |
| `ComputerScienceARMuseum/Assets/IARA/Tests/Editor/MuseumUiTests.cs` | Invalid-input fixture updated to schema 2; existing behavior/layout tests retained |
| `scripts/Validate-Catalog.ps1` | Content mode, historical/source integrity, immutable mappings and bundle hash check |
| `docs/architecture/ARCHITECTURE.md` | Current schema, layer ownership and guarded manual synchronization |
| `docs/design/ACCEPTANCE_CRITERIA.md` | Link I4 evidence without claiming final acceptance |

## Exact recommended staging paths

Review these paths with read-only Git tools before manually staging them. This is a path list, not a command executed by Codex.

```text
.gitignore
README.md
content/catalog/museum_catalog.json
content/sources/HISTORICAL_SOURCES.md
scripts/Validate-Catalog.ps1
scripts/Test-CatalogValidation.ps1
docs/architecture/ARCHITECTURE.md
docs/architecture/CATALOG_SCHEMA.md
docs/design/ACCEPTANCE_CRITERIA.md
docs/testing/I4_CONTENT.md
docs/testing/I4_FILES.md
ComputerScienceARMuseum/Assets/IARA/Content/MuseumCatalog.cs
ComputerScienceARMuseum/Assets/IARA/Content/Resources/museum_catalog.json
ComputerScienceARMuseum/Assets/IARA/Presentation/ExhibitCardView.cs
ComputerScienceARMuseum/Assets/IARA/Tests/Editor/MuseumUiTests.cs
ComputerScienceARMuseum/Assets/IARA/Tests/Editor/CatalogContentTests.cs
ComputerScienceARMuseum/Assets/IARA/Tests/Editor/CatalogContentTests.cs.meta
```

Do not stage `ComputerScienceARMuseum/Library/`, `unity-i4-tests.log`, local compiler response files/binaries, or negative fixtures. Scenes, prefabs, packages, project settings and existing asset metadata are outside the intended diff.
