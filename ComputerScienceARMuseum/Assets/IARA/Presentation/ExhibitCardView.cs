using System;
using System.Collections.Generic;
using System.Linq;
using Iara.Content;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Iara.Presentation
{
    public sealed class ExhibitCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text identifier;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text summary;
        [SerializeField] private TMP_Text details;
        [SerializeField] private TMP_Text toggleLabel;
        [SerializeField] private Button toggle;
        [SerializeField] private Button viewInAr;
        [SerializeField] private GameObject expandedBody;
        public string ExhibitId { get; private set; }
        public Button ToggleButton => toggle;
        public bool Expanded => expandedBody.activeSelf;
        public bool ArActionEnabled => viewInAr.interactable;

        public void Configure(TMP_Text id, TMP_Text heading, TMP_Text shortText, TMP_Text detailText,
            TMP_Text actionText, Button expand, Button ar, GameObject body)
        { identifier = id; title = heading; summary = shortText; details = detailText; toggleLabel = actionText;
            toggle = expand; viewInAr = ar; expandedBody = body; }

        public void Bind(ExhibitRecord exhibit, bool expanded, Action onToggle)
        {
            ExhibitId = exhibit.exhibitId;
            identifier.text = exhibit.exhibitId + " / " + exhibit.yearLabel;
            title.text = exhibit.title;
            summary.text = string.IsNullOrWhiteSpace(exhibit.summary) ? "Description pending editorial review." : exhibit.summary;
            var sections = new List<string>();
            AddSection(sections, "Date" + Citations(exhibit, "yearLabel", "yearMeaning"), exhibit.yearLabel + " - " + exhibit.yearMeaning);
            AddSection(sections, "Creators / contributors" + Citations(exhibit, "creators"), string.Join("\n", exhibit.creators));
            AddSection(sections, "Historical context" + Citations(exhibit, "historicalContext"), exhibit.historicalContext);
            AddSection(sections, "How it works" + Citations(exhibit, "howItWorks"), exhibit.howItWorks);
            AddSection(sections, "Impact" + Citations(exhibit, "impact"), exhibit.impact);
            foreach (var note in exhibit.editorialNotes)
                AddSection(sections, note.kind == "simplification" ? "Educational simplification" : note.kind == "uncertainty" ? "Uncertainty" : "Scope note", note.text);
            var references = exhibit.sources.Select((source, index) => "[" + (index + 1) + "] " + source.publisher + ": " + source.title);
            AddSection(sections, "Sources (stored offline)", string.Join("\n", references));
            AddSection(sections, "Availability", "3D models, AR exploration and educational demonstrations are not available yet.");
            details.text = sections.Count == 0 ? "Historical details are not yet available in this draft catalog." : string.Join("\n\n", sections);
            expandedBody.SetActive(expanded);
            toggleLabel.text = expanded ? "Close details  -" : "Explore details  +";
            viewInAr.interactable = false;
            toggle.onClick.RemoveAllListeners();
            toggle.onClick.AddListener(() => onToggle());
        }
        private static void AddSection(List<string> sections, string heading, string text)
        { if (!string.IsNullOrWhiteSpace(text)) sections.Add(heading + "\n" + text); }

        private static string Citations(ExhibitRecord exhibit, params string[] fields)
        {
            var references = exhibit.sources.Select((source, index) => new { source, index })
                .Where(item => item.source.supports.Any(field => fields.Contains(field)))
                .Select(item => "[" + (item.index + 1) + "]");
            return " " + string.Join(" ", references);
        }
    }
}
