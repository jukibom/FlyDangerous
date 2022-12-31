using UnityEngine;

namespace Menus.Main_Menu {
    public class MainMenuGeo : MonoBehaviour {
        private Transform _transform;

        private void Start() {
            _transform = transform;
        }

        private void FixedUpdate() {
            var position = _transform.position;
            position -= 30 * _transform.forward;
            if (position.z < -20000) position += _transform.forward * 40000;
            _transform.position = position;
        }
    }
}