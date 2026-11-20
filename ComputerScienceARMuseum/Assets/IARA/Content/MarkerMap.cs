using System;
using System.Collections.Generic;

namespace Iara.Content
{
    // Metadata lookup only: reference-image names must be the existing catalog marker IDs.
    public sealed class MarkerMap
    {
        private readonly Dictionary<string, string> exhibits = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> markers = new Dictionary<string, string>(StringComparer.Ordinal);
        public MarkerMap(MuseumCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            foreach (var item in catalog.exhibits)
            {
                if (string.IsNullOrWhiteSpace(item.markerId) || exhibits.ContainsKey(item.markerId) || markers.ContainsKey(item.exhibitId))
                    throw new FormatException("Duplicate or missing marker mapping.");
                exhibits.Add(item.markerId, item.exhibitId);
                markers.Add(item.exhibitId, item.markerId);
            }
        }
        public bool TryGetExhibit(string markerId, out string exhibitId) => exhibits.TryGetValue(markerId ?? "", out exhibitId);
        public string MarkerFor(string exhibitId) => markers.TryGetValue(exhibitId ?? "", out var marker)
            ? marker : throw new ArgumentException("Unknown exhibit selection.", nameof(exhibitId));
    }
}
