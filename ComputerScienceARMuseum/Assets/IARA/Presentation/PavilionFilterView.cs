using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Iara.Presentation
{
    public sealed class PavilionFilterView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image background;
        public Button Button => button;
        public void Configure(Button control, TMP_Text text, Image image)
        { button = control; label = text; background = image; }
        public void Bind(string title, bool selected, Action onSelect)
        {
            label.text = selected ? "Selected / " + title : title;
            label.color = selected ? new Color32(15, 30, 38, 255) : new Color32(239, 244, 247, 255);
            background.color = selected ? new Color32(119, 230, 196, 255) : new Color32(36, 54, 68, 255);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelect());
        }
    }
}
