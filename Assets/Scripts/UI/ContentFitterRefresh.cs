using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class ContentFitterRefresh : MonoBehaviour {
        private void Awake() {
            RefreshContentFitters();
        }

        public void RefreshContentFitters() {
            var rectTransform = (RectTransform)transform;
            RefreshContentFitter(rectTransform);
        }

        private void RefreshContentFitter(RectTransform rectTransform) {
            if (rectTransform == null || !rectTransform.gameObject.activeSelf) return;

            foreach (RectTransform child in rectTransform) RefreshContentFitter(child);

            var layoutGroup = rectTransform.GetComponent<LayoutGroup>();
            var contentSizeFitter = rectTransform.GetComponent<ContentSizeFitter>();
            if (layoutGroup != null) {
                layoutGroup.SetLayoutHorizontal();
                layoutGroup.SetLayoutVertical();
            }

            if (contentSizeFitter != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }
}