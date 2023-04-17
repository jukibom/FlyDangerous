using Misc;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameUI.Components.Terrain_Indicator {
    public class HeightMarker : MonoBehaviour {
        [SerializeField] private float position2500m;
        [SerializeField] private float position2000m;
        [SerializeField] private float positionFloor;
        [SerializeField] private Text heightLabel;
        [SerializeField] private bool allowNegatives;

        [SerializeField] private Text label2km;
        [SerializeField] private Text labelBottom;

        private Vector3 label2kmPositivePosition;
        private Vector3 labelBottomPositivePosition;

        [FormerlySerializedAs("indicatorValueNormalized")] [Range(0f, 3000f)] [OnValueChanged("RefreshIndicator")] [SerializeField]
        private float heightAboveSurface;

        private void OnEnable() {
            label2kmPositivePosition = label2km.transform.localPosition;
            labelBottomPositivePosition = labelBottom.transform.localPosition;
        }

        public float HeightAboveSurface {
            get => heightAboveSurface;
            set {
                heightAboveSurface = allowNegatives ? value : Mathf.Max(0, value);
                RefreshIndicator();
            }
        }

        private void RefreshIndicator() {
            var currentPosition = transform.localPosition;
            var indicatorPosition = allowNegatives && heightAboveSurface < 0
                ? heightAboveSurface.Remap(-2000, 0, positionFloor, position2000m)
                : heightAboveSurface.Remap(0, 2500, positionFloor, position2500m);

            transform.localPosition = new Vector3(currentPosition.x, indicatorPosition, currentPosition.z);
            heightLabel.text = heightAboveSurface < 1000
                ? Mathf.Round(heightAboveSurface) + "m"
                : Mathf.Round(heightAboveSurface / 100) / 10 + "km";

            // swap labels when under 0
            if (allowNegatives && Application.IsPlaying(this)) {
                label2km.transform.localPosition = heightAboveSurface >= 0 ? label2kmPositivePosition : labelBottomPositivePosition;
                labelBottom.transform.localPosition = heightAboveSurface >= 0 ? labelBottomPositivePosition : label2kmPositivePosition;
            }
        }
    }
}