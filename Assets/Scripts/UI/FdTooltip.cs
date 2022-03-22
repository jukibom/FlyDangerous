using UnityEngine;
using UnityEngine.EventSystems;

namespace UI {
    /**
     * When attached to a UI element in the Options panel, fires an event to a subscribed element to forward on a string explaining what the options does.
     */
    public class FdTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler {
        public delegate void OnTextChangeEvent(string text);

        [SerializeField] private string toolTipText = "";

        public void OnDeselect(BaseEventData eventData) {
            if (OnTextChange != null) OnTextChange("");
        }

        public void OnPointerEnter(PointerEventData eventData) {
            if (OnTextChange != null) OnTextChange(toolTipText);
        }

        public void OnPointerExit(PointerEventData eventData) {
            if (OnTextChange != null) OnTextChange("");
        }

        public void OnSelect(BaseEventData eventData) {
            if (OnTextChange != null) OnTextChange(toolTipText);

            // propagate up the stack for auto scrolling
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.selectHandler);
        }

        public event OnTextChangeEvent OnTextChange;
    }
}