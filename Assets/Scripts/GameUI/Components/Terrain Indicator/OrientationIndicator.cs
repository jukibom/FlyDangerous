using System.Collections.Generic;
using System.Globalization;
using Core;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI.Components.Terrain_Indicator {
    public class OrientationIndicator : MonoBehaviour {
        [Tooltip("The pitch container to rotate towards the floor in a terrain environment")] [SerializeField]
        private Transform pitchIndicator;

        [Tooltip("The entire pitch element container to move to show the correct pitch")] [SerializeField]
        private Transform pitchElements;

        [Tooltip("The entire yaw element container to move to show the correct yaw")] [SerializeField]
        private Transform yawElements;

        [Tooltip("The 180-360 degree elements on the right which we need to flip to make the indicator seem infinite")] [SerializeField]
        private Transform yawElementsRight;

        [Tooltip("Text field for pitch (left side)")] [SerializeField]
        private Text pitchTextLeft;

        [Tooltip("Text field for pitch (right side)")] [SerializeField]
        private Text pitchTextRight;

        [Tooltip("Text field for yaw")] [SerializeField]
        private Text yawText;

        [Tooltip("List of all UI Image elements to NOT override the color with (i.e. mask images)")] [SerializeField]
        private List<Image> excludedUIColorImages;

        private float pitchValueNormalized;
        private float yawValueNormalized;

        public Transform PitchIndicator => pitchIndicator;

        public float PitchValueNormalized {
            get => pitchValueNormalized;
            set {
                pitchValueNormalized = Mathf.Clamp(value, -1, 1);
                RefreshIndicator();
            }
        }

        public float YawValueNormalized {
            get => yawValueNormalized;
            set {
                yawValueNormalized = Mathf.Clamp(value, 0, 1);
                RefreshIndicator();
            }
        }

        private void Start() {
            RefreshColors();
        }

        private void OnEnable() {
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
        }

        private void OnDisable() {
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
        }

        private void RefreshIndicator() {
            var pitchElementsTransform = pitchElements.transform;
            var yawElementsTransform = yawElements.transform;

            var pitchElementsPosition = pitchElementsTransform.localPosition;
            var yawElementsPosition = yawElementsTransform.localPosition;

            var yawXPos = yawValueNormalized.Remap(0, 1, 22.5f, -67.5f);

            var flip = yawValueNormalized <= 0.25f;
            if (yawValueNormalized >= 0.75f) {
                flip = true;
                yawXPos += 90f;
            }

            var flipHalfXPos = flip ? -45f : 45f;

            pitchElementsTransform.localPosition = new Vector3(pitchElementsPosition.x, pitchValueNormalized.Remap(-1, 1, 40, -40), pitchElementsPosition.z);
            yawElements.transform.localPosition = new Vector3(yawXPos, yawElementsPosition.y, yawElementsPosition.z);
            yawElementsRight.transform.localPosition = new Vector3(flipHalfXPos, 0, 0);

            // text labels
            var pitchText = Mathf.Round(pitchValueNormalized * 90).ToString(CultureInfo.InvariantCulture);
            pitchTextLeft.text = pitchText;
            pitchTextRight.text = pitchText;

            var yawDegreesText = Mathf.Round(yawValueNormalized * 360).ToString(CultureInfo.InvariantCulture);
            // make damn sure we never round to 360 because the indicator only ever shows 359 then 0
            yawText.text = yawDegreesText.Equals("360") ? "0" : yawDegreesText;
        }

        private void OnGameSettingsApplied() {
            RefreshColors();
        }

        private void RefreshColors() {
            var uiColor = ColorExtensions.ParseHtmlColor(Preferences.Instance.GetString("playerHUDIndicatorColor"));
            foreach (var image in GetComponentsInChildren<Image>(true))
                if (!excludedUIColorImages.Contains(image)) {
                    // respect alpha
                    var alpha = image.color.a;
                    var color = new Color(uiColor.r, uiColor.g, uiColor.b, alpha);
                    image.color = color;
                }

            foreach (var text in GetComponentsInChildren<Text>(true)) text.color = uiColor;
        }
    }
}