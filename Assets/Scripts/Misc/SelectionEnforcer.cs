using UnityEngine;
using UnityEngine.EventSystems;

namespace Misc {
    // This script is intended to be attached to a high-level menu component to enforce selection when a mouse deselects something.
    public class SelectionEnforcer : MonoBehaviour {
        private EventSystem _eventSystem;
        private GameObject _selected;

        private void Start() {
            _eventSystem = EventSystem.current;
        }

        private void Update() {
            if (_eventSystem.currentSelectedGameObject != null && _eventSystem.currentSelectedGameObject != _selected)
                _selected = _eventSystem.currentSelectedGameObject;
            else if (_selected != null && _eventSystem.currentSelectedGameObject == null)
                _eventSystem.SetSelectedGameObject(_selected);
        }
    }
}