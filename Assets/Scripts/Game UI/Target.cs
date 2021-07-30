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
            string text;
            if (_targetDistanceMeters < 850) {
                text = Mathf.Round(_targetDistanceMeters) + " M";
            }
            else if (_targetDistanceMeters < 850000) {
                text = Mathf.Round(_targetDistanceMeters / 100) / 10 + " kM";
            }
            else {
                text = Mathf.Max(0.1f, Mathf.Round(_targetDistanceMeters / 29980000f) / 10) + " lS";
            }

            targetDistanceText.text = text;
        }
    }
}
