using UnityEngine;

namespace Menus.Main_Menu.Components {
    public class LoadingSpinner : MonoBehaviour {
        public Vector3 rotation = new Vector3(0, 0, -1);
        private void FixedUpdate() {
            transform.Rotate(rotation);
        }
    }
}
