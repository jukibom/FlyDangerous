using Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Misc {
    [RequireComponent(typeof(LensFlareComponentSRP))]
    public class LensFlareEdgeCaseHandler : MonoBehaviour {
        private LensFlareComponentSRP _lensFlare;

        private void Awake() {
            _lensFlare = GetComponent<LensFlareComponentSRP>();
        }

        private void Start() {
            OnApplySettings();
        }

        private void OnEnable() {
            Game.OnRestart += OnApplySettings;
            Game.OnGameSettingsApplied += OnApplySettings;
            Game.OnVRStatus += OnVRStatus;
        }

        private void OnDisable() {
            Game.OnRestart -= OnApplySettings;
            Game.OnGameSettingsApplied -= OnApplySettings;
            Game.OnVRStatus -= OnVRStatus;
        }

        private void OnApplySettings() {
            HandleVisibility(Game.Instance.IsVREnabled);
        }

        private void OnVRStatus(bool IsEnabled) {
            HandleVisibility(IsEnabled);
        }

        private void HandleVisibility(bool vrEnabled) {
            var shouldShow = true;

            // TODO: remove this when #199 fixed (MSAA breaks lens flares!)
            var urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            if (urp.msaaSampleCount != 0) shouldShow = false;

            // TODO: remove this when #200 fixed (lens flare broken in VR!)
            if (vrEnabled) shouldShow = false;

            _lensFlare.enabled = shouldShow;
        }
    }
}