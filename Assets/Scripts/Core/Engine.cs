using Misc;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Core {
    [RequireComponent(typeof(Volume))]
    public class Engine : Singleton<Engine> {
        [SerializeField] private GameObject integrations;
        private bool _nightVisionEnabled;
        private Volume _volume;
        public MonoBehaviour[] Integrations => integrations.GetComponentsInChildren<MonoBehaviour>();

        protected override void Awake() {
            base.Awake();
            DontDestroyOnLoad(this);
        }

        private void FixedUpdate() {
            _volume.weight += _nightVisionEnabled ? 0.01f : -1f;
            _volume.weight = Mathf.Clamp(_volume.weight, 0, 1);
        }

        private void OnEnable() {
            _volume = GetComponent<Volume>();
        }

        public void SetNightVisionActive(bool isActive) {
            _nightVisionEnabled = isActive;
            if (isActive) _volume.weight = 0.2f;
            RenderSettings.ambientIntensity = isActive ? Game.Instance.LoadedLevelData.environment.NightVisionAmbientLight : 0;
        }

        public void SetNightVisionColor(Color nightVisionColor) {
            _volume.profile.TryGet<ColorAdjustments>(out var colorAdjustments);
            colorAdjustments.colorFilter.value = nightVisionColor;
        }
    }
}