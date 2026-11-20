using System;
using System.Collections;
using System.IO;
using System.Linq;
using Iara.Application;
using Iara.AR;
using Iara.Content;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.TestTools;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

namespace Iara.Tests
{
    public sealed class ImageTrackingTests
    {
        private static MuseumCatalog Catalog() => MuseumCatalog.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/IARA/Content/Resources/museum_catalog.json").text);
        private static ImageTrackingState State(string selection = "ENC-01")
        { var state = new ImageTrackingState(new MarkerMap(Catalog())); state.Select(selection, 0); return state; }

        [TestCase("ENC-01", "marker_enigma")]
        [TestCase("ENC-02", "marker_bombe")]
        [TestCase("EAR-01", "marker_colossus")]
        [TestCase("EAR-02", "marker_eniac")]
        [TestCase("PC-01", "marker_apple_i")]
        [TestCase("PC-02", "marker_apple_ii")]
        [TestCase("PC-03", "marker_ibm_5150")]
        [TestCase("PC-04", "marker_macintosh_128k")]
        public void EveryMarkerMapsOnlyToItsSelectedExhibit(string exhibit, string marker)
        {
            var map = new MarkerMap(Catalog());
            Assert.That(map.MarkerFor(exhibit), Is.EqualTo(marker));
            Assert.That(map.TryGetExhibit(marker, out var mapped), Is.True);
            Assert.That(mapped, Is.EqualTo(exhibit));
            foreach (var item in Catalog().exhibits)
            {
                var state = State(item.exhibitId);
                state.Observe("trackable", marker, true, 1);
                Assert.That(state.Visible, Is.EqualTo(item.exhibitId == exhibit));
                Assert.That(state.Status, Is.EqualTo(item.exhibitId == exhibit ? ImageStatus.Detected : ImageStatus.WrongMarker));
            }
        }
        [Test]
        public void LibraryHasEightUniqueImagesAtRecordedPhysicalSize()
        {
            var assets = AssetDatabase.LoadAssetAtPath<ImageTrackingAssets>("Assets/IARA/AR/Assets/ImageTrackingAssets.asset");
            Assert.That(assets, Is.Not.Null); Assert.That(assets.DiagnosticPrefab, Is.Not.Null);
            var images = assets.ReferenceLibrary.ToArray();
            CollectionAssert.AreEquivalent(Catalog().exhibits.Select(item => item.markerId), images.Select(image => image.name));
            Assert.That(images.Select(image => image.guid).Distinct().Count(), Is.EqualTo(8));
            Assert.That(images.Select(image => image.textureGuid).Distinct().Count(), Is.EqualTo(8));
            var hashes = images.Select(image => Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(AssetDatabase.GetAssetPath(image.texture))))).ToArray();
            Assert.That(hashes.Distinct().Count(), Is.EqualTo(8));
            foreach (var image in images)
            {
                Assert.That(image.texture, Is.Not.Null);
                Assert.That(image.texture.width, Is.EqualTo(512));
                Assert.That(image.texture.height, Is.EqualTo(512));
                Assert.That(image.specifySize, Is.True);
                Assert.That(image.size, Is.EqualTo(new Vector2(0.15f, 0.15f)));
            }
        }
        [Test]
        public void SelectionContractRejectsUnknownIdsAndCanBeCleared()
        {
            ExhibitSelection.Clear();
            Assert.Throws<ArgumentException>(() => ExhibitSelection.Select(Catalog(), "unknown"));
            foreach (var exhibit in Catalog().exhibits)
            { ExhibitSelection.Select(Catalog(), exhibit.exhibitId); Assert.That(ExhibitSelection.ExhibitId, Is.EqualTo(exhibit.exhibitId)); }
            ExhibitSelection.Clear(); Assert.That(ExhibitSelection.ExhibitId, Is.Null);
        }
        [Test]
        public void NoSelectionOrUnknownMarkerNeverActivatesVisualization()
        {
            var state = State(null);
            state.Observe("a", "marker_enigma", true, 1);
            Assert.That(state.Visible, Is.False);
            state.Remove("a", 2); state.Select("ENC-01", 2);
            state.Observe("unknown", "unregistered-marker", true, 3);
            Assert.That(state.Visible, Is.False); Assert.That(state.Status, Is.EqualTo(ImageStatus.WrongMarker));
            state.Remove("unknown", 4); Assert.That(state.Status, Is.EqualTo(ImageStatus.Scanning));
        }
        [Test]
        public void SustainedLossHidesAt200msAndRepeatedLossDoesNotRestartTimer()
        {
            var state = State(); state.Observe("a", "marker_enigma", true, 1);
            state.Observe("a", "marker_enigma", false, 2);
            Assert.That(state.Status, Is.EqualTo(ImageStatus.TrackingLost)); Assert.That(state.Visible, Is.True);
            state.Observe("a", "marker_enigma", false, 2.1);
            state.Tick(2.199); Assert.That(state.Visible, Is.True);
            state.Tick(2.2); Assert.That(state.Visible, Is.False); Assert.That(state.ActiveTrackableId, Is.Null);
        }
        [Test]
        public void ReacquisitionCancelsPendingHideAndRemovalStartsNewDebounce()
        {
            var state = State(); state.Observe("a", "marker_enigma", true, 1);
            state.Observe("a", "marker_enigma", false, 2);
            state.Observe("a", "marker_enigma", true, 2.199);
            state.Tick(2.4); Assert.That(state.Visible, Is.True);
            state.Remove("a", 3); state.Tick(3.199); Assert.That(state.Visible, Is.True);
            state.Tick(3.2); Assert.That(state.Visible, Is.False);
            state.Observe("b", "marker_enigma", true, 4); Assert.That(state.Visible, Is.True);
        }
        [Test]
        public void MultipleMarkersAndDuplicatePrintsStillHaveOneStableActiveTrackable()
        {
            var state = State(); state.Observe("z", "marker_enigma", true, 1);
            state.Observe("a", "marker_enigma", true, 2);
            state.Observe("b", "marker_bombe", true, 3);
            Assert.That(state.ActiveTrackableId, Is.EqualTo("z")); Assert.That(state.Status, Is.EqualTo(ImageStatus.Detected));
            state.Remove("z", 4); Assert.That(state.ActiveTrackableId, Is.EqualTo("a"));
            state.Select("PC-01", 5); Assert.That(state.Visible, Is.False);
            state.Select(null, 6); Assert.That(state.Visible, Is.False); Assert.That(state.Status, Is.EqualTo(ImageStatus.Scanning));
        }
        [Test]
        public void SessionLossExpiresWithoutAnyFurtherImageEvents()
        {
            var state = State(); state.Observe("a", "marker_enigma", true, 1);
            state.LoseAll(2); state.LoseAll(2.1); state.Tick(2.2);
            Assert.That(state.Visible, Is.False);
            state.Observe("a", "marker_enigma", true, 3); Assert.That(state.Visible, Is.True);
        }
        [Test]
        public void SceneHasSerializedCameraTrackingAndNoPerImageContentPrefab()
        {
            EditorSceneManager.OpenScene("Assets/IARA/Scenes/ARExperience.unity");
            var origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            Assert.That(origin, Is.Not.Null); Assert.That(origin.Camera, Is.Not.Null);
            Assert.That(origin.Camera.GetComponent<TrackedPoseDriver>(), Is.Not.Null);
            Assert.That(origin.Camera.GetComponent<ARCameraManager>().enabled, Is.False);
            Assert.That(origin.Camera.GetComponent<ARCameraBackground>().enabled, Is.False);
            var manager = origin.GetComponent<ARTrackedImageManager>();
            Assert.That(manager.enabled, Is.False); Assert.That(manager.referenceLibrary.count, Is.EqualTo(8));
            Assert.That(manager.trackedImagePrefab, Is.Null);
            var session = UnityEngine.Object.FindFirstObjectByType<ARSession>();
            Assert.That(session.enabled, Is.False); Assert.That(session.attemptUpdate, Is.False);
            Assert.That(session.GetComponent<ARInputManager>().enabled, Is.False);
            Assert.That(UnityEngine.Object.FindObjectsByType<ArTrackingController>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator SceneBindsSelectedDiagnosticPoseAndPreservesUserTransformOnLoss()
        {
            EditorSceneManager.OpenScene("Assets/IARA/Scenes/ARExperience.unity");
            yield return new EnterPlayMode(); yield return null; yield return null;
            var controller = UnityEngine.Object.FindFirstObjectByType<ArTrackingController>();
            Assert.That(controller.State, Is.Not.Null); Assert.That(controller.Visualization.activeSelf, Is.False);
            var label = UnityEngine.Object.FindFirstObjectByType<TMP_Text>();
            var now = Time.realtimeSinceStartupAsDouble;
            foreach (var record in Catalog().exhibits)
            {
                ExhibitSelection.Select(Catalog(), record.exhibitId);
                var wrong = Catalog().exhibits.First(item => item.exhibitId != record.exhibitId);
                controller.ApplyObservation("wrong", wrong.markerId, true, Pose.identity, now);
                StringAssert.Contains("Wrong marker", label.text);
                Assert.That(controller.Visualization.activeSelf, Is.False);
                controller.RemoveObservation("wrong", now);
                var pose = new Pose(new Vector3(1, 2, 3), Quaternion.Euler(0, 30, 0));
                controller.ApplyObservation("selected", record.markerId, true, pose, now);
                Assert.That(controller.Visualization.activeSelf, Is.True);
                StringAssert.Contains("Detected / " + record.exhibitId, label.text);
                var poseRoot = controller.UserTransform.parent;
                Assert.That(poseRoot.position, Is.EqualTo(pose.position));
                Assert.That(Quaternion.Angle(poseRoot.rotation, pose.rotation), Is.LessThan(0.01f));
                var rotation = Quaternion.Euler(0, 47, 0); var scale = Vector3.one * 1.7f;
                controller.UserTransform.localRotation = rotation; controller.UserTransform.localScale = scale;
                controller.ApplyObservation("selected", record.markerId, false, Pose.identity, now);
                StringAssert.Contains("Tracking lost", label.text);
                controller.Advance(now + 0.199); Assert.That(controller.Visualization.activeSelf, Is.True);
                controller.Advance(now + 0.2); Assert.That(controller.Visualization.activeSelf, Is.False);
                controller.ApplyObservation("selected", record.markerId, true, new Pose(Vector3.one, Quaternion.identity), now + 0.3);
                Assert.That(controller.UserTransform.localScale, Is.EqualTo(scale));
                Assert.That(Quaternion.Angle(controller.UserTransform.localRotation, rotation), Is.LessThan(0.01f));
                Assert.That(poseRoot.position, Is.EqualTo(Vector3.one));
                controller.RemoveObservation("selected", now + 0.4); controller.Advance(now + 1);
                now += 2;
            }
            ExhibitSelection.Clear(); Assert.That(controller.Visualization.activeSelf, Is.False);
            Assert.That(controller.GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }
        [UnityTearDown]
        public IEnumerator CleanupPlayMode()
        { if (UnityEngine.Application.isPlaying) { ExhibitSelection.Clear(); yield return new ExitPlayMode(); } }
    }
}
