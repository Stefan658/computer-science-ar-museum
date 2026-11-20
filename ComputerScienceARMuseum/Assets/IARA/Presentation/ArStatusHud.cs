using Iara.Application;
using TMPro;
using UnityEngine;

namespace Iara.Presentation
{
    public sealed class ArStatusHud : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour statusSource;
        [SerializeField] private TMP_Text label;
        private IArStatusSource source;
        public void Configure(MonoBehaviour value, TMP_Text text) { statusSource = value; label = text; }
        private void OnEnable()
        {
            source = statusSource as IArStatusSource;
            if (source != null) source.StatusChanged += Refresh;
            Refresh();
        }
        private void Refresh() { if (label != null) label.text = source?.StatusMessage ?? "AR status unavailable"; }
        private void OnDisable() { if (source != null) source.StatusChanged -= Refresh; source = null; }
    }
}
