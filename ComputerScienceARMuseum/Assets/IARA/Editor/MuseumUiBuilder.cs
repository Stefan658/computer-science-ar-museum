using System;
using System.IO;
using Iara.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Iara.Editor
{
    public static class MuseumUiBuilder
    {
        private const string ScenePath = "Assets/IARA/Scenes/Museum.unity";
        private const string Prefabs = "Assets/IARA/Presentation/Prefabs";
        private static readonly Color Ink = new Color32(15, 27, 38, 255);
        private static readonly Color Panel = new Color32(28, 44, 57, 255);
        private static readonly Color Light = new Color32(239, 244, 247, 255);
        private static readonly Color Muted = new Color32(185, 203, 214, 255);
        private static readonly Color Mint = new Color32(119, 230, 196, 255);
        private static TMP_FontAsset font;

        [MenuItem("IARA/Museum/1. Import bundled TMP essentials")]
        public static void ImportFonts()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset") != null)
            {
                if (UnityEngine.Application.isBatchMode) EditorApplication.Exit(0);
                return;
            }
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui/package.json");
            if (package == null) throw new InvalidOperationException("Installed uGUI package was not found.");
            AssetDatabase.importPackageCompleted += OnTmpImported;
            AssetDatabase.importPackageFailed += OnTmpImportFailed;
            AssetDatabase.ImportPackage(Path.Combine(package.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage"), false);
        }

        private static void OnTmpImported(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnTmpImported;
            AssetDatabase.importPackageFailed -= OnTmpImportFailed;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var imported = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset") != null;
            if (imported) Debug.Log("IARA_I3_TMP_IMPORTED: verified bundled font asset after package completion.");
            else Debug.LogError("IARA TMP import completed without the required font asset.");
            if (UnityEngine.Application.isBatchMode) EditorApplication.Exit(imported ? 0 : 1);
        }

        private static void OnTmpImportFailed(string packageName, string error)
        {
            AssetDatabase.importPackageCompleted -= OnTmpImported;
            AssetDatabase.importPackageFailed -= OnTmpImportFailed;
            Debug.LogError("IARA TMP import failed: " + error);
            if (UnityEngine.Application.isBatchMode) EditorApplication.Exit(1);
        }

        [MenuItem("IARA/Museum/2. Build Museum UI")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before building Museum UI.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) throw new InvalidOperationException("Run IARA/Museum/1. Import bundled TMP essentials first.");
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            if (scene.isDirty) throw new InvalidOperationException("Museum contains unsaved work. Save or review it before building UI.");
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<MuseumScreen>(true) != null)
                {
                    Debug.Log("IARA_I3_UI_ALREADY_EXISTS: preserving existing UI and prefab edits.");
                    if (openedHere) EditorSceneManager.CloseScene(scene, true);
                    return;
                }
                if (root.GetComponentInChildren<EventSystem>(true) != null)
                    throw new InvalidOperationException("Museum already contains an EventSystem; review its input module before generating another.");
            }
            var previousActiveScene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            try
            {
            EnsureFolder(Prefabs);
            var filter = CreateFilterPrefab();
            var card = CreateCardPrefab();
            var canvasObject = new GameObject("Museum UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 2400);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0;
            var screen = canvasObject.AddComponent<MuseumScreen>();
            var background = Rect("Background", canvasObject.transform); Stretch(background);
            Image(background, Ink);
            var safe = Rect("Safe area", background); Stretch(safe); safe.gameObject.AddComponent<SafeAreaPanel>();
            var page = Rect("Page", safe); Stretch(page); page.offsetMin = new Vector2(48, 36); page.offsetMax = new Vector2(-48, -36);
            var column = Vertical(page, 24); column.childForceExpandHeight = false;
            var header = Rect("Header", page); Vertical(header, 8); Height(header, 216);
            Text("Wordmark", header, "IARA / THE COLLECTION", 36, Mint, 46);
            Text("Title", header, "Computer Science\nAR Museum", 68, Light, 154);
            var catalogPanel = Rect("Catalog", page); Vertical(catalogPanel, 20); Flexible(catalogPanel);
            Text("Pavilion label", catalogPanel, "CHOOSE A PAVILION", 32, Muted, 44);
            var filterRoot = Rect("Pavilion filters", catalogPanel); Height(filterRoot, 304);
            var grid = filterRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
            grid.cellSize = new Vector2(480, 144); grid.spacing = new Vector2(16, 16);
            // Width scales with the page; keeps two columns inside the safe area.
            filterRoot.gameObject.AddComponent<FilterGridSize>();
            var results = Text("Results", catalogPanel, "Loading local catalog...", 34, Muted, 60);
            var scroller = Rect("Exhibit scroll", catalogPanel); Flexible(scroller);
            var scroll = scroller.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 80; scroll.decelerationRate = 0.12f;
            var viewport = Rect("Viewport", scroller); Stretch(viewport); Image(viewport, Ink);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Exhibit cards", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1); content.sizeDelta = Vector2.zero;
            Vertical(content, 24);
            var contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            var about = Rect("About", page); Vertical(about, 32); Flexible(about); Image(about, Panel);
            about.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(32, 32, 32, 32);
            Text("Heading", about, "Explore at your own pace", 52, Light, 160);
            Text("Description", about,
                "Browse eight exhibits across three pavilions. Expand a card to read the locally bundled catalog.\n\nThis is a UI foundation. Historical descriptions are still under review. AR exploration and educational demonstrations are not available yet.",
                42, Light, 600);
            var close = Button("Back to collection", about, 48, 144);
            var spacer = Rect("Space", about); Flexible(spacer);
            var status = Text("Status", page, "Offline catalog", 30, Muted, 84);
            var footer = Rect("Navigation", page); Height(footer, 144);
            var row = footer.gameObject.AddComponent<HorizontalLayoutGroup>(); row.spacing = 16;
            row.childControlHeight = row.childControlWidth = true; row.childForceExpandWidth = true;
            row.childForceExpandHeight = false;
            var home = Button("Collection", footer, 36, 144);
            var top = Button("Back to top", footer, 36, 144);
            var info = Button("About", footer, 36, 144);
            about.gameObject.SetActive(false);
            var data = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/IARA/Content/Resources/museum_catalog.json");
            if (data == null) throw new InvalidOperationException("The existing local catalog TextAsset is missing.");
            screen.Configure(data, filter, card, filterRoot, content, scroll, results, status, home, top, info, close, about.gameObject, catalogPanel.gameObject, page);
            var eventSystem = new GameObject("Museum EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);
            }
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("IARA_I3_UI_BUILT: Museum scene, two reusable prefabs, local catalog, uGUI/TMP. No AR behavior added.");
        }

        [MenuItem("IARA/Museum/3. Apply portrait layout correction")]
        public static void ApplyPortraitLayoutCorrection()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying layout changes.");
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            if (scene.isDirty) throw new InvalidOperationException("Save or review unsaved Museum work first.");
            MuseumScreen screen = null;
            foreach (var root in scene.GetRootGameObjects())
                if ((screen = root.GetComponentInChildren<MuseumScreen>(true)) != null) break;
            if (screen == null) throw new InvalidOperationException("Generate the Museum UI before applying its layout correction.");

            var cardPath = Prefabs + "/ExhibitCard.prefab";
            var card = PrefabUtility.LoadPrefabContents(cardPath);
            try
            {
                ConstrainWidth((RectTransform)card.transform);
                NormalizeLayout(card);
                PrefabUtility.SaveAsPrefabAsset(card, cardPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(card); }

            NormalizeLayout(screen.gameObject);
            var safe = screen.GetComponentInChildren<SafeAreaPanel>(true);
            var page = (RectTransform)safe.transform.Find("Page");
            var navigation = (RectTransform)page.Find("Navigation");
            Height(navigation, 144);
            navigation.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = false;
            foreach (var button in navigation.GetComponentsInChildren<Button>(true))
                Height((RectTransform)button.transform, 144);
            var scroll = page.GetComponentInChildren<ScrollRect>(true);
            scroll.content.anchorMin = new Vector2(0, 1);
            scroll.content.anchorMax = Vector2.one;
            scroll.content.pivot = new Vector2(0.5f, 1);
            scroll.content.sizeDelta = new Vector2(0, scroll.content.sizeDelta.y);
            var fitter = scroll.content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var serializedScreen = new SerializedObject(screen);
            serializedScreen.FindProperty("layoutRoot").objectReferenceValue = page;
            serializedScreen.ApplyModifiedPropertiesWithoutUndo();
            LayoutRebuilder.ForceRebuildLayoutImmediate(page);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Museum layout.");
            AssetDatabase.SaveAssets();
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("IARA_I3_PORTRAIT_LAYOUT_UPDATED: constrained widths, fixed navigation, vertical-only content fitting. Header/safe-area/scaler settings preserved.");
        }

        private static void NormalizeLayout(GameObject root)
        {
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.parent != null && rect.parent.GetComponent<HorizontalOrVerticalLayoutGroup>() != null)
                    ConstrainWidth(rect);
                var sizing = rect.GetComponent<LayoutElement>();
                if (sizing == null) continue;
                if (sizing.flexibleHeight > 0)
                    sizing.minHeight = sizing.preferredHeight = 0;
                else
                    sizing.flexibleHeight = 0;
            }
        }

        private static PavilionFilterView CreateFilterPrefab()
        {
            var path = Prefabs + "/PavilionFilter.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponent<PavilionFilterView>();
            if (existing != null) return existing;
            var root = Rect("PavilionFilter", null);
            var image = Image(root, Panel); var button = root.gameObject.AddComponent<Button>(); Style(button, image);
            var label = Text("Label", root, "Pavilion", 48, Light);
            Stretch((RectTransform)label.transform); ((RectTransform)label.transform).offsetMin = new Vector2(20, 12);
            ((RectTransform)label.transform).offsetMax = new Vector2(-20, -12);
            label.alignment = TextAlignmentOptions.Center;
            var view = root.gameObject.AddComponent<PavilionFilterView>(); view.Configure(button, label, image);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<PavilionFilterView>();
        }
        private static ExhibitCardView CreateCardPrefab()
        {
            var path = Prefabs + "/ExhibitCard.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponent<ExhibitCardView>();
            if (existing != null) return existing;
            var root = Rect("ExhibitCard", null); Image(root, Panel);
            ConstrainWidth(root);
            var layout = Vertical(root, 20); layout.padding = new RectOffset(32, 32, 32, 32);
            var id = Text("Identifier", root, "EXHIBIT", 32, Mint, 44);
            var title = Text("Title", root, "Exhibit title", 60, Light);
            Layout((RectTransform)title.transform).minHeight = 80;
            var summary = Text("Summary", root, "Description pending editorial review.", 48, Muted);
            Layout((RectTransform)summary.transform).minHeight = 112;
            var toggle = Button("Explore details  +", root, 48, 144);
            var body = Rect("Expanded details", root); Vertical(body, 24);
            var details = Text("Details", body, "Historical details are not yet available in this draft catalog.", 48, Light);
            var detailSizing = Layout((RectTransform)details.transform); detailSizing.minHeight = 160;
            var ar = Button("View in AR / coming later", body, 48, 144); ar.interactable = false;
            var view = root.gameObject.AddComponent<ExhibitCardView>();
            view.Configure(id, title, summary, details, toggle.GetComponentInChildren<TMP_Text>(), toggle, ar, body.gameObject);
            body.gameObject.SetActive(false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<ExhibitCardView>();
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            if (parent != null) result.SetParent(parent, false);
            if (parent != null && parent.GetComponent<HorizontalOrVerticalLayoutGroup>() != null)
                ConstrainWidth(result);
            result.localScale = Vector3.one; return result;
        }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static Image Image(RectTransform rect, Color color)
        { var image = rect.gameObject.AddComponent<Image>(); image.color = color; return image; }
        private static VerticalLayoutGroup Vertical(RectTransform rect, float spacing)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = spacing;
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = false; layout.childForceExpandWidth = true; return layout;
        }
        private static void Height(RectTransform rect, float height)
        { var sizing = Layout(rect); sizing.minHeight = height; sizing.preferredHeight = height; sizing.flexibleHeight = 0; }
        private static void Flexible(RectTransform rect)
        { var sizing = Layout(rect); sizing.flexibleHeight = 1; sizing.minHeight = sizing.preferredHeight = 0; }

        private static LayoutElement Layout(RectTransform rect)
        { return rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>(); }

        private static void ConstrainWidth(RectTransform rect)
        {
            var sizing = Layout(rect);
            sizing.minWidth = sizing.preferredWidth = 0;
            sizing.flexibleWidth = 1;
        }
        private static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color, float height = 0)
        {
            var rect = Rect(name, parent); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color;
            text.richText = false; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow; text.alignment = TextAlignmentOptions.MidlineLeft;
            if (height > 0) Height(rect, height);
            return text;
        }
        private static Button Button(string title, Transform parent, float fontSize, float height)
        {
            var root = Rect(title, parent); Height(root, height);
            var image = Image(root, new Color32(45, 67, 82, 255));
            var button = root.gameObject.AddComponent<Button>(); Style(button, image);
            var label = Text("Label", root, title, fontSize, Light); label.alignment = TextAlignmentOptions.Center;
            var labelRect = (RectTransform)label.transform; Stretch(labelRect);
            labelRect.offsetMin = new Vector2(20, 10); labelRect.offsetMax = new Vector2(-20, -10);
            return button;
        }
        private static void Style(Button button, Image image)
        {
            button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(0.85f, 1, 0.95f);
            colors.selectedColor = new Color(0.85f, 1, 0.95f); colors.pressedColor = new Color(0.65f, 0.85f, 0.78f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f); button.colors = colors;
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = path.Substring(0, path.LastIndexOf('/')); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
    }
}
