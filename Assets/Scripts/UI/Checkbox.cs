using Audio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI {
    public class Checkbox : MonoBehaviour, ISubmitHandler, IPointerClickHandler {
        [SerializeField] public UnityEvent<bool> onToggle;
        public string preference;
        public bool isChecked;
        public Image statusImage;

        public void Update() {
            statusImage.enabled = isChecked;
        }

        public void OnPointerClick(PointerEventData eventData) {
            Toggle();
        }

        public void OnSubmit(BaseEventData eventData) {
            Toggle();
        }

        private void Toggle() {
            isChecked = !isChecked;
            if (isChecked)
                UIAudioManager.Instance.Play("ui-confirm");
            else
                UIAudioManager.Instance.Play("ui-cancel");

            onToggle?.Invoke(isChecked);
        }
    }
}