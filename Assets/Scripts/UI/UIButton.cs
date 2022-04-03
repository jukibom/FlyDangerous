using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI {
    public class UIButton : MonoBehaviour, ISelectHandler, IDeselectHandler, ISubmitHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {
        public delegate void OnButtonHighlightedAction(UIButton caller);

        public delegate void OnButtonSelectAction(UIButton caller);

        public delegate void OnButtonSubmitAction(UIButton caller);

        public delegate void OnButtonUnHighlightedAction(UIButton caller);

        [SerializeField] public Button button;
        [SerializeField] public Text label;

        public void OnDeselect(BaseEventData eventData) {
            OnButtonUnHighlightedEvent?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData) {
            OnButtonSubmitEvent?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData) {
            OnButtonHighlightedEvent?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData) {
            OnButtonUnHighlightedEvent?.Invoke(this);
        }

        public void OnSelect(BaseEventData eventData) {
            OnButtonSelectEvent?.Invoke(this);
        }

        public void OnSubmit(BaseEventData eventData) {
            OnButtonSubmitEvent?.Invoke(this);
        }

        public event OnButtonSelectAction OnButtonSelectEvent;
        public event OnButtonSubmitAction OnButtonSubmitEvent;
        public event OnButtonHighlightedAction OnButtonHighlightedEvent;
        public event OnButtonUnHighlightedAction OnButtonUnHighlightedEvent;
    }
}