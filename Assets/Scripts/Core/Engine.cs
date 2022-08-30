using Misc;
using UnityEngine;

namespace Core {
    public class Engine : Singleton<Engine> {
        [SerializeField] private GameObject integrations;
        public MonoBehaviour[] Integrations => integrations.GetComponentsInChildren<MonoBehaviour>();

        protected override void Awake() {
            base.Awake();
            DontDestroyOnLoad(this);
        }
    }
}