using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Game_UI {
    [RequireComponent(typeof(Text))]
    public class TimeDisplay : MonoBehaviour {
        private Text _textBox;

        public Text TextBox => _textBox;
        
        public void Awake() {
            _textBox = GetComponent<Text>();
        }

        public void SetTimeSeconds(float time, bool showPositiveSymbol = false) {
            _textBox.text = TimeExtensions.TimeSecondsToString(time);
            if (showPositiveSymbol && time > 0) _textBox.text = $"+ {_textBox.text}";
        }
    }
}