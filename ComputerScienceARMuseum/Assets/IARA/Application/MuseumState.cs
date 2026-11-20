using System;
using System.Collections.Generic;
using System.Linq;
using Iara.Content;

namespace Iara.Application
{
    // No UI or scene dependencies. Empty category means all exhibits.
    public sealed class MuseumState
    {
        private readonly MuseumCatalog catalog;
        private readonly HashSet<string> expanded = new HashSet<string>();
        public string CategoryId { get; private set; } = "";
        public IEnumerable<ExhibitRecord> VisibleExhibits => catalog.exhibits.Where(item => CategoryId == "" || item.categoryId == CategoryId);
        public event Action Changed;

        public MuseumState(MuseumCatalog catalog) => this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

        public void SelectCategory(string id)
        {
            if (id == null || (id != "" && !catalog.categories.Any(item => item.categoryId == id)))
                throw new ArgumentException("Unknown pavilion.", nameof(id));
            CategoryId = id;
            Changed?.Invoke();
        }
        public bool IsExpanded(string id) => expanded.Contains(id);
        public void ToggleExhibit(string id)
        {
            if (!catalog.exhibits.Any(item => item.exhibitId == id)) throw new ArgumentException("Unknown exhibit.", nameof(id));
            if (!expanded.Add(id)) expanded.Remove(id);
            Changed?.Invoke();
        }
    }
}
