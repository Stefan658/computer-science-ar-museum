using System;
using System.Collections.Generic;
using System.Linq;
using Iara.Content;

namespace Iara.Application
{
    public enum ImageStatus { Scanning, Detected, TrackingLost, WrongMarker }

    // Pure policy, independent of provider, GameObjects, UI and wall-clock time.
    public sealed class ImageTrackingState
    {
        private sealed class Observation { public string Marker; public bool Tracking; }
        private readonly MarkerMap map;
        private readonly Dictionary<string, Observation> observations = new Dictionary<string, Observation>(StringComparer.Ordinal);
        private readonly double debounce;
        private double? lossAt;
        private bool detected;
        public string ExhibitId { get; private set; }
        public string ActiveTrackableId { get; private set; }
        public bool Visible { get; private set; }
        public ImageStatus Status { get; private set; }
        public ImageTrackingState(MarkerMap markerMap, double lossDebounceSeconds = 0.2)
        {
            map = markerMap ?? throw new ArgumentNullException(nameof(markerMap));
            if (double.IsNaN(lossDebounceSeconds) || double.IsInfinity(lossDebounceSeconds) || lossDebounceSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(lossDebounceSeconds));
            debounce = lossDebounceSeconds;
        }
        public void Select(string exhibitId, double now)
        {
            if (exhibitId != null) map.MarkerFor(exhibitId);
            ExhibitId = exhibitId; Visible = false; ActiveTrackableId = null; lossAt = null; detected = false;
            Tick(now);
        }
        public void Observe(string trackableId, string markerId, bool tracking, double now)
        {
            if (string.IsNullOrWhiteSpace(trackableId)) throw new ArgumentException("Trackable ID required.");
            observations[trackableId] = new Observation { Marker = markerId, Tracking = tracking };
            Tick(now);
        }
        public void Remove(string trackableId, double now) { observations.Remove(trackableId); Tick(now); }
        public void LoseAll(double now)
        {
            foreach (var observation in observations.Values) observation.Tracking = false;
            Tick(now);
        }
        public void Tick(double now)
        {
            if (double.IsNaN(now) || double.IsInfinity(now)) throw new ArgumentOutOfRangeException(nameof(now));
            if (ExhibitId == null) { Visible = false; ActiveTrackableId = null; Status = ImageStatus.Scanning; return; }
            var marker = map.MarkerFor(ExhibitId);
            var matching = observations.Where(pair => pair.Value.Tracking && pair.Value.Marker == marker)
                .Select(pair => pair.Key).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            if (matching.Length > 0)
            {
                if (!matching.Contains(ActiveTrackableId)) ActiveTrackableId = matching[0];
                Visible = true; detected = true; lossAt = null; Status = ImageStatus.Detected;
                return;
            }
            if (Visible)
            {
                if (!lossAt.HasValue) lossAt = now;
                if (now >= lossAt.Value + debounce) { Visible = false; ActiveTrackableId = null; }
            }
            Status = observations.Values.Any(value => value.Tracking && value.Marker != marker)
                ? ImageStatus.WrongMarker : detected ? ImageStatus.TrackingLost : ImageStatus.Scanning;
        }
    }
}
