using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Core {
    [RequireComponent(typeof(Volume))]
    public class NightVision : MonoBehaviour {
        private bool _nightVisionEnabled;

        private Volume _volume;

        private void FixedUpdate() {
            _volume.weight += _nightVisionEnabled ? 0.008f : -1f;
            _volume.weight = Mathf.Clamp(_volume.weight, 0, 1);

            var targetAmbientIntensity = Game.Instance.LoadedLevelData.environment.NightVisionAmbientLight;

#if !UNITY_EDITOR
            // no idea why it's darker in release, I give up :(
            targetAmbientIntensity *= 1.8f;
#endif

            // gradually reduce ambient night vision light
            RenderSettings.ambientIntensity =
                _nightVisionEnabled
                    ? RenderSettings.ambientIntensity > targetAmbientIntensity
                        ? RenderSettings.ambientIntensity * 0.985f
                        : targetAmbientIntensity
                    : 0;
        }

        private void OnEnable() {
            _volume = GetComponent<Volume>();
        }

        public void SetNightVisionActive(bool isActive) {
            _nightVisionEnabled = isActive;
            if (isActive) _volume.weight = 0.5f;
            RenderSettings.ambientIntensity = isActive ? Game.Instance.LoadedLevelData.environment.NightVisionAmbientLight * 3 : 0;
        }

        public void SetNightVisionColor(Color nightVisionColor) {
            _volume.profile.TryGet<ColorAdjustments>(out var colorAdjustments);
            colorAdjustments.colorFilter.value = nightVisionColor;
        }
    }
}