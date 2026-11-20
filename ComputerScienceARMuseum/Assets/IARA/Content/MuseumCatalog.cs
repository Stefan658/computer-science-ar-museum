using System;
using System.Collections.Generic;
using UnityEngine;

namespace Iara.Content
{
    [Serializable]
    public sealed class MuseumCatalog
    {
        public int schemaVersion;
        public PavilionRecord[] categories;
        public ExhibitRecord[] exhibits;

        public static MuseumCatalog Parse(string json)
        {
            var catalog = JsonUtility.FromJson<MuseumCatalog>(json);
            if (catalog == null || catalog.schemaVersion != 1 || catalog.categories == null || catalog.exhibits == null)
                throw new FormatException("The bundled museum catalog is unavailable or unsupported.");
            var categories = new HashSet<string>();
            foreach (var category in catalog.categories)
                if (category == null || string.IsNullOrWhiteSpace(category.categoryId) || !categories.Add(category.categoryId))
                    throw new FormatException("Invalid pavilion identifiers.");
            var exhibits = new HashSet<string>();
            foreach (var exhibit in catalog.exhibits)
                if (exhibit == null || string.IsNullOrWhiteSpace(exhibit.exhibitId) || string.IsNullOrWhiteSpace(exhibit.title) ||
                    !exhibits.Add(exhibit.exhibitId) || !categories.Contains(exhibit.categoryId))
                    throw new FormatException("Invalid exhibit identifiers or pavilion mapping.");
            return catalog;
        }
    }

    [Serializable]
    public sealed class PavilionRecord
    {
        public string categoryId;
        public string title;
        public int displayOrder;
    }

    [Serializable]
    public sealed class ExhibitRecord
    {
        public string exhibitId;
        public string categoryId;
        public string title;
        public string summary;
        public string historicalContext;
        public string howItWorks;
        public string impact;
    }
}
