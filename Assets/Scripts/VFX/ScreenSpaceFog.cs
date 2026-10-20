using System;
using Core;
using MapMagic.Core;
using UnityEngine;
using UnityEngine.Rendering;
#if !NO_PAID_ASSETS
#endif

namespace VFX {
    [RequireComponent(typeof(Volume))]
    [ExecuteAlways]
    public class ScreenSpaceFog : MonoBehaviour {

        public void OnEnable() {
            if (SkyboxDepthFogRendererFeature.Instance != null) 
                SkyboxDepthFogRendererFeature.Instance.IsActive = true;
            
            if (Application.isPlaying)
                Game.OnGameSettingsApplied += OnGameSettingsApplied;
            else 
                Refresh();
        }

        public void OnDisable() {
            if (SkyboxDepthFogRendererFeature.Instance != null) 
                SkyboxDepthFogRendererFeature.Instance.IsActive = false;
            
            if (Application.isPlaying)
                Game.OnGameSettingsApplied -= OnGameSettingsApplied;
        }

        private void OnGameSettingsApplied() {
            Refresh();
        }

        private void Refresh() {
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