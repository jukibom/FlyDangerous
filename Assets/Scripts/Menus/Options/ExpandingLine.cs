using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Options {
    public class ExpandingLine : MonoBehaviour {
        [SerializeField] private float animatePercentPerFrame = 0.01f;
        [SerializeField] private bool isOpen;
        public Text icon;

        // Primary Container (should have a canvas group for opacity)
        public GameObject container;

        // Containers to iterate through in order to modify child element heights.
        // This may just contain the primary container children of it.
        public List<GameObject> scaleElementsContainers;

        private Coroutine _animation;

        private float _currentAnimationHeightPercent;

        // Map of child elements => initial (target) height
        private Dictionary<RectTransform, float> _elements;

        public bool IsOpen {
            get => isOpen;
            set {
                isOpen = value;
                container.SetActive(isOpen);
                icon.text = isOpen ? "-" : "+";
            }
        }

        private void Start() {
            IsOpen = isOpen;
        }

        private void OnEnable() {
            // get one layer deep of elements in each attached container
            var children = new List<RectTransform>();
            scaleElementsContainers.ForEach(childContainer => {
                children.AddRange(childContainer.GetComponentsInChildren<RectTransform>()
                    .ToList()
                    .FindAll(rectTransform => rectTransform.transform.parent == childContainer.transform)
                );
            });

            // var children = container.GetComponentsInChildren<RectTransform>();
            if (_elements == null || _elements.Count != children.Count) {
                _elements = new Dictionary<RectTransform, float>();
                children.ForEach(rectTransform => _elements.Add(rectTransform, rectTransform.rect.height));
            }
        }

        public void Toggle() {
            isOpen = !isOpen;
            HandleToggleLogic();
        }

        private void HandleToggleLogic() {
            if (_animation != null) StopCoroutine(_animation);

            IEnumerator AnimatePanel(ExpandingLineDirection direction, Action onComplete = null) {
                var canvasGroup = container.GetComponent<CanvasGroup>();

                float targetHeightPercent = direction == ExpandingLineDirection.Open ? 1 : 0;
                float startingHeightPercent = direction == ExpandingLineDirection.Open ? 0 : 1;
                // float currentAnimationHeightPercent = startingHeightPercent;

                bool HeightAssertion() {
                    return startingHeightPercent > targetHeightPercent
                        ? _currentAnimationHeightPercent >= targetHeightPercent
                        : _currentAnimationHeightPercent <= targetHeightPercent;
                }

                var increment = animatePercentPerFrame * (direction == ExpandingLineDirection.Open ? 1 : -1);

                while (HeightAssertion()) {
                    foreach (var rectTransformHeightPair in _elements) {
                        var rectTransformCanonicalHeight = rectTransformHeightPair.Value;
                        var size = new Vector2(0, _currentAnimationHeightPercent * rectTransformCanonicalHeight);

                        var rectTransform = rectTransformHeightPair.Key;
                        var rect = rectTransform.rect;
                        size.x = rect.width;
                        rectTransform.sizeDelta = size;
                    }

                    _currentAnimationHeightPercent += increment;

                    canvasGroup.alpha = _currentAnimationHeightPercent;

                    RefreshView();

                    yield return new WaitForEndOfFrame();
                }

                onComplete?.Invoke();
            }

            if (isOpen) {
                icon.text = "-";
                UIAudioManager.Instance.Play("ui-dialog-open");
                container.SetActive(true);
                _animation = StartCoroutine(AnimatePanel(ExpandingLineDirection.Open));
            }
            else {
                icon.text = "+";
                UIAudioManager.Instance.Play("ui-cancel");
                _animation = StartCoroutine(AnimatePanel(ExpandingLineDirection.Close, () => {
                    container.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
                    container.SetActive(false);
                    RefreshView();
                }));
            }
        }

        private void RefreshView() {
            var parentContentFitters = GetComponentsInParent<ContentFitterRefresh>();
            foreach (var contentFitterRefresh in parentContentFitters) contentFitterRefresh.RefreshContentFitters();
        }

        private enum ExpandingLineDirection {
            Open,
            Close
        }
    }
}