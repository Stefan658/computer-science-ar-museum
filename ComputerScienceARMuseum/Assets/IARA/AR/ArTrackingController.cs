using System;
using System.Collections.Generic;
using Iara.Application;
using Iara.Content;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Iara.AR
{
    public sealed class ArTrackingController : MonoBehaviour, IArStatusSource
    {
        [SerializeField] private TextAsset catalogAsset;
        [SerializeField] private ImageTrackingAssets assets;
        [SerializeField] private ARTrackedImageManager imageManager;
        [SerializeField] private Transform poseRoot;
        [SerializeField] private Transform userTransform;
        private readonly Dictionary<string, Pose> poses = new Dictionary<string, Pose>();
        private ImageTrackingState state;
        private GameObject visual;
        private bool paused;
        private string providerStatus = "";
        public string StatusMessage { get; private set; } = "Scanning / select an exhibit before starting AR.";
        public event Action StatusChanged;
        public ImageTrackingState State => state;
        public Transform UserTransform => userTransform;
        public GameObject Visualization => visual;
        public void Configure(TextAsset catalog, ImageTrackingAssets assetReferences, ARTrackedImageManager manager,
            Transform trackedPose, Transform userRoot)
        { catalogAsset = catalog; assets = assetReferences; imageManager = manager; poseRoot = trackedPose; userTransform = userRoot; }

        private void Awake()
        {
            try
            {
                var catalog = MuseumCatalog.Parse(catalogAsset.text);
                var map = new MarkerMap(catalog);
                if (assets == null || assets.ReferenceLibrary == null || assets.ReferenceLibrary.count != catalog.exhibits.Length || assets.DiagnosticPrefab == null)
                    throw new FormatException("Tracking assets are incomplete.");
                var names = new HashSet<string>();
                foreach (var image in assets.ReferenceLibrary)
                    if (!map.TryGetExhibit(image.name, out _) || !names.Add(image.name) || !image.specifySize || image.width <= 0)
                        throw new FormatException("Invalid reference image mapping.");
                state = new ImageTrackingState(map);
                visual = Instantiate(assets.DiagnosticPrefab, userTransform);
                visual.SetActive(false);
            }
            catch (Exception exception)
            {
                SetProviderStatus("AR content unavailable: " + exception.Message);
                enabled = false;
            }
        }
        private void OnEnable()
        {
            if (state == null) return;
            imageManager.trackablesChanged.AddListener(OnImagesChanged);
            ExhibitSelection.Changed += OnSelectionChanged;
            OnSelectionChanged();
        }
        private void OnDisable()
        {
            if (imageManager != null) imageManager.trackablesChanged.RemoveListener(OnImagesChanged);
            ExhibitSelection.Changed -= OnSelectionChanged;
            if (visual != null) visual.SetActive(false);
            state?.LoseAll(Time.realtimeSinceStartupAsDouble);
        }
        private void OnSelectionChanged()
        {
            var selection = ExhibitSelection.ExhibitId;
            if (state.ExhibitId != selection)
            {
                userTransform.localRotation = Quaternion.identity;
                userTransform.localScale = Vector3.one;
            }
            state.Select(selection, Time.realtimeSinceStartupAsDouble);
            Refresh();
        }
        private void OnImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> changes)
        {
            foreach (var image in changes.added) Observe(image);
            foreach (var image in changes.updated) Observe(image);
            foreach (var removed in changes.removed) RemoveObservation(removed.Key.ToString(), Time.realtimeSinceStartupAsDouble);
            Refresh();
        }
        private void Observe(ARTrackedImage image) => ApplyObservation(image.trackableId.ToString(), image.referenceImage.name,
            image.trackingState == TrackingState.Tracking && ARSession.state == ARSessionState.SessionTracking && !paused,
            new Pose(image.transform.position, image.transform.rotation), Time.realtimeSinceStartupAsDouble);

        // Provider-independent observation seam used by deterministic tests, not a mock camera feed.
        public void ApplyObservation(string trackableId, string markerId, bool tracking, Pose pose, double now)
        {
            if (state == null) return;
            if (tracking) poses[trackableId] = pose;
            state.Observe(trackableId, markerId, tracking, now);
            Refresh();
        }
        public void RemoveObservation(string trackableId, double now)
        { if (state == null) return; poses.Remove(trackableId); state.Remove(trackableId, now); Refresh(); }
        public void Advance(double now) { state?.Tick(now); Refresh(); }
        public void SetProviderStatus(string message) { providerStatus = message; Refresh(); }
        private void Update()
        {
            if (state == null) return;
            if (!UnityEngine.Application.isEditor && (paused || ARSession.state != ARSessionState.SessionTracking))
                state.LoseAll(Time.realtimeSinceStartupAsDouble);
            Advance(Time.realtimeSinceStartupAsDouble);
        }
        private void OnApplicationPause(bool value)
        { paused = value; if (value) state?.LoseAll(Time.realtimeSinceStartupAsDouble); Refresh(); }
        private void Refresh()
        {
            if (state != null && visual != null)
            {
                if (state.Visible && state.Status == ImageStatus.Detected && state.ActiveTrackableId != null && poses.TryGetValue(state.ActiveTrackableId, out var pose))
                    poseRoot.SetPositionAndRotation(pose.position, pose.rotation);
                visual.SetActive(isActiveAndEnabled && state.Visible);
            }
            var message = state == null || string.IsNullOrEmpty(state.ExhibitId) ? "Scanning / select an exhibit in application state."
                : state.Status == ImageStatus.Detected ? "Detected / " + state.ExhibitId + " / diagnostic visual only"
                : state.Status == ImageStatus.WrongMarker ? "Wrong marker / scan the marker for " + state.ExhibitId
                : state.Status == ImageStatus.TrackingLost ? "Tracking lost / reacquire " + state.ExhibitId
                : "Scanning / " + state.ExhibitId;
            if (!string.IsNullOrEmpty(providerStatus)) message = providerStatus + "\n" + message;
            if (message == StatusMessage) return;
            StatusMessage = message;
            StatusChanged?.Invoke();
        }
    }
}
