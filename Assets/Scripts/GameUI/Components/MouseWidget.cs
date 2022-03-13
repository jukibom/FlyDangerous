using System;
using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game_UI {
    public class MouseWidget : MonoBehaviour {
        public GameObject crosshair;
        public GameObject arrow;

        public Vector2 mousePositionNormalised = Vector2.zero;

        public float maxDistanceUnits = 1f;
        private Image _arrowImage;

        private Image _crosshairImage;

        // Start is called before the first frame update
        private void Start() {
            _crosshairImage = crosshair.GetComponent<Image>();
            _arrowImage = arrow.GetComponent<Image>();
        }

        // Update is called once per frame
        private void Update() {
            // pref determines draw active
            var shouldShow = Preferences.Instance.GetBool("showMouseWidget");
            crosshair.SetActive(shouldShow);
            arrow.SetActive(shouldShow);

            // position
            arrow.transform.localPosition = Vector3.ClampMagnitude(new Vector3(
                mousePositionNormalised.x * maxDistanceUnits,
                mousePositionNormalised.y * maxDistanceUnits,
                0
            ), maxDistanceUnits);

            // rotation
            if (mousePositionNormalised != Vector2.zero) {
                var dir = arrow.transform.localPosition;
                var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                arrow.transform.localRotation = Quaternion.AngleAxis(angle - 90, Vector3.forward);
            }

            var arrowImageColor = _arrowImage.color;
            var crosshairImageColor = _crosshairImage.color;
            var normalisedMagnitude = arrow.transform.localPosition.magnitude / maxDistanceUnits;
            arrowImageColor.a = normalisedMagnitude;
            crosshairImageColor.a = Mathf.Pow(1f - normalisedMagnitude, 2);

            _crosshairImage.transform.localScale = Vector3.one * (2 * Math.Min(0.4f, normalisedMagnitude) + 1);
            _arrowImage.transform.localScale = Vector3.one * Mathf.Min(1, normalisedMagnitude + 0.5f);

            _arrowImage.color = arrowImageColor;
            _crosshairImage.color = crosshairImageColor;
        }

        public void UpdateWidgetSprites(Vector2 mousePosNormalised) {
            mousePositionNormalised = mousePosNormalised;
        }

        public void ResetToCentre() {
            UpdateWidgetSprites(new Vector2(0, 0));
        }
    }
}