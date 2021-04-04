using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TabButton : MonoBehaviour, ISubmitHandler, IPointerClickHandler {

    [SerializeField]
    private TabGroup tabGroup;
    
    [SerializeField]
    private GameObject tabPanel;
    
    [SerializeField]
    private Color selectedColor;

    private Image _background;
    private Color _defaultColor;

    private void Awake() {
        _background = GetComponent<Image>();
        this._defaultColor = _background.color;
        tabGroup.Subscribe(this);
    }

    public void OnSubmit(BaseEventData eventData) {
        tabGroup.OnTabSelected(this);
        AudioManager.Instance.Play("ui-dialog-open");
    }

    public void OnPointerClick(PointerEventData eventData) {
        tabGroup.OnTabSelected(this);
        AudioManager.Instance.Play("ui-dialog-open");
    }

    public void SetSelectedState(bool enabled) {
        if (enabled) {
            this._background.color = this.selectedColor;
            this.tabPanel.SetActive(true);
        }
        else {
            this._background.color = this._defaultColor;
            this.tabPanel.SetActive(false);
        }
    }
}
