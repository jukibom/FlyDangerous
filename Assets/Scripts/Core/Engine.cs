using Misc;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Core {
    public class Engine : Singleton<Engine> {
        [SerializeField] private GameObject integrations;
        [SerializeField] private NightVision nightVision;
        public MonoBehaviour[] Integrations => integrations.GetComponentsInChildren<MonoBehaviour>();
        public NightVision NightVision => nightVision;

        protected override void Awake() {
            base.Awake();
            DontDestroyOnLoad(this);
        }
    }
}