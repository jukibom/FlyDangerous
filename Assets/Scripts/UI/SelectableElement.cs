using System.Diagnostics.Eventing;
using Audio;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI {
    /**
     * Propagates UI events up the stack such that parent containers can hook into them
     * (these events are raised on selectable elements, not their parents)
     */
    public class SelectableElement : EventTrigger {
        private bool _elementInteracting;

        public override void OnPointerEnter(PointerEventData eventData) {
            PlaySound();
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.pointerEnterHandler);
        }

        public override void OnPointerDown(PointerEventData eventData) {
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.pointerDownHandler);
        }

        public override void OnPointerClick(PointerEventData eventData) {
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.pointerClickHandler);
        }

        public override void OnSelect(BaseEventData eventData) {
            PlaySound();
            
            // User is currently interacting with the element (dropdown, slider, text field etc)
            _elementInteracting = false;
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.selectHandler);
        }

        public override void OnDeselect(BaseEventData eventData) {
            // When the element is deselected, the user has stopped interacting with it (cancel has been pressed once)
            _elementInteracting = true;
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.deselectHandler);
        }

        public override void OnCancel(BaseEventData eventData) {
            // only propagate the event if the user is not interacting (e.g. cancel is to stop interacting with this element)
            if (!_elementInteracting) {
                ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.cancelHandler);
            }
        }

        public override void OnMove(AxisEventData eventData) {
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.moveHandler);
        }
        
        public override void OnScroll(PointerEventData eventData) {
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.scrollHandler);
        }
        
        private void PlaySound() {
            var audioManager = UIAudioManager.Instance;
            if (audioManager != null) {
                audioManager.Play("ui-nav");
            }
        }
    }
}