using System;
using System.Globalization;
using Misc;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game_UI {
    public class MouseWidget : MonoBehaviour {
        [SerializeField] private GameObject crosshair;
        [SerializeField] private GameObject arrow;
        [SerializeField] private GameObject deadzone;
        [SerializeField] private GameObject extents;
        [SerializeField] private GameObject inputText;
        [SerializeField] private Text xText;
        [SerializeField] private Text yText;
        [SerializeField] private Text magText;

        [FormerlySerializedAs("mousePositionNormalised")]
        public Vector2 widgetPositionNormalized = Vector2.zero;

        [FormerlySerializedAs("mouseInputActual")]
        public Vector2 inputActualNormalized = Vector2.zero;

        public float maxDistanceUnits = 1f;

        private Image _arrowImage;
        private Image _crosshairImage;
        private Image _deadzoneImage;
        private Image _extentsImage;
        private float _deadzoneValue;

        public bool ShouldShowWidget { get; set; }
        public bool ShouldShowText { get; set; }

        // Start is called before the first frame update
        private void Start() {
            _crosshairImage = crosshair.GetComponent<Image>();
            _arrowImage = arrow.GetComponent<Image>();
            _deadzoneImage = deadzone.GetComponentInChildren<Image>();
            _extentsImage = extents.GetComponentInChildren<Image>();
        }

        // Update is called once per frame
        private void Update() {
            // pref determines draw active
            crosshair.SetActive(ShouldShowWidget);
            arrow.SetActive(ShouldShowWidget);
            deadzone.SetActive(ShouldShowWidget);
            extents.SetActive(ShouldShowWidget);
            inputText.SetActive(ShouldShowText);

            // map our "square" xy coordinates to a circle
            Vector2 mappedMousePosition;
            mappedMousePosition.x =
                widgetPositionNormalized.x * Mathf.Sqrt(1 - Mathf.Pow(widgetPositionNormalized.y, 2) / 2);
            mappedMousePosition.y =
                widgetPositionNormalized.y * Mathf.Sqrt(1 - Mathf.Pow(widgetPositionNormalized.x, 2) / 2);

            Vector2 mappedInput;
            mappedInput.x =
                inputActualNormalized.x * Mathf.Sqrt(1 - Mathf.Pow(inputActualNormalized.y, 2) / 2);
            mappedInput.y =
                inputActualNormalized.y * Mathf.Sqrt(1 - Mathf.Pow(inputActualNormalized.x, 2) / 2);

            // position
            arrow.transform.localPosition = Vector3.ClampMagnitude(new Vector3(
                mappedMousePosition.x * maxDistanceUnits,
                mappedMousePosition.y * maxDistanceUnits,
                0
            ), maxDistanceUnits);

            // rotation
            if (widgetPositionNormalized != Vector2.zero) {
                var dir = arrow.transform.localPosition;
                var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                arrow.transform.localRotation = Quaternion.AngleAxis(angle - 90, Vector3.forward);
            }

            var arrowImageColor = _arrowImage.color;
            var crosshairImageColor = _crosshairImage.color;
            var deadzoneImageColor = _deadzoneImage.color;
            var extentsImageColor = _extentsImage.color;

            var normalisedMagnitude = arrow.transform.localPosition.magnitude / maxDistanceUnits;
            arrowImageColor.a = normalisedMagnitude;
            crosshairImageColor.a = Mathf.Pow(1f - normalisedMagnitude, 2);

            deadzoneImageColor.a = normalisedMagnitude.Remap(0, _deadzoneValue, 0, 0.15f);
            if (normalisedMagnitude > _deadzoneValue) deadzoneImageColor.a = 0.5f;

            extentsImageColor.a = normalisedMagnitude.Remap(_deadzoneValue, 1, 0, 0.15f);
            if (normalisedMagnitude >= 0.99f) extentsImageColor.a = 0.5f;

            _crosshairImage.transform.localScale = Vector3.one * (2 * Math.Min(0.4f, normalisedMagnitude) + 1);
            _arrowImage.transform.localScale = Vector3.one * Mathf.Min(1, normalisedMagnitude + 0.5f);
            deadzone.transform.localScale =
                Vector3.one * (_deadzoneValue * normalisedMagnitude.Remap(0, _deadzoneValue, 0.8f, 1));
            extents.transform.localScale =
                Vector3.one * normalisedMagnitude.Remap(0, 1, 0.9f, 1);

            _arrowImage.color = arrowImageColor;
            _crosshairImage.color = crosshairImageColor;
            _deadzoneImage.color = deadzoneImageColor;
            _extentsImage.color = extentsImageColor;

            xText.text = $"{inputActualNormalized.x.ToString("F2", CultureInfo.CurrentCulture)}x";
            yText.text = $"{inputActualNormalized.y.ToString("F2", CultureInfo.CurrentCulture)}y";
            magText.text = $"{mappedInput.magnitude.ToString("F2", CultureInfo.CurrentCulture)}tot";
        }

        public void UpdateWidgetSprites(Vector2 mousePosNormalised, Vector2 mouseActualInput) {
            widgetPositionNormalized.x = Mathf.Clamp(mousePosNormalised.x, -1, 1);
            widgetPositionNormalized.y = Mathf.Clamp(mousePosNormalised.y, -1, 1);
            inputActualNormalized.x = Mathf.Clamp(mouseActualInput.x, -1, 1);
            inputActualNormalized.y = Mathf.Clamp(mouseActualInput.y, -1, 1);
        }

        public void UpdateDeadzone(float deadzoneVal) {
            _deadzoneValue = deadzoneVal;
        }

        public void ResetToCentre() {
            UpdateWidgetSprites(Vector2.zero, Vector2.zero);
        }
    }
}