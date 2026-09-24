using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Services
{
    /// <summary>
    /// GridLayoutGroup의 셀 크기를 부모 폭에 맞춰 자동 계산한다. (정사각형)
    /// 열 수(columns)는 고정, 해상도/패널 크기가 바뀌어도 항상 딱 맞는다.
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public class AutoGridCellSize : MonoBehaviour
    {
        public int columns = 6;
        public float spacing = 12f;

        private void OnEnable() => Apply();
        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            var grid = GetComponent<GridLayoutGroup>();
            float width = ((RectTransform)transform).rect.width;
            if (width <= 0f || columns <= 0) return;

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.spacing = new Vector2(spacing, spacing);

            float cell = (width - grid.padding.horizontal - spacing * (columns - 1)) / columns;
            grid.cellSize = new Vector2(cell, cell);
        }
    }
}
