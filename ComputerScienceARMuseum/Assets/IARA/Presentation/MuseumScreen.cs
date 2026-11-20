using System;
using System.Collections.Generic;
using System.Linq;
using Iara.Application;
using Iara.Content;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Iara.Presentation
{
    public sealed class MuseumScreen : MonoBehaviour
    {
        [SerializeField] private TextAsset catalogAsset;
        [SerializeField] private PavilionFilterView filterPrefab;
        [SerializeField] private ExhibitCardView cardPrefab;
        [SerializeField] private Transform filterContainer;
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button catalogButton;
        [SerializeField] private Button topButton;
        [SerializeField] private Button aboutButton;
        [SerializeField] private Button closeAboutButton;
        [SerializeField] private GameObject aboutPanel;
        [SerializeField] private GameObject catalogPanel;
        [SerializeField] private RectTransform layoutRoot;
        private MuseumState state;
        private readonly Dictionary<string, PavilionFilterView> filters = new Dictionary<string, PavilionFilterView>();
        private readonly Dictionary<string, string> filterLabels = new Dictionary<string, string>();
        private readonly Dictionary<string, ExhibitRecord> records = new Dictionary<string, ExhibitRecord>();
        private readonly List<ExhibitCardView> cards = new List<ExhibitCardView>();
        public IReadOnlyList<ExhibitCardView> Cards => cards;
        public IReadOnlyDictionary<string, PavilionFilterView> Filters => filters;
        public ScrollRect ExhibitScroll => scroll;
        public Button AboutButton => aboutButton;
        public Button CloseAboutButton => closeAboutButton;
        public bool AboutVisible => aboutPanel.activeSelf;

        public void Configure(TextAsset data, PavilionFilterView filter, ExhibitCardView card, Transform filterRoot,
            RectTransform cardsRoot, ScrollRect scroller, TMP_Text results, TMP_Text status, Button catalog,
            Button top, Button about, Button close, GameObject aboutView, GameObject catalogView, RectTransform pageLayout = null)
        {
            catalogAsset = data; filterPrefab = filter; cardPrefab = card; filterContainer = filterRoot;
            cardContainer = cardsRoot; scroll = scroller; resultLabel = results; statusLabel = status;
            catalogButton = catalog; topButton = top; aboutButton = about; closeAboutButton = close;
            aboutPanel = aboutView; catalogPanel = catalogView;
            layoutRoot = pageLayout;
        }
        private void Start()
        {
            catalogButton.onClick.AddListener(() => { ShowAbout(false); SelectCategory(""); });
            topButton.onClick.AddListener(ScrollToTop);
            aboutButton.onClick.AddListener(() => ShowAbout(true));
            closeAboutButton.onClick.AddListener(() => ShowAbout(false));
            ShowAbout(false);
            try
            {
                if (catalogAsset == null) throw new FormatException("Bundled catalog is missing.");
                var catalog = MuseumCatalog.Parse(catalogAsset.text);
                state = new MuseumState(catalog);
                AddFilter("", "All exhibits");
                foreach (var pavilion in catalog.categories.OrderBy(item => item.displayOrder))
                    AddFilter(pavilion.categoryId, PavilionLabel(pavilion));
                foreach (var exhibit in catalog.exhibits)
                {
                    records.Add(exhibit.exhibitId, exhibit);
                    var card = Instantiate(cardPrefab, cardContainer);
                    cards.Add(card);
                    var record = exhibit;
                    card.Bind(record, false, () => state.ToggleExhibit(record.exhibitId));
                }
                state.Changed += Refresh;
                statusLabel.text = "Offline catalog / AR exploration coming later";
                Refresh();
            }
            catch (Exception exception)
            {
                resultLabel.text = "Catalog unavailable";
                statusLabel.text = "The local catalog could not be loaded. Please reopen the Museum.";
                Debug.LogError("IARA catalog: " + exception.Message, this);
            }
        }
        private void AddFilter(string id, string label)
        {
            var view = Instantiate(filterPrefab, filterContainer);
            filters.Add(id, view);
            filterLabels.Add(id, label);
            view.Bind(label, id == "", () => SelectCategory(id));
        }
        private static string PavilionLabel(PavilionRecord pavilion)
        {
            switch (pavilion.categoryId)
            {
                case "cryptography": return "Cryptography";
                case "early-computing": return "Early Computing";
                case "personal-computing": return "Personal Computing";
                default: return pavilion.title;
            }
        }
        private void SelectCategory(string id) { state?.SelectCategory(id); ScrollToTop(); }
        private void Refresh()
        {
            var visible = new HashSet<string>(state.VisibleExhibits.Select(item => item.exhibitId));
            foreach (var pair in filters)
            {
                var id = pair.Key;
                pair.Value.Bind(filterLabels[id], id == state.CategoryId, () => SelectCategory(id));
            }
            foreach (var card in cards)
            {
                card.gameObject.SetActive(visible.Contains(card.ExhibitId));
                var record = records[card.ExhibitId];
                card.Bind(record, state.IsExpanded(record.exhibitId), () => state.ToggleExhibit(record.exhibitId));
            }
            resultLabel.text = visible.Count + (visible.Count == 1 ? " exhibit" : " exhibits") + " / explore the collection";
            Canvas.ForceUpdateCanvases();
            // Resolve ancestor widths before measuring wrapped card text and content height.
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot != null ? layoutRoot : cardContainer);
            Canvas.ForceUpdateCanvases();
        }
        private void ScrollToTop() { scroll.StopMovement(); scroll.verticalNormalizedPosition = 1; }
        private void ShowAbout(bool visible)
        {
            aboutPanel.SetActive(visible); catalogPanel.SetActive(!visible);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(visible ? closeAboutButton.gameObject : aboutButton.gameObject);
        }
        private void OnDestroy() { if (state != null) state.Changed -= Refresh; }
    }
}
