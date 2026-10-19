using Core;
using MapMagic.Core;
using UnityEngine;
using UnityEngine.Rendering;
#if !NO_PAID_ASSETS
#endif

namespace VFX {
    [RequireComponent(typeof(Volume))]
    public class ScreenSpaceFog : MonoBehaviour {
        private Volume _volume;

        public void Awake() {
            _volume = GetComponent<Volume>();
        }

        public void OnEnable() {
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
        }

        public void OnDisable() {
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
        }

        private void OnGameSettingsApplied() {
#if !NO_PAID_ASSETS
            if (SkyboxDepthFogRendererFeature.Instance != null) {
                var fogIntensity = 0f;
                var fogStartDistance = 0f;
                var fogEndDistance = 1000f;
                var mapMagic = FindFirstObjectByType<MapMagicObject>();
                if (mapMagic) {
                    fogIntensity = 1;
                    
                    var tileChunkCount = Preferences.Instance.GetFloat("graphics-terrain-chunks") + 1; // include drafts
                    var tileSize = mapMagic.tileSize.x;
                    var tileGenBuffer = 1.7f; // a little leeway for time to generate tiles in the distance
            
                    fogEndDistance = (tileSize * tileChunkCount - tileSize / 2) / tileGenBuffer;
                    fogStartDistance = Mathf.Max(fogEndDistance / 2, fogEndDistance - tileSize);
                }

                SkyboxDepthFogRendererFeature.Instance.SetFog(fogStartDistance, fogEndDistance, fogIntensity);
            }
#endif
        }
    }
}