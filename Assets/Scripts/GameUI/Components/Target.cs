using System.Globalization;
using JetBrains.Annotations;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI.Components {
    [RequireComponent(typeof(CanvasGroup))]
    public class Target : MonoBehaviour {
        [SerializeField] private Text targetNameText;
        [SerializeField] private Text targetDistanceText;
        [SerializeField] private Image icon;
        [SerializeField] private Image outline;
        [SerializeField] private GameObject targetIndicator2d;

        [SerializeField] private Transform IndicatorModelTransform;
        [SerializeField] private Indicator3D Indicator3D;

        private CanvasGroup _canvasGroup;
        private float _targetDistanceMeters;
        private bool _is3dIndicatorActive;
        private float _facingForwardNormalized;

        public string Name {
            get => targetNameText.text;
            set => targetNameText.text = value;
        }

        public float DistanceMeters {
            get => _targetDistanceMeters;
            set {
                _targetDistanceMeters = value;
                UpdateDistanceText();
            }
        }

        public bool IsSpectatorTarget { get; set; } = true;

        [CanBeNull]
        public Sprite Icon {
            get => icon.sprite;
            set {
                icon.gameObject.SetActive(value != null);
                targetNameText.alignment = value != null ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
                icon.sprite = value;
            }
        }

        public float Opacity {
            get => _canvasGroup.alpha;
            set => _canvasGroup.alpha = value;
        }

        private void OnEnable() {
            _canvasGroup = GetComponent<CanvasGroup>();
            Opacity = 0;
        }

        private void UpdateDistanceText() {
            string AddPointZeroIfNeeded(float distance) {
                return distance % 1 == 0 ? distance + ".0" : distance.ToString(CultureInfo.CurrentCulture);
            }

            string text;
            if (_targetDistanceMeters < 850)
                text = Mathf.Round(_targetDistanceMeters) + "m";
            else if (_targetDistanceMeters < 850000)
                text = AddPointZeroIfNeeded(Mathf.Round(_targetDistanceMeters / 100) / 10) + "Km";
            else if (_targetDistanceMeters < 29979245.8f)
                text = AddPointZeroIfNeeded(Mathf.Round(_targetDistanceMeters / 100000) / 10) + "Mm";
            else
                text = AddPointZeroIfNeeded(Mathf.Max(0.1f, Mathf.Round(_targetDistanceMeters / 29980000f) / 10)) + "Ls";

            targetDistanceText.text = text;
        }

        public void Update3dIndicatorFromOrientation(Transform matchTransform, Transform cameraTransform) {
            var orientation = matchTransform.rotation;
            IndicatorModelTransform.gameObject.SetActive(_is3dIndicatorActive && _targetDistanceMeters > 100);
            IndicatorModelTransform.rotation = orientation;

            var radialDirection = (IndicatorModelTransform.position - cameraTransform.position).normalized;
            var angle = Vector3.SignedAngle(radialDirection, IndicatorModelTransform.forward, IndicatorModelTransform.up);

            // get opacity value from the target facing the camera and facing AWAY from the camera
            var backwardOpacity = Mathf.Abs(angle).Remap(165, 135, 0, 1);
            var forwardOpacity = Mathf.Abs(angle).Remap(15, 45, 0, 1);

            var indicator3dFacingNormalised = backwardOpacity * forwardOpacity;

            _facingForwardNormalized = Mathf.Lerp(_facingForwardNormalized, indicator3dFacingNormalised, 0.9f);
            Indicator3D.SetFacingValueNormalized(_facingForwardNormalized);

            targetDistanceText.enabled = IsSpectatorTarget;
            outline.enabled = IsSpectatorTarget;
        }

        public void SetColor(Color color) {
            outline.color = color;
            targetDistanceText.color = color;
            targetNameText.color = color;
            Indicator3D.SetUIColor(color);
        }

        public void Toggle3dIndicator(bool isActive) {
            _is3dIndicatorActive = isActive;
        }

        public void Toggle2dIndicator(bool isActive) {
            targetIndicator2d.SetActive(isActive);
        }
    }
}