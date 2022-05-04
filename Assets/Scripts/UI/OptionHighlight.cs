using System.Collections.Generic;
using System.Linq;
using Audio;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(SelectableElement))]
public class OptionHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler, ISubmitHandler,
    IPointerClickHandler {
    [SerializeField] private Image selectionImage;
    [SerializeField] private Selectable interactionObject;
    [SerializeField] private bool highlightOnlyOnSubmit;
    private Color _baseColor;
    private Color _hoverColor;

    private List<Selectable> _selectables;

    private bool _selected;

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

    public void OnDeselect(BaseEventData eventData) {
        selectionImage.enabled = false;
        _selected = false;
    }

    public void OnPointerClick(PointerEventData eventData) {
        // Only activate if the click originated outside the element to activate but within this boundary
        if (eventData.pointerPress != interactionObject.gameObject)
            ActivateElement();
    }

    public void OnPointerEnter(PointerEventData eventData) {
        selectionImage.enabled = true;
        if (!_selected) {
            selectionImage.color = _hoverColor;
            PlaySound();
        }
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (!_selected) {
            selectionImage.enabled = false;
            _selected = false;
        }
    }

    public void OnSelect(BaseEventData eventData) {
        if (!_selected) PlaySound();
        _selected = true;
        selectionImage.enabled = true;
        selectionImage.color = _baseColor;
    }

    public void OnSubmit(BaseEventData eventData) {
        ActivateElement();
    }

    private void ActivateElement() {
        if (highlightOnlyOnSubmit) {
            interactionObject.Select();
        }
        else {
            var ped = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(interactionObject.gameObject, ped, ExecuteEvents.submitHandler);
        }
    }

    private void PlaySound() {
        var audioManager = UIAudioManager.Instance;
        if (audioManager != null) audioManager.Play("ui-nav");
    }
}