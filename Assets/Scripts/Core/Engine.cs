using UnityEngine;

namespace Core {
    public class Engine : MonoBehaviour {
        private void Awake() {
            // all other core engine components are children of this component, so keep it alive
            DontDestroyOnLoad(gameObject);
        }
    }
}