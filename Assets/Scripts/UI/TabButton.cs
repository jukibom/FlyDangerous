using Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI {
    [RequireComponent(typeof(Image))]
    public class TabButton : MonoBehaviour, ISubmitHandler, IPointerClickHandler {
        [SerializeField] private GameObject tabPanel;

        [SerializeField] private Color selectedColor;
        private Image _background;
        private Color _defaultColor;

        private TabGroup _tabGroup;

        private void Awake() {
            _background = GetComponent<Image>();
            _defaultColor = _background.color;
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (_tabGroup != null) _tabGroup.OnTabSelected(this);

            UIAudioManager.Instance.Play("ui-dialog-open");
        }

        public void OnSubmit(BaseEventData eventData) {
            if (_tabGroup != null) _tabGroup.OnTabSelected(this);

            UIAudioManager.Instance.Play("ui-dialog-open");
        }

        public void Subscribe(TabGroup tabGroup) {
            _tabGroup = tabGroup;
        }

        public void SetSelectedState(bool selected) {
            if (selected) {
                _background.color = selectedColor;
                tabPanel.SetActive(true);
            }
            else {
                if (_background != null) _background.color = _defaultColor;
                tabPanel.SetActive(false);
            }
        }
    }
}