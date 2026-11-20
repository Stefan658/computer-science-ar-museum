# I5 — AR image tracking foundation

## Scope and baseline

Baseline: clean repository at `0c29f42`. Unity 6000.3.27f1, URP 17.3.0, AR Foundation/ARCore 6.3.5 were inspected locally. `ARExperience` had no root objects. The existing project disables automatic XR startup; I5 retains that configuration and owns provider startup inside the AR scene.

The Museum scene, UI prefabs, historical catalog copies, existing asset GUIDs, packages and Android project settings are preserved. This task adds diagnostic tracking infrastructure, not historical models, demos, gestures, Bootstrap flow or production Museum-to-AR navigation. No Android application build, physical device connection or device test is performed. No explicit ADB command is issued; Unity's Android Editor tooling can log its own ADB server lifecycle during import.

## Responsibilities and behavior

| Layer | Implementation |
| --- | --- |
| Content | `MarkerMap` derives the unique marker/exhibit lookup from the unchanged JSON; PNG source markers are local assets |
| Application | `ExhibitSelection` is the scene handoff contract; `ImageTrackingState` owns selected identity, one active trackable, status and injected-time debounce without provider/UI dependencies |
| AR | `ImageTrackingAssets` stores Unity library/prefab references; `ArTrackingController` adapts AR Foundation changes to policy and applies world poses; `ArProviderLifecycle` owns manual XR startup/cleanup |
| Presentation | `ArStatusHud` consumes `IArStatusSource` and binds TMP status inside a safe-area portrait Canvas |

The AR scene contains an AR Session, AR Input Manager, XR Origin with device tracking origin and zero camera height offset, AR Camera, Input System Tracked Pose Driver, AR Camera Manager/Background, and AR Tracked Image Manager. The pose driver uses installed handheld-AR device controls. `trackablesChanged` replaces deprecated tracked-image events. The manager references eight images, requests one moving image, and has no per-trackable visualization prefab. Application policy, rather than the provider request, enforces a single visualization.

Only a fully tracking observation whose marker maps to the selected exhibit may activate the diagnostic cube. Another recognized marker produces Wrong marker without changing selection or activating another exhibit. Multiple copies of the selected marker retain a stable active trackable while it remains tracked; a replacement may take over after loss. Unknown observations fail closed. Images not recognized by the provider produce no image event.

On Limited/None, removal, session loss or application pause, loss begins. The last valid pose remains visible during a 0.2-second grace period; the status already reports loss. Reacquisition cancels pending hiding. Sustained loss hides on the first update at/after the deadline; repeated loss events do not restart the timer. No new pose is applied from a limited observation. Pose and user-transform roots are separate: local user rotation/scale survive loss and reacquisition, while changing selected exhibit resets them. Gesture input is not implemented.

Provider startup waits for explicit application selection, initializes the existing XR loader, checks support, and on Android requests camera permission before enabling managers. No ARCore install/update is requested. Unsupported/provisioning/permission failures produce guidance. AR components stay disabled in Editor; simulated tests do not emulate a camera. Scene disable stops owned subsystems and deinitializes an owned loader. Android permission/lifecycle branches remain hardware-only verification work.

## Selection and inspection

There is **no default exhibit**. A future navigation action must call `ExhibitSelection.Select(catalog, exhibitId)` before opening `ARExperience`. Existing Museum AR actions remain disabled in this task.

For Editor inspection, open ARExperience, enter Play Mode, and open `IARA > AR > Diagnostic selection (Play Mode)`. The window selects one of the eight IDs through the Application API. It does not inject detections; without simulated events the HUD remains scanning. The automated integration test supplies deterministic observations through the same controller observation seam used by the AR adapter. A mint 4-cm diagnostic cube is shared across selections and is explicitly not any exhibit's historical model. Final per-exhibit model prefabs/asset registry remain future work.

Marker files, exact physical dimensions, feature evaluation and printing requirements are recorded in [MARKERS.md](../design/MARKERS.md).

## Verification record

Executed on 2026-11-20. Final verification is recorded below after the final input lifecycle correction; initial results are retained separately.

