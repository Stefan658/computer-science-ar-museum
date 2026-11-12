# Planned acceptance criteria

These are test requirements, not test results. All runtime criteria are pending implementation and verification. See [architecture](../architecture/ARCHITECTURE.md) and the [catalog](../../content/catalog/museum_catalog.json).

For each executed criterion, record build identifier, Unity/package versions, device and Android version, prerequisites, steps, expected/observed results, and evidence in `docs/testing/`. Mark each result Pass, Fail, or Blocked; do not infer runtime success from catalog validation.

## Catalog navigation

| ID | Test and expected result |
| --- | --- |
| CAT-01 | Launch the configured app. Bootstrap loads bundled content and opens Museum without duplicate initialization. Missing/invalid content produces a readable error rather than a crash. |
| CAT-02 | Show all exhibits: exactly ENC-01, ENC-02, EAR-01, EAR-02, PC-01, PC-02, PC-03, PC-04 appear once, with titles matching JSON. |
| CAT-03 | Select each pavilion filter: cryptography shows ENC-01/02, early-computing shows EAR-01/02, personal-computing shows PC-01/02/03/04. Restore the full catalog and verify all eight entries. Pavilion order follows `displayOrder`. |
| CAT-04 | Expand and collapse each entry. Display its associated verified metadata without mixing content from another entry; controls remain usable in portrait on S21. |
| CAT-05 | Select each exhibit and enter ARExperience. The selected identity is correct; return to Museum and select another exhibit without retaining the previous active model. |

## Image tracking

Prerequisites: all eight approved marker images, their recorded physical sizes, mapped prefabs, camera permission, and provisioned AR support. Record marker print size, distance, lighting, and timing method.

| ID | Test and expected result |
| --- | --- |
| AR-01 | For each selected exhibit, scan its marker. Exactly its main model appears and follows the marker pose. Scan a different exhibit's marker: it does not activate a different model. |
| AR-02 | Present multiple exhibit markers simultaneously. At most one exhibit model is active, and it belongs to the selected exhibit. |
| AR-03 | Inject or measure a loss shorter than the configured debounce. Reacquisition cancels hiding; the model does not disappear because of that transient loss. Initial target: 200 ms. |
| AR-04 | Sustain loss beyond the configured debounce. The model hides and HUD shows scan/reacquisition guidance. Use timestamped events to verify hiding on the first update at/after expiry, allowing the measured update interval. |
| AR-05 | Rotate and zoom, lose tracking beyond debounce, then reacquire the same marker. The model reappears at the current marker pose with the same stored user rotation and zoom. |
| AR-06 | Deny camera permission or make AR support unavailable. Show actionable guidance without a crash and allow return to the catalog. |

## Gestures

| ID | Test and expected result |
| --- | --- |
| GES-01 | Swipe over the model interaction area. User rotation changes while the tracked pose remains controlled by tracking. |
| GES-02 | Pinch inward/outward. Scale changes smoothly within documented positive minimum/maximum bounds; it never becomes zero, negative, or non-finite. Bounds must be defined before this test can pass. |
| GES-03 | Rotate and zoom, then reset. Default user rotation/zoom is restored relative to the exhibit's configured initial orientation/scale without moving the tracking root away from the marker. |
| GES-04 | Tap/drag HUD controls or information panels. UI actions do not also rotate/zoom the model. Repeat after tracking loss and reacquisition. |

## Educational interactions and content

| ID | Test and expected result |
| --- | --- |
| EDU-01 | Activate each configured hotspot. The correct exhibit-specific information is shown; closing it returns control to the AR experience. An exhibit with no hotspots has no broken hotspot controls. |
| EDU-02 | Run `enigma_signal_path`, `bombe_drum_cycle`, `colossus_tape_flow`, and `eniac_calculation_flow` on their corresponding exhibits. Each responds visibly to its documented trigger and can be replayed from its documented starting state. Approve source-backed steps and expected outcomes before implementation; this criterion remains blocked until those scripts exist. |
| EDU-03 | Open PC-01 through PC-04. No unavailable demonstration action is offered for their null `demoKey` values. |
| EDU-04 | Before release, verify historical descriptions, dates/year meanings, creators, model fidelity labels, sources, and asset credits/licenses for every exhibit against recorded evidence. Verify permission and required attribution for each distributed asset; do not treat an unverified asset as cleared. |
| EDU-05 | Run the catalog validator in Release mode: zero errors. Separately verify all marker/model/demo keys resolve to bundled assets; validator success alone is insufficient. |

## Offline operation

| ID | Test and expected result |
| --- | --- |
| OFF-01 | After installation and AR prerequisite provisioning, disable Wi-Fi/mobile data and cold-launch the app. Browse all eight exhibits, scan each marker, use gestures/hotspots, and replay all four demos without a download, login, or network-related blockage. |
| OFF-02 | Inspect runtime dependencies and content-loading paths and observe app traffic during OFF-01. No feature requires a backend, cloud service, account, remote asset, or online runtime request. Source links do not need to be fetched to display bundled content. |

## Android deployment

| ID | Test and expected result |
| --- | --- |
| AND-01 | Inspect project settings/build evidence: Unity `6000.3.27f1`, URP, compatible recorded AR Foundation/ARCore versions, Android target, ARM64, IL2CPP, and portrait orientation. Confirm Hub-provided SDK/NDK/OpenJDK paths. |
| AND-02 | Build and install on Samsung Galaxy S21. Launch, grant camera permission, complete Museum → ARExperience → Museum, background/resume, and relaunch without crashes or duplicate active models/sessions. Record device/Android/build details. |
| AND-03 | Execute the catalog, tracking, gesture, educational, and offline criteria on S21. Record frame timing, memory observations, and tracking behavior. Numeric performance budgets remain to be approved; do not claim performance acceptance before budgets and measurements exist. |

## Open decisions

Confirm package versions, Android API levels, gesture bounds, repeatable marker test conditions, demonstration scripts, and performance budgets before their dependent criteria can be accepted. The 200 ms debounce is an initial target; any adjustment requires documented measurement and an explicit criterion update.
