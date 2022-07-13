using UnityEngine;

// Use this on stretched, centered elements within a parent container to constrain to a min and max width.
namespace FdUI {
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    public class ConstrainHorizontal : MonoBehaviour {
        [SerializeField] private float minWidth;
        [SerializeField] private float maxWidth;
        [SerializeField] private float horizontalPadding;
        private bool _needsUpdate;
        private RectTransform _parentRectTransform;
        private RectTransform _rectTransform;

        private void Update() {
            if (_needsUpdate) {
                if (!_parentRectTransform) return;

                // match parent if under max width
                if (_parentRectTransform.rect.width <= maxWidth + horizontalPadding * 2)
                    _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _parentRectTransform.rect.width - horizontalPadding * 2);
                // otherwise constrain to max
                else
                    _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);

                // constrain to min width
                if (_rectTransform.rect.width < minWidth) _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, minWidth);
            }
        }

        private void OnEnable() {
            _needsUpdate = true;
            _rectTransform = GetComponent<RectTransform>();
            _parentRectTransform = transform.parent != null ? transform.parent.GetComponent<RectTransform>() : null;

            // This shouldn't really be possible as a canvas element includes a rect transform and is a perfectly acceptable parent element to check
            if (!_parentRectTransform) Debug.LogWarning("Warning: Cannot constrain width on RectTransform: Parent entity does not include a RectTransform!");
        }

        private void OnRectTransformDimensionsChange() {
            _needsUpdate = true;
        }
    }
}