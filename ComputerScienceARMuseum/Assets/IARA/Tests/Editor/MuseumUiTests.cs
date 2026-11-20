using System;
using System.Collections;
using System.Linq;
using Iara.Application;
using Iara.Content;
using Iara.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Iara.Tests
{
    public sealed class MuseumUiTests
    {
        private static MuseumCatalog Catalog() => MuseumCatalog.Parse(
            AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/IARA/Content/Resources/museum_catalog.json").text);

        [Test]
        public void FiltersMatchCatalogAndPreserveExpandedIdentity()
        {
            var state = new MuseumState(Catalog());
            Assert.That(state.VisibleExhibits.Count(), Is.EqualTo(8));
            state.SelectCategory("cryptography");
            CollectionAssert.AreEquivalent(new[] { "ENC-01", "ENC-02" }, state.VisibleExhibits.Select(item => item.exhibitId));
            state.ToggleExhibit("ENC-01");
            state.SelectCategory("early-computing");
            CollectionAssert.AreEquivalent(new[] { "EAR-01", "EAR-02" }, state.VisibleExhibits.Select(item => item.exhibitId));
            state.SelectCategory("personal-computing");
            CollectionAssert.AreEquivalent(new[] { "PC-01", "PC-02", "PC-03", "PC-04" }, state.VisibleExhibits.Select(item => item.exhibitId));
            state.SelectCategory("");
            Assert.That(state.IsExpanded("ENC-01"), Is.True);
            state.ToggleExhibit("ENC-01");
            Assert.That(state.IsExpanded("ENC-01"), Is.False);
            Assert.Throws<ArgumentException>(() => state.SelectCategory("unknown"));
            Assert.Throws<ArgumentException>(() => state.ToggleExhibit("unknown"));
        }

        [Test]
        public void InvalidCatalogIsRejected()
        {
            Assert.Throws<FormatException>(() => MuseumCatalog.Parse("{}"));
            Assert.Throws<FormatException>(() => MuseumCatalog.Parse(
                "{\"schemaVersion\":1,\"categories\":[{\"categoryId\":\"x\"}],\"exhibits\":[{\"exhibitId\":\"a\",\"title\":\"Title\",\"categoryId\":\"missing\"}]}"));
        }

        [UnityTest]
        public IEnumerator MuseumInteractionsInPlayMode()
        {
            EditorSceneManager.OpenScene("Assets/IARA/Scenes/Museum.unity");
            yield return new EnterPlayMode();
            yield return null;
            yield return null;
            var screen = UnityEngine.Object.FindFirstObjectByType<MuseumScreen>();
            Assert.That(screen, Is.Not.Null);
            Assert.That(screen.Cards.Count, Is.EqualTo(8));
            Assert.That(screen.Filters.Count, Is.EqualTo(4));
            var scaler = screen.GetComponent<CanvasScaler>();
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080, 2400)));
            // Test equivalent width-scaled layout geometry without depending on the headless Game View size.
            // This does not test GPU rendering or actual screen/touch input.
            var canvas = screen.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            foreach (var size in new[] { new Vector2(1080, 2400), new Vector2(720, 1600), new Vector2(480, 800) })
            {
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(1080, 1080 * size.y / size.x);
                yield return null;
                Canvas.ForceUpdateCanvases();
                Assert.That(screen.ExhibitScroll.viewport.rect.height, Is.GreaterThan(200), "Usable viewport at " + size);
                foreach (var label in screen.GetComponentsInChildren<TMP_Text>())
                {
                    var rect = label.rectTransform.rect;
                    var needed = label.GetPreferredValues(label.text, rect.width, float.PositiveInfinity).y;
                    Assert.That(needed, Is.LessThanOrEqualTo(rect.height + 3), "Text overflow: " + label.name + " at " + size);
                }
            }
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(1080, 2400);
            yield return null;
            foreach (var category in new[] { "cryptography", "early-computing", "personal-computing", "" })
            {
                screen.Filters[category].Button.onClick.Invoke();
                yield return null;
                var expected = Catalog().exhibits.Where(item => category == "" || item.categoryId == category).Select(item => item.exhibitId);
                CollectionAssert.AreEquivalent(expected, screen.Cards.Where(item => item.gameObject.activeSelf).Select(item => item.ExhibitId));
            }
            var card = screen.Cards.First(item => item.ExhibitId == "ENC-01");
            Canvas.ForceUpdateCanvases();
            var oldHeight = ((RectTransform)card.transform).rect.height;
            card.ToggleButton.onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(card.Expanded, Is.True);
            Assert.That(((RectTransform)card.transform).rect.height, Is.GreaterThan(oldHeight));
            Assert.That(card.ArActionEnabled, Is.False);
            screen.Filters["personal-computing"].Button.onClick.Invoke();
            screen.Filters[""].Button.onClick.Invoke();
            Assert.That(card.Expanded, Is.True);
            card.ToggleButton.onClick.Invoke();
            Assert.That(card.Expanded, Is.False);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(screen.ExhibitScroll.content.rect.height, Is.GreaterThan(screen.ExhibitScroll.viewport.rect.height));
            var initialPosition = screen.ExhibitScroll.content.anchoredPosition.y;
            screen.ExhibitScroll.verticalNormalizedPosition = 0;
            yield return null;
            Assert.That(screen.ExhibitScroll.content.anchoredPosition.y, Is.GreaterThan(initialPosition));
            screen.AboutButton.onClick.Invoke();
            Assert.That(screen.AboutVisible, Is.True);
            screen.CloseAboutButton.onClick.Invoke();
            Assert.That(screen.AboutVisible, Is.False);
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator LeavePlayMode()
        {
            if (UnityEngine.Application.isPlaying) yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator PortraitLayoutStaysInsideSafeArea()
        {
            EditorSceneManager.OpenScene("Assets/IARA/Scenes/Museum.unity");
            yield return new EnterPlayMode();
            yield return null;
            var screen = UnityEngine.Object.FindFirstObjectByType<MuseumScreen>();
            var canvas = screen.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var safeArea = screen.GetComponentInChildren<SafeAreaPanel>();
            safeArea.enabled = false; // Inject geometry only; these are not measured device inset values.
            var safe = (RectTransform)safeArea.transform;
            var page = (RectTransform)safe.Find("Page");
            var navigation = (RectTransform)page.Find("Navigation");
            foreach (var size in new[] { new Vector2(1440, 3088), new Vector2(1080, 2400), new Vector2(480, 800) })
            {
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(1080, 1080 * size.y / size.x);
                foreach (var inset in new[] { 0f, 0.035f })
                {
                    safe.anchorMin = new Vector2(inset, inset);
                    safe.anchorMax = new Vector2(1 - inset, 1 - inset);
                    safe.offsetMin = safe.offsetMax = Vector2.zero;
                    foreach (var category in new[] { "cryptography", "early-computing", "personal-computing", "" })
                    {
                        screen.Filters[category].Button.onClick.Invoke();
                        foreach (var card in screen.Cards.Where(item => item.gameObject.activeSelf && !item.Expanded))
                            card.ToggleButton.onClick.Invoke();
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        LayoutRebuilder.ForceRebuildLayoutImmediate(page);
                        Canvas.ForceUpdateCanvases();
                        var context = size + ", inset=" + inset + ", category=" + category;
                        AssertHorizontalBounds(safe, screen.ExhibitScroll.viewport, context);
                        AssertHorizontalBounds(safe, navigation, context);
                        foreach (var filter in screen.Filters.Values)
                            AssertHorizontalBounds(safe, (RectTransform)filter.transform, context);
                        foreach (var card in screen.Cards.Where(item => item.gameObject.activeSelf))
                        {
                            AssertHorizontalBounds(screen.ExhibitScroll.viewport, (RectTransform)card.transform, context);
                            foreach (var label in card.GetComponentsInChildren<TMP_Text>())
                            {
                                AssertHorizontalBounds(screen.ExhibitScroll.viewport, label.rectTransform, context);
                                var needed = label.GetPreferredValues(label.text, label.rectTransform.rect.width, float.PositiveInfinity).y;
                                Assert.That(needed, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 3), label.name + ": " + context);
                            }
                        }
                        Assert.That(navigation.rect.height, Is.EqualTo(144).Within(1), "Fixed navigation: " + context);
                        foreach (var button in navigation.GetComponentsInChildren<Button>())
                        {
                            Assert.That(((RectTransform)button.transform).rect.height, Is.EqualTo(144).Within(1), "Fixed button height: " + context);
                            AssertHorizontalBounds(safe, (RectTransform)button.transform, context);
                        }
                        var header = (RectTransform)page.Find("Header");
                        AssertHorizontalBounds(safe, header, context);
                        Assert.That(BoundsIn(safe, header).yMax, Is.LessThanOrEqualTo(safe.rect.yMax + 1), "Header safe-area containment: " + context);
                        Assert.That(header.rect.height, Is.EqualTo(216).Within(1), "Preserved header height: " + context);
                        var viewportBounds = BoundsIn(safe, screen.ExhibitScroll.viewport);
                        var navigationBounds = BoundsIn(safe, navigation);
                        Assert.That(viewportBounds.height, Is.GreaterThan(200), "Remaining scroll space: " + context);
                        Assert.That(viewportBounds.yMin, Is.GreaterThanOrEqualTo(navigationBounds.yMax), "Scroll/footer overlap: " + context);
                        Assert.That(screen.ExhibitScroll.content.rect.height, Is.GreaterThan(screen.ExhibitScroll.viewport.rect.height));
                    }
                }
            }
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        private static void AssertHorizontalBounds(RectTransform reference, RectTransform child, string context)
        {
            var bounds = BoundsIn(reference, child);
            Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(reference.rect.xMin - 1), child.name + " left: " + context);
            Assert.That(bounds.xMax, Is.LessThanOrEqualTo(reference.rect.xMax + 1), child.name + " right: " + context);
        }

        private static Rect BoundsIn(RectTransform reference, RectTransform child)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            var local = corners.Select(point => reference.InverseTransformPoint(point)).ToArray();
            return Rect.MinMaxRect(local.Min(point => point.x), local.Min(point => point.y),
                local.Max(point => point.x), local.Max(point => point.y));
        }
    }
}
