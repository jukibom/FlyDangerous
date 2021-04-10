using System.Collections;
using System.Collections.Generic;
using Audio;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    public void OnPointerEnter(PointerEventData eventData) {
        PlaySound();
    }

    public void OnSelect(BaseEventData eventData) {
        PlaySound();
    }

    private void PlaySound() {
        AudioManager.Instance?.Play("ui-nav");
    }
}
