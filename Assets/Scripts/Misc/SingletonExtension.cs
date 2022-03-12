using UnityEngine;

namespace Misc {
    // Inherit from this base class to create a singleton.
    // e.g. public class MyClassName : Singleton<MyClassName> {}
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour {
        // Check to see if we're about to be destroyed.
        private static readonly object _lock = new();
        private static T _instance;

        // Access singleton instance through this propriety.
        public static T Instance {
            get {
                lock (_lock) {
                    if (_instance == null) {
                        // Search for existing instance.
                        _instance = (T)FindObjectOfType(typeof(T));

                        // Create new instance if one doesn't already exist.
                        if (_instance == null) {
                            // Need to create a new GameObject to attach the singleton to.
                            var singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = typeof(T) + " (Singleton)";

                            // Make instance persistent.
                            DontDestroyOnLoad(singletonObject);
                        }
                    }

                    return _instance;
                }
            }
        }

        protected virtual void Awake() {
            // if this class is created before a script calls it, it must be set in the unity editor
            if (_instance != null) Destroy(_instance.gameObject);

            _instance = GetComponent<T>();
        }
    }
}