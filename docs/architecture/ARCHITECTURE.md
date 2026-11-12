# Planned architecture

This document describes the approved direction. Task I2 adds Unity project scaffolding, empty scene assets, and Editor-only setup/verification; runtime behavior remains planned. The [draft catalog](../../content/catalog/museum_catalog.json) and [validator](../../scripts/Validate-Catalog.ps1) provide content tooling. See [project status](../../README.md), [I2 setup evidence](../testing/I2_SETUP.md), and [acceptance criteria](../design/ACCEPTANCE_CRITERIA.md).

## Four layers

| Layer | Planned responsibility | Boundary |
| --- | --- | --- |
| Presentation | Portrait catalog, pavilion filters, expandable entries, AR HUD, information panels, gesture input | Sends user intent to Application; displays state without owning AR tracking or parsing metadata |
| Application | Catalog queries, selected exhibit, scene flow, interaction coordination, single active exhibit policy | Coordinates Content access and AR capabilities through explicit contracts; avoids direct dependence on UI widgets or ARCore provider details |
| AR | AR Foundation/ARCore integration, image tracking, model placement/visibility, gesture transform application | Reports tracking state to Application; resolves the selected exhibit's marker and applies transforms without owning historical text |
| Content | JSON data, loading/validation, ScriptableObject asset registry, packaged models and markers | Supplies metadata and local asset mappings; does not navigate scenes or manage tracking |

Presentation calls Application. Application consumes Content and coordinates AR through contracts. AR may use Content asset mappings but does not control catalog navigation. Unity composition wires these boundaries; exact classes, assembly layout, and loader choices remain implementation decisions.

## Planned scenes

| Scene | Responsibility |
| --- | --- |
| `Bootstrap` | Initialize local content and application services, validate required mappings, then open Museum; show a usable error if required content cannot load |
| `Museum` | Browse/filter the catalog, expand entries, select an exhibit, and request ARExperience |
| `ARExperience` | Own the AR session, camera, image tracking, selected model, gestures, educational interactions, and AR HUD; release scene-owned resources when leaving |

Planned flow: Bootstrap → Museum → ARExperience → Museum. Selection crosses the scene boundary through Application state, not by copying historical text into scene objects. Initialization must avoid duplicate persistent services on repeated navigation. Scene loading mode and service lifetime mechanisms remain to be chosen.

## Content ownership

- JSON owns stable `exhibitId`, `categoryId`, `markerId`, `modelKey`, optional `demoKey`, editorial text, dates and their meaning, creators, sources, credits, fidelity labels, hotspots, and initial scale/orientation metadata. The current schema is version 1.
- Pavilions map to JSON categories: `cryptography`, `early-computing`, and `personal-computing`.
- Exhibit registry ScriptableObjects will map stable keys to Unity asset references: one main prefab and one unique reference image per exhibit, plus applicable demonstration assets/components. No exhibit registry exists yet; Unity rendering/XR configuration assets serve a separate purpose.
- JSON keys alone are not Unity asset references. All eight entries must resolve locally; unknown or missing mappings must produce a controlled error rather than display the wrong model.
- Initial metadata scale/orientation establishes a model baseline. User zoom/rotation is separate session state; resetting restores that baseline.
- Packaging must include JSON, models, images, educational resources, and attribution. Task I2 places a byte-identical JSON TextAsset copy under `Assets/IARA/Content/Resources/`; runtime loading and synchronization are not implemented. Packaging of other exhibit assets remains to be designed and must work on Android without network access.

## Tracking and interaction

Only the selected exhibit's marker may activate its model. At most one AR exhibit model is active, including when multiple markers are visible. A tracked pose drives a model root; user rotation and zoom are applied separately so pose updates do not erase them.

Tracking loss starts a debounce timer with an initial 200 ms target. Reacquisition before expiry cancels the pending hide. Sustained loss hides the model and updates HUD guidance without discarding user rotation/zoom. Reacquiring the same marker restores the model at its tracked pose with those user transforms preserved. Timing must be measured on S21 before finalizing the threshold.

Swipe rotates, pinch zooms within bounds to be defined during implementation, and reset restores the selected model's default transform. UI touches must not also manipulate the model. Hotspots and four planned demonstrations are driven by the selected exhibit; their educational scripts require source verification.

## Offline and platform constraints

- No backend, accounts, cloud services, remote asset loading, or online runtime dependencies.
- Source URLs are provenance records, not resources required to render an exhibit.
- Android target: ARM64, IL2CPP, portrait, Samsung Galaxy S21; Unity Hub supplies SDK/NDK/OpenJDK after tooling is confirmed.
- AR platform prerequisites must be provisioned before offline acceptance testing. Missing camera permission or unavailable AR support must yield clear guidance and a route back to the catalog.
- Package versions, Android API levels, performance budgets, and gesture bounds remain to be confirmed and documented during implementation. No build or device-test success is implied.
