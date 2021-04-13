using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AxisDropdown : MonoBehaviour, ISubmitHandler, IPointerClickHandler {

    public bool isChecked;
    public Text icon;
    public GameObject container;
    
    void Start() {
        isChecked = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isChecked) {
            icon.text = "+";
            container.SetActive(false);
        }
        else {
            icon.text = "-";
            container.SetActive(true);
        }
    }

    public void OnSubmit(BaseEventData eventData) {
        Toggle();
    }

    public void OnPointerClick(PointerEventData eventData) {
        Toggle();
    }

    private void Toggle() {
        isChecked = !isChecked;
    }
}
