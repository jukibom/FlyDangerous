using UnityEngine;
using UnityEngine.EventSystems;

namespace FdUI {
    public class DropdownElement : MonoBehaviour, IUpdateSelectedHandler {
        public void OnUpdateSelected(BaseEventData eventData) {
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.updateSelectedHandler);
        }
    }
}
