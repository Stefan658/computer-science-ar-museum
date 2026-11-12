# Task I2 — Unity project setup

## Scope

Initialize `ComputerScienceARMuseum/` with installed Unity `6000.3.27f1` and Android tooling. No gameplay, catalog navigation, AR session objects, image tracking, models, markers, or demos are implemented. S21/ADB testing and Android builds are postponed.

The project originates from the installed official `com.unity.template.3d-cross-platform-17.0.14.tgz` archive. Its package metadata identifies it as `com.unity.template.urp-blank`, display name **3D URP**. Unity resolves URP to the Editor's built-in `17.3.0`. AR Foundation and ARCore are pinned to `6.3.5`, listed as released for Unity 6000.3 by the official package documentation.

## Intended configuration

- Android, ARM64 only, IL2CPP, portrait orientation, OpenGL ES 3.
- Template minimum Android API 25 is retained; target API remains Automatic (highest installed). SDK platform directories observed: 34, 35, 36, and 37.0. NDK reports `27.2.12479018` (r27c); OpenJDK reports `17.0.18`. No toolchain build has been attempted.
- Mobile URP pipeline used for the default and all quality levels; AR background renderer feature configured without adding a scene camera or AR session.
- Android ARCore loader only. Automatic XR initialization is disabled: a future ARExperience implementation must own initialization and shutdown, keeping Bootstrap/Museum outside an AR session.
- ARCore is Required, depth is Optional, and Gradle version validation remains enabled. Package-generated SimulationLoader/settings assets are retained but no simulation loader is assigned to a build target; no XR simulation test was run.
- Empty `Bootstrap`, `Museum`, and `ARExperience` scene assets in that build order. No automatic scene navigation is implemented.
- Four folders under `Assets/IARA/`: Presentation, Application, AR, Content. Editor setup code is isolated under `Assets/IARA/Editor/`.
- Byte-identical catalog copy at `Assets/IARA/Content/Resources/museum_catalog.json`, imported as a local TextAsset. The repository catalog remains authoritative; synchronization is manual until a separately authorized content pipeline exists.
- Official template sample scene and tutorial assets are retained to avoid deletion; SampleScene is excluded from the IARA build scene list. Optional direct template packages for AI Navigation, Version Control, Rider, Visual Studio, Multiplayer Center, Timeline, and Visual Scripting are removed. Default built-in modules remain.

## Reproduce headless verification

Close any Editor instance using this project. From the repository root, use the actual installed Editor path:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe' `
  -batchmode -nographics -quit -buildTarget Android `
  -projectPath "$PWD\ComputerScienceARMuseum" `
  -executeMethod Iara.Editor.IaraProjectSetup.Verify `
  -logFile "$PWD\unity-i2-verify.log"
```

Check the process exit and the log marker `IARA_I2_VERIFY_PASSED`; process launch alone does not prove success. `Iara.Editor.IaraProjectSetup.Configure` is an explicit setup operation, not a read-only test: it changes project settings and build scene order, creates missing scene assets, and copies the catalog only if absent. It preserves existing scenes and refuses to overwrite a differing catalog.

## Verification record

Executed on 2026-11-12 on the Windows development machine, without device testing:

| Check | Observed result |
| --- | --- |
| Official template creation and headless import | Completed; log ends with return code 0 |
| `Iara.Editor.IaraProjectSetup.Configure` | Process exit 0; `IARA_I2_VERIFY_PASSED` recorded |
| Separate process running `Iara.Editor.IaraProjectSetup.Verify` | Process exit 0; `IARA_I2_VERIFY_PASSED` recorded after reopening the project |
| Resolved primary packages | URP `17.3.0`, AR Foundation `6.3.5`, ARCore `6.3.5`; XR Management `4.6.1` and Core Utils `2.6.0` are transitive dependencies |
| Manifest and package lock | All direct dependency versions match |
| Project configuration | Android build support reported available; ARM64/IL2CPP/portrait/OpenGL ES 3/URP/ARCore and saved scene order verified by Editor APIs |
| Scene scaffolding | Three `.unity` files with metadata; no GameObject entries in their serialized files |
| Layer folders | Presentation, Application, AR, Content exist with Unity metadata |
| Catalog | Imported TextAsset; SHA-256 `015692287FB4D63AC1C55F2AEC3F7EA46D2B6A34BBB9AFDE0ECB57B38604C10D` matches the unchanged repository source |
| Existing validator | SHA-256 unchanged; Draft passed with 0 errors and 48 expected editorial warnings |
| Documentation and ignore rules | Markdown structure/internal paths checked; caches, logs, local environment, and build artifacts remain ignored |
| Android build, device/ADB, Play Mode, visual rendering, offline runtime | Not run; no success claimed |

Local evidence logs are ignored: `unity-i2-create.log`, `unity-i2-create-elevated.log`, `unity-i2-configure.log`, and `unity-i2-verify.log`. The initial sandbox attempt could not access local Unity caches/licensing; approved execution outside the sandbox resolved the license. An initial compilation before AR packages were installed reported missing XR namespaces; subsequent compilation with resolved packages succeeded.

The headless logs contain native-extension messages for Android/WindowsStandalone, shader fallback/Null graphics messages during initial import, and Mono debugger/thread cleanup messages at shutdown. Despite these diagnostics, configuration and fresh-process verification exited 0, and `BuildPipeline.IsBuildTargetSupported` returned true for Android. These checks do not establish Android build or visual rendering correctness. Review those diagnostics in the GUI before building; do not classify them as fixed by this setup.

See the [file inventory](I2_FILES.md) for every non-generated file created or modified by I2. Earlier Task #002 files were already present and untracked at the start; AGENTS.md and ACCEPTANCE_CRITERIA.md were not changed by I2.

## Follow-up checks

Review Player Settings and XR Plug-in Management → Project Validation in the GUI before the first Android build. Review rendering visually once cameras/content exist. Future tasks must implement scene flow, AR lifecycle, permission handling, markers, models, asset registry, and content loading. No runtime acceptance criterion passes solely because project setup succeeds.

Active build target selection is local Editor state. On a fresh clone, select Android in Build Profiles or pass `-buildTarget Android`; this setup does not add a shared custom Build Profile asset. Confirm Hub-provided tool paths in Editor preferences before a build. No remaining I2 setting required GUI-only authoring, but GUI project validation and visual review remain unperformed.

## Official references

- [Unity 6000.3 command-line arguments](https://docs.unity.com/en-us/engine/6000.3/manual/unity-editor/command-line-arguments/editor)
- [AR Foundation for Unity 6000.3](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-xr-arfoundation)
- [ARCore for Unity 6000.3](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-xr-arcore)
- [AR Foundation URP configuration](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/manual/project-setup/universal-render-pipeline.html)
