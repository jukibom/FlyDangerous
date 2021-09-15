using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI {
    [RequireComponent(typeof(Image))]
    public class TabButton : MonoBehaviour, ISubmitHandler, IPointerClickHandler {

        [SerializeField] private GameObject tabPanel;

        [SerializeField] private Color selectedColor;

        private TabGroup _tabGroup;
        private Image _background;
        private Color _defaultColor;

        private void Awake() {
            _background = GetComponent<Image>();
            this._defaultColor = _background.color;
        }

        public void Subscribe(TabGroup tabGroup) {
            this._tabGroup = tabGroup;
        }

        public void OnSubmit(BaseEventData eventData) {
            if (this._tabGroup != null) {
                _tabGroup.OnTabSelected(this);
            }

            UIAudioManager.Instance.Play("ui-dialog-open");
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (this._tabGroup != null) {
                _tabGroup.OnTabSelected(this);
            }

            UIAudioManager.Instance.Play("ui-dialog-open");
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
}