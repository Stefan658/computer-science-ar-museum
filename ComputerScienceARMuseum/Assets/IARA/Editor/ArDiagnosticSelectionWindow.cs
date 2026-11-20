using Iara.Application;
using Iara.Content;
using UnityEditor;
using UnityEngine;

namespace Iara.Editor
{
    public sealed class ArDiagnosticSelectionWindow : EditorWindow
    {
        [MenuItem("IARA/AR/Diagnostic selection (Play Mode)")]
        private static void Open() => GetWindow<ArDiagnosticSelectionWindow>("IARA AR diagnostics");
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Application selection only. No simulated detection or physical tracking is provided by this window. Museum navigation remains unchanged.", MessageType.Info);
            if (!EditorApplication.isPlaying) { EditorGUILayout.LabelField("Open ARExperience and enter Play Mode first."); return; }
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/IARA/Content/Resources/museum_catalog.json");
            if (asset == null) return;
            var catalog = MuseumCatalog.Parse(asset.text);
            foreach (var item in catalog.exhibits)
                if (GUILayout.Button(item.exhibitId + " / " + item.title)) ExhibitSelection.Select(catalog, item.exhibitId);
            if (GUILayout.Button("Clear selection")) ExhibitSelection.Clear();
        }
    }
}
