using System;
using System.Collections;
using System.IO;
using System.Linq;
using Iara.Content;
using Iara.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Iara.Tests
{
    public sealed class CatalogContentTests
    {
        private const string AssetPath = "Assets/IARA/Content/Resources/museum_catalog.json";
        private static MuseumCatalog Catalog() => MuseumCatalog.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(AssetPath).text);

        [TestCase("ENC-01", "1918")]
        [TestCase("ENC-02", "1940")]
        [TestCase("EAR-01", "1944")]
        [TestCase("EAR-02", "1946")]
        [TestCase("PC-01", "1976")]
        [TestCase("PC-02", "1977")]
        [TestCase("PC-03", "1981")]
        [TestCase("PC-04", "1984")]
        public void EveryExhibitHasCompleteContentAndLocalSourceAssociations(string id, string year)
        {
            var item = Catalog().exhibits.Single(record => record.exhibitId == id);
            Assert.That(item.yearLabel, Is.EqualTo(year));
            Assert.That(item.creators, Is.Not.Empty);
            Assert.That(item.sources, Is.Not.Empty);
            if (id.StartsWith("PC-", StringComparison.Ordinal)) Assert.That(string.IsNullOrEmpty(item.demoKey), Is.True);
            CollectionAssert.AreEquivalent(MuseumCatalog.HistoricalFields, item.sources.SelectMany(source => source.supports).Distinct());
            foreach (var note in item.editorialNotes)
                Assert.That(note.sourceIds.All(key => item.sources.Any(source => source.sourceId == key)), Is.True);
        }

        [Test]
        public void BundledBytesAndExistingTextAssetGuidArePreserved()
        {
            var root = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../.."));
            CollectionAssert.AreEqual(File.ReadAllBytes(Path.Combine(root, "content/catalog/museum_catalog.json")),
                File.ReadAllBytes(Path.Combine(UnityEngine.Application.dataPath, "IARA/Content/Resources/museum_catalog.json")));
            Assert.That(AssetDatabase.AssetPathToGUID(AssetPath), Is.EqualTo("54be6c30bbd06c548adc0d41c9530d2d"));
        }

        [TestCase("missing-summary")]
        [TestCase("missing-creators")]
        [TestCase("missing-attribution")]
        [TestCase("bad-url")]
        [TestCase("duplicate-source")]
        [TestCase("unknown-note-source")]
        [TestCase("missing-field-association")]
        [TestCase("changed-mapping")]
        public void InvalidContentIsRejected(string defect)
        {
            var data = Catalog();
            var item = data.exhibits[0];
            switch (defect)
            {
                case "missing-summary": item.summary = ""; break;
                case "missing-creators": item.creators = Array.Empty<string>(); break;
                case "missing-attribution": item.sources[0].attribution = ""; break;
                case "bad-url": item.sources[0].url = "javascript:invalid"; break;
                case "duplicate-source": item.sources[1].sourceId = item.sources[0].sourceId; break;
                case "unknown-note-source": item.editorialNotes[0].sourceIds = new[] { "missing" }; break;
                case "missing-field-association":
                    foreach (var source in item.sources) source.supports = source.supports.Where(field => field != "impact").ToArray();
                    break;
                case "changed-mapping": item.markerId = "different-marker"; break;
            }
            Assert.Throws<FormatException>(() => MuseumCatalog.Parse(JsonUtility.ToJson(data)));
        }

        [UnityTest]
        public IEnumerator HistoricalContentBindsEveryCardInPlayMode()
        {
            EditorSceneManager.OpenScene("Assets/IARA/Scenes/Museum.unity");
            yield return new EnterPlayMode();
            yield return null;
            yield return null;
            var screen = UnityEngine.Object.FindFirstObjectByType<MuseumScreen>();
            foreach (var record in Catalog().exhibits)
            {
                var card = screen.Cards.Single(view => view.ExhibitId == record.exhibitId);
                card.ToggleButton.onClick.Invoke();
                yield return null;
                var labels = card.GetComponentsInChildren<TMP_Text>(true);
                Assert.That(labels.Single(label => label.name == "Title").text, Is.EqualTo(record.title));
                Assert.That(labels.Single(label => label.name == "Summary").text, Is.EqualTo(record.summary));
                StringAssert.Contains(record.yearLabel, labels.Single(label => label.name == "Identifier").text);
                var detail = labels.Single(label => label.name == "Details").text;
                foreach (var value in new[] { record.yearMeaning, record.historicalContext, record.howItWorks, record.impact }.Concat(record.creators))
                    StringAssert.Contains(value, detail);
                foreach (var source in record.sources)
                { StringAssert.Contains(source.title, detail); StringAssert.Contains(source.publisher, detail); }
                foreach (var note in record.editorialNotes) StringAssert.Contains(note.text, detail);
                StringAssert.Contains("Sources (stored offline)", detail);
                StringAssert.Contains("3D models, AR exploration and educational demonstrations are not available yet.", detail);
                Assert.That(card.ArActionEnabled, Is.False);
            }
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator LeavePlayMode()
        { if (UnityEngine.Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
