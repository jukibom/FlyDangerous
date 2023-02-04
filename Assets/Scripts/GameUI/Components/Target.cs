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
        [SerializeField] private GameObject targetIndicator2d;

        [SerializeField] private Transform IndicatorModelTransform;
        [SerializeField] private GameObject Front3dIndicator;

        private MeshRenderer[] _front3dIndicatorMeshRenderers;

        private CanvasGroup _canvasGroup;
        private float _targetDistanceMeters;
        private bool _is3dIndicatorActive;

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
            _front3dIndicatorMeshRenderers = Front3dIndicator.GetComponentsInChildren<MeshRenderer>();
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

        public void Update3dIndicatorOrientation(Transform matchTransform, Transform cameraTransform) {
            var orientation = matchTransform.rotation;
            IndicatorModelTransform.gameObject.SetActive(_is3dIndicatorActive && _targetDistanceMeters > 100);
            IndicatorModelTransform.rotation = orientation;

            var frontOpacity = Mathf.Abs(Vector3.SignedAngle(cameraTransform.forward,
                    cameraTransform.InverseTransformDirection(matchTransform.forward), cameraTransform.right))
                .Remap(30, 180, 0, 1);

            foreach (var meshRenderer in _front3dIndicatorMeshRenderers) {
                var meshMaterial = meshRenderer.material;
                meshMaterial.color = new Color(meshMaterial.color.r, meshMaterial.color.g, meshMaterial.color.b, frontOpacity);
            }
        }

        public void Toggle3dIndicator(bool isActive) {
            _is3dIndicatorActive = isActive;
        }

        public void Toggle2dIndicator(bool isActive) {
            targetIndicator2d.SetActive(isActive);
        }
    }
}