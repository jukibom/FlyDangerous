using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Game_UI {
    public class Target : MonoBehaviour {
        [SerializeField] private Text targetNameText;
        [SerializeField] private Text targetDistanceText;
        private float _targetDistanceMeters;

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

        private void UpdateDistanceText() {
            string addPointZeroIfNeeded(float distance) {
                return distance % 1 == 0 ? distance + ".0" : distance.ToString(CultureInfo.CurrentCulture);
            }

            string text;
            if (_targetDistanceMeters < 850)
                text = Mathf.Round(_targetDistanceMeters) + "m";
            else if (_targetDistanceMeters < 850000)
                text = addPointZeroIfNeeded(Mathf.Round(_targetDistanceMeters / 100) / 10) + "Km";
            else if (_targetDistanceMeters < 29979245.8f)
                text = addPointZeroIfNeeded(Mathf.Round(_targetDistanceMeters / 100000) / 10) + "Mm";
            else
                text = addPointZeroIfNeeded(Mathf.Max(0.1f, Mathf.Round(_targetDistanceMeters / 29980000f) / 10)) + "Ls";

            targetDistanceText.text = text;
        }
    }
}