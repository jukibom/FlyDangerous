using System;
using System.Collections;
using System.Collections.Generic;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Game_UI {
    public class TimeDisplay : MonoBehaviour {
        public Text textBox;

        public void Start() {
            textBox = GetComponent<Text>();
        }

        public void SetTimeSeconds(float time, bool showPositiveSymbol = false) {
            textBox.text = TimeExtensions.TimeSecondsToString(time);
            if (showPositiveSymbol && time > 0) {
                textBox.text = $"+ {textBox.text}";
            }
        }
    }
}