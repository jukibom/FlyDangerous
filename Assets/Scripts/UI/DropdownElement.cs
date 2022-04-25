using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropdownElement : MonoBehaviour, IUpdateSelectedHandler {
    public void OnUpdateSelected(BaseEventData eventData) {
        ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.updateSelectedHandler);
    }
}
