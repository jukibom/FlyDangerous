using UnityEngine;
using UnityEngine.EventSystems;

namespace UI {
    
    /** When attached to a UI element in the Options panel, fires an event to a subscribed element to forward on a string explaining what the options does. */
    public class FdTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler {

        [SerializeField] private string toolTipText = "";

        public delegate void OnTextChangeEvent(string text);
        public event OnTextChangeEvent OnTextChange;
        
        public void OnPointerEnter(PointerEventData eventData) {
            Debug.Log("Pointer over! " + toolTipText);
            if (OnTextChange != null) OnTextChange(toolTipText);
        }

        public void OnPointerExit(PointerEventData eventData) {
            Debug.Log("Pointer exit!");
            if (OnTextChange != null) OnTextChange("");
        }

        public void OnSelect(BaseEventData eventData) {
            Debug.Log("Selected! " + toolTipText);
            if (OnTextChange != null) OnTextChange(toolTipText);
        }

        public void OnDeselect(BaseEventData eventData) {
            Debug.Log("Deselected!");
            if (OnTextChange != null) OnTextChange("");
        }
    }
}
