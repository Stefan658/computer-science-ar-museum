using System;
using Iara.Content;
using UnityEngine;

namespace Iara.Application
{
    // Scene handoff contract. Museum navigation will call Select before loading ARExperience.
    public static class ExhibitSelection
    {
        public static string ExhibitId { get; private set; }
        public static event Action Changed;
        public static void Select(MuseumCatalog catalog, string exhibitId)
        {
            new MarkerMap(catalog).MarkerFor(exhibitId);
            if (ExhibitId == exhibitId) return;
            ExhibitId = exhibitId;
            Changed?.Invoke();
        }
        public static void Clear() { ExhibitId = null; Changed?.Invoke(); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { ExhibitId = null; Changed = null; }
    }

    public interface IArStatusSource
    {
        string StatusMessage { get; }
        event Action StatusChanged;
    }
}
