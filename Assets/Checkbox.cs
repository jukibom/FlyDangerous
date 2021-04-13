using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Checkbox : MonoBehaviour, ISubmitHandler, IPointerClickHandler {

    public string preference;
    public bool isChecked;
    public Image statusImage;
    
    // Start is called before the first frame update
    void Start() {
        isChecked = PlayerPrefs.GetInt(preference) == 1;
    }
    
    public void Update() {
        statusImage.enabled = isChecked;
    }

    public void OnSubmit(BaseEventData eventData) {
        Toggle();
    }


    public void OnPointerClick(PointerEventData eventData) {
        Toggle();
    }

    private void Toggle() {
        // TODO: Emit an event?
        isChecked = !isChecked;
        PlayerPrefs.SetInt(preference, isChecked ? 1 : 0);
    } 
}
