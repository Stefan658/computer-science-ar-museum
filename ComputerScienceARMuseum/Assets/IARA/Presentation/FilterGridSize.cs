using UnityEngine;
using UnityEngine.UI;

namespace Iara.Presentation
{
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class FilterGridSize : MonoBehaviour, ILayoutSelfController
    {
        private void OnEnable() => Resize();
        private void OnRectTransformDimensionsChange() => Resize();
        public void SetLayoutHorizontal() => Resize();
        public void SetLayoutVertical() { }

        private void Resize()
        {
            var grid = GetComponent<GridLayoutGroup>();
            var width = ((RectTransform)transform).rect.width;
            if (grid == null || width <= 0) return;
            var columns = Mathf.Max(1, grid.constraintCount);
            var available = Mathf.Max(0, width - grid.padding.horizontal - grid.spacing.x * (columns - 1));
            var cellWidth = available / columns;
            if (!Mathf.Approximately(grid.cellSize.x, cellWidth))
                grid.cellSize = new Vector2(cellWidth, grid.cellSize.y);
        }
    }
}
