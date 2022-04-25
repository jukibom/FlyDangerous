using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(SelectableElement))]
public class OptionHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler, ISubmitHandler {
    [SerializeField] private Image selectionImage;
    [SerializeField] private Selectable interactionObject;
    [SerializeField] private bool hightlightOnlyOnSubmit;

    private bool _selected;
    private Color _baseColor;
    private Color _hoverColor;
    
    private List<Selectable> _selectables;
    
    private void OnEnable() {
        selectionImage.enabled = false;
        _baseColor = selectionImage.color;
        _hoverColor = new Color(_baseColor.r, _baseColor.g, _baseColor.b, _baseColor.a / 2);
        GetComponent<SelectableElement>().playSound = false;
        
        // get all selectable elements in the hierarchy excluding this one
        _selectables = GetComponentsInChildren<Selectable>()
            .ToList()
            .FindAll(s => s.gameObject != gameObject);
    }

    public void OnSelect(BaseEventData eventData) {
        _selected = true;
        selectionImage.enabled = true;
        selectionImage.color = _baseColor;

        IEnumerator SelectOption() {
            yield return new WaitForEndOfFrame();
            var anyChildSelected = _selectables.Find(s => EventSystem.current.currentSelectedGameObject == s.gameObject) != null;
            if (_selected && !anyChildSelected)
                interactionObject.Select();
        }

        if (!hightlightOnlyOnSubmit) StartCoroutine(SelectOption());
    }

    public void OnDeselect(BaseEventData eventData) {
        selectionImage.enabled = false;
        _selected = false;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        selectionImage.enabled = true;
        if (!_selected) selectionImage.color = _hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (!_selected) selectionImage.enabled = false;
    }

    public void OnSubmit(BaseEventData eventData) {
        if (hightlightOnlyOnSubmit) {
            interactionObject.Select();
        }
    }
}
