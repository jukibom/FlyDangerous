using System.Collections.Generic;
using System.Globalization;
using Core;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI.Components.Terrain_Indicator {
    public class OrientationIndicator : MonoBehaviour {
        [SerializeField] private Transform pitchIndicator;
        [SerializeField] private Transform pitchElements;
        [SerializeField] private Text pitchTextLeft;
        [SerializeField] private Text pitchTextRight;

        [SerializeField] private List<Image> excludedUIColorImages;

        private float pitchValueNormalized;

        public Transform PitchIndicator => pitchIndicator;

        public float PitchValueNormalized {
            get => pitchValueNormalized;
            set {
                pitchValueNormalized = Mathf.Clamp(value, -1, 1);
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
            var pitchElementsPosition = pitchElementsTransform.localPosition;
            pitchElementsTransform.localPosition = new Vector3(pitchElementsPosition.x, pitchValueNormalized.Remap(-1, 1, 40, -40), pitchElementsPosition.z);
            var pitchText = Mathf.Round(pitchValueNormalized * 90).ToString(CultureInfo.InvariantCulture);
            pitchTextLeft.text = pitchText;
            pitchTextRight.text = pitchText;
        }

        private void OnGameSettingsApplied() {
            RefreshColors();
        }

        private void RefreshColors() {
            var uiColor = ColorExtensions.ParseHtmlColor(Preferences.Instance.GetString("playerHUDIndicatorColor"));
            foreach (var image in GetComponentsInChildren<Image>(true))
                if (!excludedUIColorImages.Contains(image))
                    image.color = uiColor;
            foreach (var text in GetComponentsInChildren<Text>(true)) text.color = uiColor;
        }
    }
}