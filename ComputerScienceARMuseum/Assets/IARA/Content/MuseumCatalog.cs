using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Iara.Content
{
    [Serializable]
    public sealed class MuseumCatalog
    {
        public static readonly string[] HistoricalFields = { "yearLabel", "yearMeaning", "creators", "summary", "historicalContext", "howItWorks", "impact" };
        public int schemaVersion;
        public PavilionRecord[] categories;
        public ExhibitRecord[] exhibits;

        public static MuseumCatalog Parse(string json)
        {
            MuseumCatalog catalog;
            try { catalog = JsonUtility.FromJson<MuseumCatalog>(json); }
            catch (ArgumentException exception) { throw new FormatException("Invalid catalog JSON.", exception); }
            if (catalog == null || catalog.schemaVersion != 2 || catalog.categories == null || catalog.exhibits == null ||
                catalog.categories.Length != 3 || catalog.exhibits.Length != 8)
                throw new FormatException("The bundled museum catalog is unavailable or unsupported.");
            var categories = new HashSet<string>();
            foreach (var category in catalog.categories)
                if (category == null || string.IsNullOrWhiteSpace(category.categoryId) || string.IsNullOrWhiteSpace(category.title) ||
                    category.displayOrder < 1 || !categories.Add(category.categoryId))
                    throw new FormatException("Invalid pavilion identifiers.");
            if (!categories.SetEquals(new[] { "cryptography", "early-computing", "personal-computing" }) ||
                catalog.categories.Select(item => item.displayOrder).Distinct().Count() != 3)
                throw new FormatException("Invalid pavilion configuration.");
            var exhibits = new HashSet<string>();
            foreach (var exhibit in catalog.exhibits)
            {
                if (exhibit == null || string.IsNullOrWhiteSpace(exhibit.exhibitId) || string.IsNullOrWhiteSpace(exhibit.title) ||
                    !exhibits.Add(exhibit.exhibitId) || !categories.Contains(exhibit.categoryId))
                    throw new FormatException("Invalid exhibit identifiers or pavilion mapping.");
                var identity = string.Join("|", exhibit.categoryId, exhibit.markerId, exhibit.modelKey, exhibit.demoKey);
                // JsonUtility represents a JSON null string as an empty string. The source validator
                // enforces literal null; either representation means no demo in the runtime DTO.
                if (identity != ExpectedIdentity(exhibit.exhibitId))
                    throw new FormatException("Changed exhibit mapping: " + exhibit.exhibitId);
                ValidateHistoricalContent(exhibit);
            }
            return catalog;
        }

        private static string ExpectedIdentity(string id)
        {
            switch (id)
            {
                case "ENC-01": return "cryptography|marker_enigma|enigma|enigma_signal_path";
                case "ENC-02": return "cryptography|marker_bombe|bombe|bombe_drum_cycle";
                case "EAR-01": return "early-computing|marker_colossus|colossus|colossus_tape_flow";
                case "EAR-02": return "early-computing|marker_eniac|eniac|eniac_calculation_flow";
                case "PC-01": return "personal-computing|marker_apple_i|apple_i|";
                case "PC-02": return "personal-computing|marker_apple_ii|apple_ii|";
                case "PC-03": return "personal-computing|marker_ibm_5150|ibm_5150|";
                case "PC-04": return "personal-computing|marker_macintosh_128k|macintosh_128k|";
                default: return null;
            }
        }

        private static void ValidateHistoricalContent(ExhibitRecord item)
        {
            RequireText(item.yearLabel, 4);
            if (item.yearLabel.Length != 4 || !int.TryParse(item.yearLabel, out var year) || year < 1800 || year > DateTime.UtcNow.Year)
                throw new FormatException("Invalid exhibit year: " + item.exhibitId);
            RequireText(item.yearMeaning, 250);
            RequireText(item.summary, 240);
            RequireText(item.historicalContext, 600);
            RequireText(item.howItWorks, 500);
            RequireText(item.impact, 400);
            if (item.creators == null || item.creators.Length == 0) throw new FormatException("Creators missing: " + item.exhibitId);
            foreach (var creator in item.creators) RequireText(creator, 200);
            if (item.sources == null || item.sources.Length == 0) throw new FormatException("Sources missing: " + item.exhibitId);
            var ids = new HashSet<string>();
            var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var covered = new HashSet<string>();
            foreach (var source in item.sources)
            {
                if (source == null) throw new FormatException("Null source.");
                RequireText(source.sourceId, 64);
                RequireText(source.title, 200); RequireText(source.publisher, 150);
                RequireText(source.attribution, 300); RequireText(source.locator, 300);
                RequireText(source.url, 2000);
                if (!Regex.IsMatch(source.sourceId, "^" + Regex.Escape(item.exhibitId) + "-S[1-9][0-9]*$") || !ids.Add(source.sourceId) ||
                    !Uri.TryCreate(source.url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
                    uri.IsLoopback || !string.IsNullOrEmpty(uri.UserInfo) || !urls.Add(source.url))
                    throw new FormatException("Invalid or duplicate source: " + item.exhibitId);
                if (!new[] { "museum", "primary", "manufacturer", "academic-archive" }.Contains(source.sourceType) ||
                    !DateTime.TryParseExact(source.accessedOn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var accessed) ||
                    accessed.Date > DateTime.UtcNow.Date || source.supports == null || source.supports.Length == 0)
                    throw new FormatException("Invalid source metadata: " + source.sourceId);
                foreach (var field in source.supports)
                {
                    if (!HistoricalFields.Contains(field)) throw new FormatException("Unknown source field: " + source.sourceId);
                    covered.Add(field);
                }
            }
            if (!covered.SetEquals(HistoricalFields)) throw new FormatException("Incomplete source coverage: " + item.exhibitId);
            if (item.editorialNotes == null || item.editorialNotes.Length == 0) throw new FormatException("Editorial scope missing: " + item.exhibitId);
            foreach (var note in item.editorialNotes)
            {
                if (note == null || !new[] { "scope", "simplification", "uncertainty" }.Contains(note.kind)) throw new FormatException("Invalid editorial note.");
                RequireText(note.text, 400);
                if (note.sourceIds == null || note.sourceIds.Length == 0 || note.sourceIds.Any(id => !ids.Contains(id)))
                    throw new FormatException("Unknown note source: " + item.exhibitId);
            }
        }

        private static void RequireText(string value, int limit)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > limit) throw new FormatException("Missing or oversized catalog text.");
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
        public string markerId;
        public string modelKey;
        public string demoKey;
        public string yearLabel;
        public string yearMeaning;
        public string[] creators;
        public string summary;
        public string historicalContext;
        public string howItWorks;
        public string impact;
        public SourceRecord[] sources;
        public EditorialNote[] editorialNotes;
    }

    [Serializable]
    public sealed class SourceRecord
    {
        public string sourceId, title, publisher, url, sourceType, accessedOn, attribution, locator;
        public string[] supports;
    }

    [Serializable]
    public sealed class EditorialNote
    {
        public string kind, text;
        public string[] sourceIds;
    }
}
