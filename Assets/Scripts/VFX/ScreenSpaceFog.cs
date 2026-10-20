using System;
using Core;
using MapMagic.Core;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering;
#if !NO_PAID_ASSETS
#endif

namespace VFX {
    [ExecuteAlways]
    public class ScreenSpaceFog : MonoBehaviour {

        public struct ScreenSpaceFogUpdate {
            public bool fogEnabled;
            public float fogStartDistance;
            public float fogEndDistance;
        }
        
        public static event Action<ScreenSpaceFogUpdate> OnFogUpdate;

        [SerializeField] private bool fogEnabled = true;
        [SerializeField, EnableIf("fogEnabled")] private bool fogEnabledInSceneView = true;
        
        public void OnEnable() {
            if (SkyboxDepthFogRendererFeature.Instance != null) 
                SkyboxDepthFogRendererFeature.Instance.IsActive = fogEnabled;
            
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

        private void Update() {
            // editor only, just tie the fog render feature directly to this bool
            if (!Application.isPlaying) {
                if (SkyboxDepthFogRendererFeature.Instance != null) {
                    SkyboxDepthFogRendererFeature.Instance.IsActive = fogEnabled;
                    SkyboxDepthFogRendererFeature.Instance.IsActiveInSceneView = fogEnabledInSceneView;
                }
            } 
        }

        private void Refresh() {
#if !NO_PAID_ASSETS
            if (SkyboxDepthFogRendererFeature.Instance != null) {
                var fogStartDistance = 0f;
                var fogEndDistance = 1000f;
                var mapMagic = FindFirstObjectByType<MapMagicObject>();
                if (mapMagic) {
                    
                    var tileChunkCount = Preferences.Instance.GetFloat("graphics-terrain-chunks") + 1; // include drafts
                    var tileSize = mapMagic.tileSize.x;
                    var tileGenBuffer = 1.7f; // a little leeway for time to generate tiles in the distance
            
                    fogEndDistance = (tileSize * tileChunkCount - tileSize / 2) / tileGenBuffer;
                    fogStartDistance = Mathf.Max(fogEndDistance / 2, fogEndDistance - tileSize);
                }

                SkyboxDepthFogRendererFeature.Instance.SetFogDistances(fogStartDistance, fogEndDistance);
                OnFogUpdate?.Invoke(new ScreenSpaceFogUpdate
                    { fogEnabled = fogEnabled, fogStartDistance = fogStartDistance, fogEndDistance = fogEndDistance });
            }
#endif
        }
    }
}