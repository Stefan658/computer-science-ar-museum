using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARCore;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

namespace Iara.Editor
{
    // Explicit editor-only setup. Never runs automatically or creates gameplay objects.
    public static class IaraProjectSetup
    {
        private const string Root = "Assets/IARA";
        private const string Catalog = Root + "/Content/Resources/museum_catalog.json";
        private static readonly string[] SceneNames = { "Bootstrap", "Museum", "ARExperience" };

        public static void Configure()
        {
            Require(EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android,
                "Launch Unity with -buildTarget Android before configuring.");
            foreach (var folder in new[] { "Presentation", "Application", "AR", "Content", "Scenes", "Content/Resources" })
                EnsureFolder(Root + "/" + folder);

            PlayerSettings.companyName = "IARA";
            PlayerSettings.productName = "Computer Science AR Museum";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "ro.iara.computersciencearmuseum");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

            ConfigureRendering();
            ConfigureLoader();

            foreach (var name in SceneNames)
            {
                var path = ScenePath(name);
                if (File.Exists(path))
                    continue; // Preserve any scenes already edited by the developer.
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Require(EditorSceneManager.SaveScene(scene, path), "Could not save " + path);
            }
            EditorBuildSettings.scenes = SceneNames.Select(name => new EditorBuildSettingsScene(ScenePath(name), true)).ToArray();

            var source = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../content/catalog/museum_catalog.json"));
            if (!File.Exists(Catalog))
                File.Copy(source, Catalog, false);
            Require(File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(Catalog)), "Catalog copies differ; no overwrite performed.");
            AssetDatabase.ImportAsset(Catalog, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();
            Verify();
        }

        private static void ConfigureRendering()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            Require(pipeline != null, "Official template mobile URP asset is missing.");
            GraphicsSettings.defaultRenderPipeline = pipeline;
            var originalQuality = QualitySettings.GetQualityLevel();
            for (var index = 0; index < QualitySettings.names.Length; index++)
            {
                QualitySettings.SetQualityLevel(index, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(originalQuality, false);
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets/Settings" }))
            {
                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(guid));
                if (renderer.rendererFeatures.OfType<ARBackgroundRendererFeature>().Any())
                    continue;
                var feature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
                feature.name = "AR Background Renderer Feature";
                AssetDatabase.AddObjectToAsset(feature, renderer);
                renderer.rendererFeatures.Add(feature);
                renderer.SetDirty();
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void ConfigureLoader()
        {
            EnsureFolder("Assets/XR");
            const string path = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
            EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.k_SettingsKey, out var settings);
            if (settings == null)
            {
                Require(!File.Exists(path), "Unregistered XR settings exist; review them instead of overwriting.");
                settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(settings, path);
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settings, true);
            }
            if (settings.SettingsForBuildTarget(BuildTargetGroup.Android) == null)
                settings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            if (settings.ManagerSettingsForBuildTarget(BuildTargetGroup.Android) == null)
                settings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            var general = settings.SettingsForBuildTarget(BuildTargetGroup.Android);
            Require(XRPackageMetadataStore.AssignLoader(general.Manager, "UnityEngine.XR.ARCore.ARCoreLoader", BuildTargetGroup.Android),
                "Could not assign the Android ARCore loader.");
            // No AR session belongs in Bootstrap/Museum. ARExperience will own its lifecycle later.
            general.InitManagerOnStart = false;
            var arcore = ARCoreSettings.currentSettings;
            if (arcore == null)
            {
                const string arcorePath = "Assets/XR/ARCoreSettings.asset";
                Require(!File.Exists(arcorePath), "Unregistered ARCore settings exist; review before replacing.");
                arcore = ScriptableObject.CreateInstance<ARCoreSettings>();
                AssetDatabase.CreateAsset(arcore, arcorePath);
                ARCoreSettings.currentSettings = arcore;
            }
            arcore.requirement = ARCoreSettings.Requirement.Required;
            arcore.depth = ARCoreSettings.Requirement.Optional;
            arcore.ignoreGradleVersion = false;
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(general.Manager);
            EditorUtility.SetDirty(settings);
        }

        public static void Verify()
        {
            Require(UnityEngine.Application.unityVersion == "6000.3.27f1", "Unexpected Unity version.");
            Require(EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android, "Android is not active.");
            Require(BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android), "Android build support is unavailable.");
            Require(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP, "IL2CPP missing.");
            Require(PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64, "Expected ARM64 only.");
            Require(PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait, "Portrait missing.");
            Require(!PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) &&
                PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).SequenceEqual(new[] { GraphicsDeviceType.OpenGLES3 }), "Expected OpenGL ES 3 only.");
            Require(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset, "URP missing.");
            Require(AssetDatabase.LoadAssetAtPath<TextAsset>(Catalog) != null, "Catalog is not imported as a TextAsset.");
            var source = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../content/catalog/museum_catalog.json"));
            Require(File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(Catalog)), "Catalog copy differs from source.");
            Require(EditorBuildSettings.scenes.Select(scene => scene.path).SequenceEqual(SceneNames.Select(ScenePath)), "Unexpected scene order.");
            foreach (var name in SceneNames)
                Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath(name)) != null, "Missing scene " + name);
            var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            Require(general != null && general.Manager != null, "Android XR settings missing.");
            Require(!general.InitManagerOnStart, "XR must not initialize automatically in Bootstrap.");
            Require(general.Manager.activeLoaders.Count == 1 && general.Manager.activeLoaders[0].GetType().FullName == "UnityEngine.XR.ARCore.ARCoreLoader", "Expected only ARCore loader.");
            Require(ARCoreSettings.currentSettings != null && ARCoreSettings.currentSettings.depth == ARCoreSettings.Requirement.Optional,
                "Depth must not be required for image tracking.");
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets/Settings" }))
                Require(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(guid)).rendererFeatures.OfType<ARBackgroundRendererFeature>().Any(),
                    "AR background renderer feature missing.");
            foreach (var package in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                .Where(info => info.name == "com.unity.render-pipelines.universal" || info.name == "com.unity.xr.arcore" || info.name == "com.unity.xr.arfoundation"))
                Debug.Log("IARA_PACKAGE: " + package.name + "@" + package.version);
            Debug.Log("IARA_I2_VERIFY_PASSED: Android ARM64 IL2CPP portrait; URP; ARCore configured; three scene assets; catalog TextAsset. No build/device/runtime AR test performed.");
        }

        private static string ScenePath(string name) => Root + "/Scenes/" + name + ".unity";

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
