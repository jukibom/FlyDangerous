using System.Collections;
using System.Collections.Generic;
using Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IScrollHandler {
    [SerializeField] public Button button;
    [SerializeField] public Text label;
    public void OnPointerEnter(PointerEventData eventData) {
        PlaySound();
    }

    public void OnSelect(BaseEventData eventData) {
        PlaySound();
    }

    public void OnScroll(PointerEventData eventData) {
        // propagate event further up
        ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.scrollHandler);
    }

    private void PlaySound() {
        AudioManager.Instance?.Play("ui-nav");
    }

}
