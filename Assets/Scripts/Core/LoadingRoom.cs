using UnityEngine;
using UnityEngine.UI;

namespace Core {
    public class LoadingRoom : MonoBehaviour {
        [SerializeField] private Canvas loadingCanvas;
        [SerializeField] private Text centreDynamicLoadingText;
        [SerializeField] private Text footerDynamicLoadingText;

        public string CenterLoadingText {
            get => centreDynamicLoadingText.text;
            set => centreDynamicLoadingText.text = value;
        }

        public string FooterLoadingText {
            get => footerDynamicLoadingText.text;
            set => footerDynamicLoadingText.text = value;
        }

        private void OnEnable() {
            Game.OnVRStatus += ToggleVRStatus;
            ToggleVRStatus(Game.IsVREnabled);
        }

        private void OnDisable() {
            Game.OnVRStatus -= ToggleVRStatus;
        }

        private void ToggleVRStatus(bool vrEnabled) {
            if (vrEnabled) {
                loadingCanvas.renderMode = RenderMode.WorldSpace;
                var canvasTransform = loadingCanvas.transform;
                canvasTransform.localPosition = new Vector3(0, 0, -9);
                canvasTransform.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);
            }
            else {
                loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }
    }
}