using UnityEngine;

namespace Menus.Main_Menu.Components {
    public class LoadingSpinner : MonoBehaviour {
        public Vector3 rotation = new(0, 0, -5);

        private void FixedUpdate() {
            transform.Rotate(rotation);
        }
    }
}