using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Misc;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menus.Options {
    public class ExpandingLine : MonoBehaviour, ISubmitHandler, IPointerClickHandler {

        public bool isOpen;
        public Text icon;
        public GameObject container;
        private List<RectTransform> _elements;

        private Coroutine _animation;
        private float _currentAnimationStep = 0;

        void Start() {
            isOpen = false;
            container.SetActive(false);
        }

        private void OnEnable() {
            // get one layer deep of elements in the container
            _elements = container.GetComponentsInChildren<RectTransform>()
                .ToList()
                .FindAll(rect => rect.transform.parent == container.transform);
        }

        public void OnSubmit(BaseEventData eventData) {
            Toggle();
        }

        public void OnPointerClick(PointerEventData eventData) {
            Toggle();
        }

        private void Toggle() {
            isOpen = !isOpen;
            HandleToggleLogic();
        }

        private void HandleToggleLogic() {

            if (_animation != null) {
                StopCoroutine(_animation);
            }

            IEnumerator AnimatePanel(float startingHeight, float targetHeight, Action onComplete = null) {
                var canvasGroup = container.GetComponent<CanvasGroup>();
                float height = startingHeight;

                bool HeightAssertion() =>
                    startingHeight > targetHeight
                        ? height >= targetHeight
                        : height <= targetHeight;

                float increment = startingHeight > targetHeight ? -5 : 5;

                while (HeightAssertion()) {
                    foreach (var rectTransform in _elements) {
                        var rect = rectTransform.rect;
                        rectTransform.sizeDelta = new Vector2(rect.width, height);
                    }

                    height += increment;
                    canvasGroup.alpha = startingHeight > targetHeight
                        ? MathfExtensions.Remap(targetHeight, startingHeight, 0, 1, height)
                        : MathfExtensions.Remap(startingHeight, targetHeight, 0, 1, height);

                    RefreshView();

                    _currentAnimationStep = height;
                    yield return new WaitForEndOfFrame();
                }

                onComplete?.Invoke();
            }

            if (isOpen) {
                icon.text = "-";
                UIAudioManager.Instance.Play("ui-dialog-open");
                container.SetActive(true);
                StartCoroutine(AnimatePanel(_currentAnimationStep, 80));
            }
            else {
                icon.text = "+";
                UIAudioManager.Instance.Play("ui-cancel");
                StartCoroutine(AnimatePanel(_currentAnimationStep, 0, () => {
                    container.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
                    container.SetActive(false);
                    RefreshView();
                }));
            }
        }

        private void RefreshView() {
            var parentContentFitters = GetComponentsInParent<ContentFitterRefresh>();
            foreach (var contentFitterRefresh in parentContentFitters) {
                contentFitterRefresh.RefreshContentFitters();
            }
        }
    }
}
