using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI {
    public class ScrollablePanel : MonoBehaviour, ISelectHandler, IPointerDownHandler {
        [SerializeField] private float lerpTime;
        private ScrollRect _scrollRect;
        private int _index;
        private Coroutine _animation;

        public void Start()
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        public void OnSelect(BaseEventData eventData) {
            var selectedElement = EventSystem.current.currentSelectedGameObject;
            var positionInScrollRect = _scrollRect.content.InverseTransformPoint(selectedElement.transform.position) * -1;

            AnimateScrollbarToNormalizedPosition(1 - (positionInScrollRect.y / (_scrollRect.content.rect.height - 60)));
        }

        public void OnPointerDown(PointerEventData eventData) {
            CancelAnimation();
        }

        private void AnimateScrollbarToNormalizedPosition(float position) {
            CancelAnimation();
            IEnumerator AnimatePanel(float targetPosition) {
                yield return new WaitForEndOfFrame();
                while(Mathf.Abs(_scrollRect.verticalNormalizedPosition - targetPosition) > 0.01) {
                    _scrollRect.verticalNormalizedPosition = Mathf.Lerp(
                        _scrollRect.verticalNormalizedPosition, targetPosition, Time.fixedDeltaTime / lerpTime);
                    yield return new WaitForEndOfFrame();
                }
            }

            _animation = StartCoroutine(AnimatePanel(position));
        }

        private void CancelAnimation() {
            if (_animation != null) {
                StopCoroutine(_animation);
            }
        }
    }
}