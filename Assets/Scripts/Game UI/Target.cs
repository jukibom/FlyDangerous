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
            string text = "UNKNOWN";
            if (_targetDistanceMeters < 850) {
                text = Mathf.Round(_targetDistanceMeters) + " M";
            }
            else if (_targetDistanceMeters < 850000) {
                text = Mathf.Round(_targetDistanceMeters / 1000) + " kM";
            }
            if (_targetDistanceMeters > 2.998e7f) {
                text = Mathf.Round(_targetDistanceMeters / 2.998e8f) + " lS";
            }

            targetDistanceText.text = text;
        }
    }
}
