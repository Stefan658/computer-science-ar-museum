using System;
using System.IO;
using System.Linq;
using Iara.AR;
using Iara.Content;
using Iara.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

namespace Iara.Editor
{
    public static class ArFoundationBuilder
    {
        public const string ScenePath = "Assets/IARA/Scenes/ARExperience.unity";
        public const string AssetsPath = "Assets/IARA/AR/Assets";
        public const string MarkersPath = "Assets/IARA/Content/Markers";
        public const string LibraryPath = AssetsPath + "/MuseumImages.asset";
        public const string RegistryPath = AssetsPath + "/ImageTrackingAssets.asset";
        private const string CatalogPath = "Assets/IARA/Content/Resources/museum_catalog.json";
        [MenuItem("IARA/AR/Build tracking foundation (empty scene only)")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            for (var i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes first.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            if (scene.GetRootGameObjects().Length != 0)
            {
                if (scene.GetRootGameObjects().Any(root => root.GetComponentInChildren<ArTrackingController>() != null)) { Verify(); return; }
                throw new InvalidOperationException("ARExperience contains authored objects; no overwrite performed.");
            }
            var catalogAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
            var catalog = MuseumCatalog.Parse(catalogAsset.text);
            var outputs = catalog.exhibits.Select(item => MarkersPath + "/" + item.markerId + ".png")
                .Concat(new[] { LibraryPath, RegistryPath, AssetsPath + "/Diagnostic.prefab", AssetsPath + "/Diagnostic.mat" });
            foreach (var output in outputs)
                if (File.Exists(output) || File.Exists(output + ".meta")) throw new InvalidOperationException("Existing asset preserved: " + output);
            Folder(AssetsPath); Folder(MarkersPath);
            var library = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
            library.name = "Museum diagnostic image markers";
            AssetDatabase.CreateAsset(library, LibraryPath);
            for (var i = 0; i < catalog.exhibits.Length; i++)
            {
                var item = catalog.exhibits[i];
                var path = MarkersPath + "/" + item.markerId + ".png";
                CreateMarker(path, i + 1);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false; importer.maxTextureSize = 512;
                importer.SaveAndReimport();
                library.Add();
                library.SetName(i, item.markerId);
                library.SetTexture(i, AssetDatabase.LoadAssetAtPath<Texture2D>(path), true);
                library.SetSpecifySize(i, true);
                library.SetSize(i, new Vector2(0.15f, 0.15f));
            }
            EditorUtility.SetDirty(library);
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("Installed URP Unlit shader missing.");
            var material = new Material(shader) { name = "Diagnostic mint (not a historical model)" };
            material.SetColor("_BaseColor", new Color(0.2f, 0.9f, 0.7f));
            AssetDatabase.CreateAsset(material, AssetsPath + "/Diagnostic.mat");
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Diagnostic cube - NOT an exhibit model";
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.transform.localScale = Vector3.one * 0.04f;
            cube.transform.localPosition = new Vector3(0, 0.02f, 0);
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            var prefab = PrefabUtility.SaveAsPrefabAsset(cube, AssetsPath + "/Diagnostic.prefab");
            UnityEngine.Object.DestroyImmediate(cube);
            var assets = ScriptableObject.CreateInstance<ImageTrackingAssets>();
            assets.Configure(library, prefab);
            AssetDatabase.CreateAsset(assets, RegistryPath);

            var sessionObject = new GameObject("AR Session");
            var session = sessionObject.AddComponent<ARSession>();
            session.enabled = false; session.attemptUpdate = false;
            var inputManager = sessionObject.AddComponent<ARInputManager>(); inputManager.enabled = false;
            var originObject = new GameObject("XR Origin");
            var origin = originObject.AddComponent<XROrigin>();
            var offset = new GameObject("Camera Offset"); offset.transform.SetParent(origin.transform, false);
            origin.CameraFloorOffsetObject = offset;
            origin.CameraYOffset = 0;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
            var cameraObject = new GameObject("AR Camera"); cameraObject.transform.SetParent(offset.transform, false);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f; camera.farClipPlane = 20;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var cameraManager = cameraObject.AddComponent<ARCameraManager>(); cameraManager.enabled = false;
            cameraManager.requestedFacingDirection = CameraFacingDirection.World;
            var background = cameraObject.AddComponent<ARCameraBackground>(); background.enabled = false;
            var driver = cameraObject.AddComponent<TrackedPoseDriver>();
            var position = new InputAction("AR position", binding: "<HandheldARInputDevice>/devicePosition", expectedControlType: "Vector3");
            var rotation = new InputAction("AR rotation", binding: "<HandheldARInputDevice>/deviceRotation", expectedControlType: "Quaternion");
            var tracking = new InputAction("AR tracking", binding: "<HandheldARInputDevice>/trackingState", expectedControlType: "Integer");
            driver.positionInput = new InputActionProperty(position);
            driver.rotationInput = new InputActionProperty(rotation);
            driver.trackingStateInput = new InputActionProperty(tracking);
            origin.Camera = camera;
            var manager = originObject.AddComponent<ARTrackedImageManager>(); manager.enabled = false;
            manager.referenceLibrary = library;
            manager.requestedMaxNumberOfMovingImages = 1;
            var poseRoot = new GameObject("Selected Exhibit Pose").transform; poseRoot.SetParent(origin.transform, false);
            var userRoot = new GameObject("Preserved User Transform").transform; userRoot.SetParent(poseRoot, false);
            var controller = originObject.AddComponent<ArTrackingController>();
            controller.Configure(catalogAsset, assets, manager, poseRoot, userRoot);
            var lifecycle = sessionObject.AddComponent<ArProviderLifecycle>();
            lifecycle.Configure(session, manager, cameraManager, background, controller);
            lifecycle.ConfigureInput(inputManager);
            BuildHud(controller);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save ARExperience.");
            Verify();
            Debug.Log("IARA_I5_FOUNDATION_BUILT: 8 image entries; diagnostic scene only, no hardware claim.");
        }
        [MenuItem("IARA/AR/Apply I5 input lifecycle configuration")]
        public static void ConfigureInputLifecycle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            for (var i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes first.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var lifecycle = UnityEngine.Object.FindFirstObjectByType<ArProviderLifecycle>();
            if (lifecycle == null || UnityEngine.Object.FindFirstObjectByType<ArTrackingController>() == null)
                throw new InvalidOperationException("Generated I5 foundation required.");
            var input = lifecycle.GetComponent<ARInputManager>();
            if (input == null) throw new InvalidOperationException("AR input component missing.");
            input.enabled = false; lifecycle.ConfigureInput(input);
            EditorUtility.SetDirty(input); EditorUtility.SetDirty(lifecycle);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save AR input lifecycle.");
            Verify();
        }
        public static void Verify()
        {
            var catalog = MuseumCatalog.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath).text);
            var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
            if (library == null || library.count != 8) throw new InvalidOperationException("Eight reference images required.");
            var map = new MarkerMap(catalog);
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var image in library)
                if (!map.TryGetExhibit(image.name, out _) || !seen.Add(image.name) || image.texture == null ||
                    !image.specifySize || Mathf.Abs(image.width - 0.15f) > 0.0001f || Mathf.Abs(image.height - 0.15f) > 0.0001f)
                    throw new InvalidOperationException("Image library mismatch.");
            Debug.Log("IARA_I5_LIBRARY_VERIFIED");
        }
        private static void BuildHud(ArTrackingController controller)
        {
            var canvasObject = new GameObject("AR Status HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 2400); scaler.matchWidthOrHeight = 0;
            var safe = new GameObject("Safe Area", typeof(RectTransform), typeof(SafeAreaPanel));
            safe.transform.SetParent(canvasObject.transform, false);
            var safeRect = (RectTransform)safe.transform; safeRect.anchorMin = Vector2.zero; safeRect.anchorMax = Vector2.one; safeRect.sizeDelta = Vector2.zero;
            var panel = new GameObject("Status Panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(safe.transform, false);
            panel.GetComponent<Image>().color = new Color(0.03f, 0.06f, 0.08f, 0.94f);
            var rect = (RectTransform)panel.transform; rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1); rect.sizeDelta = new Vector2(-64, 440); rect.anchoredPosition = new Vector2(0, -32);
            var labelObject = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI)); labelObject.transform.SetParent(panel.transform, false);
            var labelRect = (RectTransform)labelObject.transform; labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(32, 24); labelRect.offsetMax = new Vector2(-32, -24);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (label.font == null) throw new InvalidOperationException("Existing TMP font missing.");
            label.fontSize = 48; label.color = Color.white; label.raycastTarget = false;
            label.text = "Scanning / choose an exhibit. Diagnostic visuals only.";
            canvasObject.AddComponent<ArStatusHud>().Configure(controller, label);
        }
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            Folder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
        private static void CreateMarker(string path, int seed)
        {
            // Multiscale, nonrepeating geometry. A diagnostic texture, not a historical illustration or QR code.
            const int size = 512;
            var pixels = Enumerable.Repeat(new Color32(238, 235, 218, 255), size * size).ToArray();
            var random = new System.Random(5100 + seed * 7919);
            for (var feature = 0; feature < 950; feature++)
            {
                var x = random.Next(4, 500); var y = random.Next(4, 500);
                var width = random.Next(3, feature < 100 ? 90 : 23); var height = random.Next(3, feature < 100 ? 70 : 20);
                var dark = (byte)random.Next(8, 150);
                var color = feature % 4 == 0 ? new Color32(240, 230, 190, 255) : new Color32(dark, (byte)(dark / 2), (byte)(dark + 45), 255);
                for (var py = y; py < Math.Min(y + height, size - 4); py++)
                    for (var px = x; px < Math.Min(x + width, size - 4); px++)
                        if (feature % 3 != 0 || px - x < (py - y) * width / (float)height) pixels[py * size + px] = color;
            }
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
