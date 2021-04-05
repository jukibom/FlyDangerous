using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class FlexibleGridLayout : LayoutGroup {

        public enum FitType {
            Uniform,
            Width,
            Height,
            FixedRow,
            FixedColumns
        }
        
        public FitType fitType;
        public int rows;
        public int columns;
        public Vector2 cellSize;
        public Vector2 spacing;
        public bool fitX;
        public bool fitY;
        
        public override void CalculateLayoutInputHorizontal() {
            base.CalculateLayoutInputHorizontal();

            if (IsTypeAuto()) {
                fitX = true;
                fitY = true;
                
                float sqrRt = Mathf.Sqrt(transform.childCount);
                rows = Mathf.CeilToInt(sqrRt);
                columns = Mathf.CeilToInt(sqrRt);
            }

            if (fitType == FitType.Width || fitType == FitType.FixedColumns) {
                rows = Mathf.CeilToInt(transform.childCount / (float) columns);
            }
            
            if (fitType == FitType.Height || fitType == FitType.FixedRow) {
                columns = Mathf.CeilToInt(transform.childCount / (float) rows);
            }

            Rect parentRect = rectTransform.rect;
            float parentWidth = parentRect.width;
            float parentHeight = parentRect.height;

            float spacingWidth = (spacing.x / columns) * 2;
            float spacingHeight = (spacing.y / rows) * 2;
            float paddingWidth = (padding.left / columns) - padding.right / columns;
            float paddingHeight = (padding.top / rows) - padding.bottom / rows;

            float cellWidth = parentWidth / (float) columns - spacingWidth - paddingWidth;
            float cellHeight = parentHeight / (float) rows - spacingHeight - paddingHeight;

            cellSize.x = fitX ? cellWidth : cellSize.x;
            cellSize.y = fitY ? cellHeight : cellSize.y;

            for (int i = 0; i < rectChildren.Count; i++) {
                int rowCount = i / columns;
                int columnCount = i % columns;

                RectTransform item = rectChildren[i];

                var xPos = (cellSize.x * columnCount) + (spacing.x * columnCount) + padding.left;
                var yPos = (cellSize.y * rowCount) + (spacing.y * rowCount) + padding.top;

                SetChildAlongAxis(item, 0, xPos, cellSize.x);
                SetChildAlongAxis(item, 1, yPos, cellSize.y);
            }
        }

        private bool IsTypeAuto() {
            return fitType == FitType.Uniform || fitType == FitType.Width || fitType == FitType.Height;
        }

        public override void CalculateLayoutInputVertical() {
        }

        public override void SetLayoutHorizontal() {
        }

        public override void SetLayoutVertical() {
        }
    }
}
