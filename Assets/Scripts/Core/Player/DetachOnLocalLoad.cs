using UnityEngine;

namespace Core.Player {
    /// <summary>
    /// Simple component, listens for a signal to detach the game object from whatever prefab it lives in and moves to root / DoNotDestroyOnLoad.
    /// </summary>
    public class DetachOnLocalLoad : MonoBehaviour, Tools.IObserver<bool> {
        
        private void Awake() {
            ShipPlayer.LocalPlayerLoadedObservable.Subscribe(this);
        }

        void OnDestroy() {
            ShipPlayer.LocalPlayerLoadedObservable.Unsubscribe(this);
        }

        public void OnValueChange(bool value) {
            if (value) {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}