# IARA — Computer Science AR Museum

IARA is a university master's project for an educational, offline-first Android museum of computer science history. The planned experience lets visitors browse exhibits, select one, scan its unique image marker, and explore a tracked 3D model. Target device: Samsung Galaxy S21. Project deadline: January 2027.

## Current status

The developer reports design phases P1–P7 as completed and approved, and repository audit #001 as approved. The repository provides a JSON catalog, a PowerShell validator, and project documentation. Task I2 initializes the Unity project in `ComputerScienceARMuseum/`; see the [setup record](docs/testing/I2_SETUP.md) for verified configuration and limitations. Unity Editor `6000.3.27f1` and Android tooling are installed. Runtime navigation, AR behavior, models, markers, hotspots, and educational demonstrations remain unimplemented.

The catalog contains 3 pavilions, 8 exhibits, and 4 demonstration identifiers. Historical text, dates, creators, sources, credits, and hotspots remain incomplete. Model and marker keys do not establish that assets exist. Historical claims, model fidelity, media licenses, and sources require verification before release.

## Planned exhibits

Pavilions correspond to the catalog's `categories` collection.

| Pavilion | Exhibit ID | Exhibit | Planned demo key |
| --- | --- | --- | --- |
| Cryptography and cryptanalysis | ENC-01 | Enigma | `enigma_signal_path` |
| Cryptography and cryptanalysis | ENC-02 | Turing-Welchman Bombe | `bombe_drum_cycle` |
| Early Computing | EAR-01 | Colossus | `colossus_tape_flow` |
| Early Computing | EAR-02 | ENIAC | `eniac_calculation_flow` |
| Personal Computing | PC-01 | Apple I | None |
| Personal Computing | PC-02 | Apple II | None |
| Personal Computing | PC-03 | IBM PC 5150 | None |
| Personal Computing | PC-04 | Macintosh 128K | None |

## Planned technology and behavior

- Unity 6.3 LTS (`6000.3.27f1`), C#, URP, AR Foundation, Google ARCore XR Plugin; direct versions are recorded in the [Unity manifest](ComputerScienceARMuseum/Packages/manifest.json), with resolved dependencies in `packages-lock.json`.
- Android ARM64 with IL2CPP; SDK, NDK, and OpenJDK supplied through Unity Hub.
- JSON for historical metadata; ScriptableObjects for Unity asset references.
- Portrait UI with pavilion filters, expandable catalog entries, and an AR HUD.
- One main prefab and one unique image marker per exhibit; at most one active AR model.
- Swipe rotation, pinch zoom, reset, and preservation of user rotation/zoom through tracking loss. Initial tracking-loss debounce target: 200 ms, subject to device validation.
- No backend, cloud services, accounts, or online runtime dependencies. Development downloads and device provisioning are separate from offline app operation.

## Architecture

Four planned layers separate Presentation, Application, AR, and Content responsibilities. Three planned scenes are `Bootstrap`, `Museum`, and `ARExperience`. See [architecture](docs/architecture/ARCHITECTURE.md) and [acceptance criteria](docs/design/ACCEPTANCE_CRITERIA.md).

## Repository structure

| Path | Purpose and status |
| --- | --- |
| [AGENTS.md](AGENTS.md) | Persistent engineering-agent conventions |
| [.gitignore](.gitignore) | Generated artifacts and local environment exclusions |
| [content/catalog/museum_catalog.json](content/catalog/museum_catalog.json) | Existing draft metadata, schema version 1 |
| `content/sources/` | Reserved for source records; currently empty |
| `assets-source/{markers,models,references}/` | Reserved source asset directories; currently empty |
| [scripts/Validate-Catalog.ps1](scripts/Validate-Catalog.ps1) | Existing Draft/Release catalog validator |
| `ComputerScienceARMuseum/` | Unity project initialized during Task I2; scene assets are scaffolding without gameplay |
| `docs/architecture/` | Architecture documentation |
| `docs/design/` | Design and acceptance documentation |
| `docs/testing/` | Reserved for test evidence; currently empty |

Empty directories are local scaffolding and are not retained by Git unless Unity folder metadata is present. Open `ComputerScienceARMuseum/` with Unity `6000.3.27f1`. `.venv/` is a local ignored environment, not an application dependency.

## Development workflow

1. Inspect existing files and [agent conventions](AGENTS.md); define one bounded task and its verification. Implementation prompts must contain fewer than 50 lines.
2. Keep changes within the approved task. Do not assume Unity or Android tooling is installed. Initialize Unity or install packages only when explicitly authorized.
3. Preserve stable catalog identifiers. Verify historical claims and asset rights before adding release content; record evidence rather than inventing facts or permissions.
4. Run relevant checks and report actual results and limitations. From the repository root, with PowerShell available:

   ```powershell
   powershell -NoProfile -File scripts/Validate-Catalog.ps1 -Mode Draft
   ```

   Draft currently permits missing editorial fields as warnings. Release mode treats those omissions, missing sources, and missing credits as errors. Neither mode proves asset availability, historical accuracy, or runtime behavior.

5. Review changes using read-only Git inspection. Codex reports every created, modified, and deleted file, verification, outstanding issues, and exact paths recommended for staging. The human developer exclusively stages, commits, and pushes; Codex must never execute state-changing Git commands.

See the [I2 setup record](docs/testing/I2_SETUP.md) for headless verification instructions. Android builds and S21/ADB tests are postponed; no deployment or runtime AR test result is claimed here.
