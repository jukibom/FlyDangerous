using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering;

namespace Core {
    public class SkyboxFetcher : MonoBehaviour {

        private enum UpdateMode {
            OnAwake,
            RealtimeThrottled,
            Realtime,
        }
        
        [SerializeField] private UpdateMode updateMode = UpdateMode.OnAwake;

        [SerializeField, ShowIf("HasUpdateInterval")]
        private float updateInterval = 0.25f;
        
        [SerializeField] private int renderSize = 512; 
        
        [SerializeField, Required] private Camera skyboxRenderCamera;

        private int _currentRenderSize;
        private float _updateIntervalTimer;
        
        public RenderTexture RenderedSkyboxCubemap { get; private set; }

        private bool HasUpdateInterval => updateMode == UpdateMode.RealtimeThrottled;
        
        private void Awake() {
            InitRenderTexture();
            if (updateMode == UpdateMode.OnAwake) RenderSkybox();
        }

        private void Update() {
            if (renderSize != _currentRenderSize) {
                InitRenderTexture();
                if (updateMode == UpdateMode.OnAwake) RenderSkybox();
            }

            if (updateMode == UpdateMode.RealtimeThrottled) {
                _updateIntervalTimer += Time.deltaTime;
                if (_updateIntervalTimer >= updateInterval) {
                    _updateIntervalTimer = 0;
                    RenderSkybox();
                }
            }
            
            if (updateMode == UpdateMode.Realtime) RenderSkybox();
        }

        public void NotifyEnvironmentLoaded() {
            if (updateMode != UpdateMode.Realtime) RenderSkybox();
        }

        private void InitRenderTexture() {
            _currentRenderSize = renderSize;
            RenderedSkyboxCubemap = new RenderTexture(_currentRenderSize, renderSize, 16, RenderTextureFormat.ARGBHalf) {
                name = "_SkyboxDepthFogRenderedSkyboxCubemap",
                dimension = TextureDimension.Cube,
                useMipMap = false,
                autoGenerateMips = false,
                hideFlags = HideFlags.HideAndDontSave
            };;
        }

        private void RenderSkybox() {
            skyboxRenderCamera.transform.position = Vector3.zero;
            skyboxRenderCamera.transform.rotation = Quaternion.identity;
            skyboxRenderCamera.clearFlags = CameraClearFlags.Skybox;
            skyboxRenderCamera.cullingMask = 0;
            skyboxRenderCamera.nearClipPlane = 0.01f;
            skyboxRenderCamera.farClipPlane = 1f;
            skyboxRenderCamera.enabled = false;
            skyboxRenderCamera.RenderToCubemap(RenderedSkyboxCubemap);
        }
    }
}