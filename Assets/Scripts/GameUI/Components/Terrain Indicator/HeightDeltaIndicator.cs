using System.Collections.Generic;
using Core;
using Misc;
using NaughtyAttributes;
using UnityEngine;

namespace GameUI.Components.Terrain_Indicator {
    [ExecuteAlways]
    public class HeightDeltaIndicator : MonoBehaviour {
        [Range(-1f, 1f)] [OnValueChanged("RefreshIndicator")] [SerializeField]
        private float indicatorValueNormalized;

        [SerializeField] private HeightDeltaIndicatorPip centralPip;
        [SerializeField] private List<HeightDeltaIndicatorPip> negativeValuePips;
        [SerializeField] private List<HeightDeltaIndicatorPip> positiveValuePips;

        public float IndicatorValueNormalized {
            get => indicatorValueNormalized;
            set {
                indicatorValueNormalized = Mathf.Clamp(value, -1, 1);
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
            var negativePipCount = indicatorValueNormalized < 0 ? negativeValuePips.Count * Mathf.Abs(indicatorValueNormalized) : 0;
            var positivePipCount = indicatorValueNormalized > 0 ? positiveValuePips.Count * indicatorValueNormalized : 0;

            for (var i = 0; i < negativeValuePips.Count; i++) negativeValuePips[i].SetPipActive(i <= negativePipCount - 1, i > 7);
            for (var i = 0; i < positiveValuePips.Count; i++) positiveValuePips[i].SetPipActive(i <= positivePipCount - 1);
        }

        private void OnGameSettingsApplied() {
            RefreshColors();
        }

        private void RefreshColors() {
            var uiColor = ColorExtensions.ParseHtmlColor(Preferences.Instance.GetString("playerHUDIndicatorColor"));
            centralPip.SetPrimaryColor(uiColor);
            foreach (var pip in negativeValuePips) pip.SetPrimaryColor(uiColor);
            foreach (var pip in positiveValuePips) pip.SetPrimaryColor(uiColor);
        }
    }
}