| Check | Actual result |
| --- | --- |
| Initial compilation attempt | Failed with duplicated policy methods; corrected before scene generation |
| Scene/library generation | Exit 0; eight mappings and 0.15 m dimensions verified |
| Initial full Unity suite | 39 passed, 0 failed; included the 22 existing I3/I4 tests |
| Initial runtime warning | AR Input Manager enabled before a loader existed in Editor; corrected to remain disabled until provider startup |
| Final Unity import/compilation/tests | Passed: 39 tests, 0 failed, 0 skipped, Unity exit 0; no C# compiler errors/warnings found |
| Marker feature evaluation | Eight scores of 100; local provider image database generated, exit 0 |
| Catalog validation and preservation | Draft and Content passed, each with 0 errors/0 warnings; both JSON copies unchanged |
| Existing identities/configuration | No tracked metadata, packages, Android project settings, Museum scene/prefabs or catalog changed |
| Physical Android tracking/build/offline deployment | Not run; outside authorized scope |

Automated coverage includes eight exact mapping cases with every selected identity (64 marker/selection pairs), source-image uniqueness and dimensions, unknown/no selection, wrong marker, removal, multiple/duplicate markers, 199/200 ms boundary, reacquisition, selection handoff, scene configuration, and a Play Mode integration that tests all eight selections, status binding, poses, one diagnostic renderer and rotation/scale preservation. Existing Museum interaction and expanded portrait geometry tests are rerun as regressions.

Ignored local evidence: root `unity-i5-generate.log`, `unity-i5-generate-final.log`, `unity-i5-lifecycle.log`, `unity-i5-tests-initial.log`, `unity-i5-tests.log`; `ComputerScienceARMuseum/Library/I5/tests-initial.xml`, `tests.xml`, `marker-scores.log`, `image-database.log`, `diagnostic.imgdb`, `catalog-draft.log`, `catalog-content.log`. No verification output should be staged.

The initial compilation failure consisted of CS0111 duplicate method definitions in the new policy class, consolidated before successful generation. The initial 39-test pass retained the early-input warning; the final 39-test pass includes the corrected activation order and no such warning. No failed compilation is represented as a successful import.

The final log retains two IL postprocessor duplicate-hint-path warnings, Android/WindowsStandalone native-extension diagnostics and Mono shutdown/debugger-listener messages. These tooling/environment diagnostics are not claimed fixed. The simulated integration test received no unexpected runtime error. Read-only Git emits global-ignore permission and line-ending notices. Source/documentation whitespace checks are separate from Unity's native scene YAML serialization.

Catalog SHA-256 remains `3A121C962A1A64639E50DD4B7F79F90AD162FA7D724475D9C003A917A2B09D5B` for both copies. No tracked `.meta` changed; existing GUIDs are retained. Inventory: 51 created files including new metadata, six modified files, zero deleted. Earlier I3/I4 reports retain their original evidence.

Final repository checks passed for six Markdown documents and 33 local references, heading/fence structure, all new Unity asset metadata pairs, and log/cache/image-database ignore coverage. New source/documentation whitespace checks and tracked source/documentation `git diff --check` passed. The separate ARExperience scene whitespace check exits 2 because Unity serializes empty YAML fields with trailing spaces (for example `m_Name: `); native serialization is retained, so an unfiltered whitespace pass is not claimed.

## API evidence and limits

Compatibility was checked against installed package source, including `ARTrackedImageManager`, `ARTrackablesChangedEventArgs`, reference-library Editor extensions, `XROrigin`, Input System Tracked Pose Driver, `ARInputManager`, and XR Management initialization APIs. See [Unity AR Foundation 6.3.5 image-manager documentation](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/manual/features/image-tracking/artrackedimagemanager.html) and [Google image feature evaluation](https://developers.google.com/ar/develop/augmented-images/arcoreimg).

Passing simulated policy tests does not verify ARCore camera recognition, real trackable events, pose accuracy, provider loss reporting, actual 200 ms timing, moving-image limits, camera permission handling, graphics/background rendering, device pause/resume, thermal behavior or offline Android operation. A future authorized device task must measure these separately on S21 with the documented print size. No package or configuration change is claimed to establish such results.

Manual GUI checks: inspect the AR scene hierarchy, library textures/dimensions and camera pose bindings; select each exhibit using the diagnostic window; verify safe-area HUD wrapping at 1440×3088 and 1080×2400 portrait. The cube cannot appear from real camera tracking in Editor with this configuration. Museum entry/return and user-facing retry navigation remain unimplemented.

See [I5_FILES.md](I5_FILES.md) for the complete change inventory and exact human staging paths. No file deletion or state-changing Git command is part of I5.
