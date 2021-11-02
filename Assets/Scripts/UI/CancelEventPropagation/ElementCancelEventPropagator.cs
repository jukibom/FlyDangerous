using System.Diagnostics.Eventing;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.CancelEventPropagation {
    public class ElementCancelEventPropagator : EventTrigger {
        private bool _elementInteracting;

        public override void OnSelect(BaseEventData eventData) {
            // User is currently interacting with the element (dropdown, slider, text field etc)
            _elementInteracting = false;
        }

        public override void OnDeselect(BaseEventData eventData) {
            // When the element is deselected, the user has stopped interacting with it (cancel has been pressed once)
            _elementInteracting = true;
        }

        public override void OnCancel(BaseEventData eventData) {
            // only propagate the event if the user is not interacting (e.g. cancel is to stop interacting with this element)
            if (!_elementInteracting) {
                ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.cancelHandler);
            }
        }
    }
}