using UnityEngine;
using UnityEngine.UI;

namespace GameUI.Components.Terrain_Indicator {
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(CanvasGroup))]
    [ExecuteAlways]
    public class HeightDeltaIndicatorPip : MonoBehaviour {
        private Image _image;
        private CanvasGroup _canvasGroup;

        [SerializeField] private float targetAlpha = 0.8f;
        [SerializeField] private Vector3 targetScale = Vector3.one;
        [SerializeField] private Color targetColor = Color.white;

        private bool _isActive;
        private Color _primaryColor = Color.white;

        private void OnEnable() {
            _image = GetComponent<Image>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void FixedUpdate() {
            _image.color = Color.Lerp(_image.color, targetColor, 0.01f);
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, targetAlpha, 0.05f);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 0.15f);
        }

        private void OnGUI() {
            FixedUpdate();
        }

        public void SetPrimaryColor(Color primaryColor) {
            _primaryColor = primaryColor;
            targetColor = primaryColor;
        }

        public void SetPipActive(bool isActive, bool isWarningPip = false) {
            targetAlpha = _isActive ? 0.8f : 0.05f;

            if (isWarningPip && isActive != _isActive) {
                if (isActive) {
                    transform.localScale = new Vector3(2.5f, 1.75f, 1.2f);
                    _canvasGroup.alpha = 1;
                    _image.color = Color.white;
                    targetColor = Color.red;
                }
                else {
                    targetColor = _primaryColor;
                }
            }

            _isActive = isActive;
        }
    }
}