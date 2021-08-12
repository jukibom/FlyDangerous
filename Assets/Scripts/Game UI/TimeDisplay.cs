using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game_UI {
    public class TimeDisplay : MonoBehaviour {
        public Text textBox;

        public void Start() {
            textBox = GetComponent<Text>();
        }

        public void SetTimeMs(float timeMs) {
            var hours = (int) (timeMs / 3600);
            var minutes = (int) (timeMs / 60) % 60;
            var seconds = (int) timeMs % 60;
            var fraction = (int) (timeMs * 100) % 100;

            var text = hours > 0
                ? String.Format("{0:00}:{1:00}:{2:00}:{3:00}", hours, minutes, seconds, fraction)
                : String.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, fraction);

            textBox.text = text;
        }
    }
}