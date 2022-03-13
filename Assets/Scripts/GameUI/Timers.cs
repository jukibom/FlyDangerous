using System;
using Game_UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI {
    [RequireComponent(typeof(CanvasGroup))]
    public class Timers : MonoBehaviour {
        [SerializeField] private TimeDisplay totalTimeDisplay;
        [SerializeField] private TimeDisplay splitTimeDisplay;
        [SerializeField] private TimeDisplay splitTimeDeltaDisplay;
        [SerializeField] private TimeDisplay targetTimeDisplay;
        [SerializeField] private Text targetTimeTypeDisplay;

        private CanvasGroup _canvasGroup;

        public TimeDisplay TotalTimeDisplay => totalTimeDisplay;
        public TimeDisplay SplitTimeDisplay => splitTimeDisplay;
        public TimeDisplay SplitTimeDeltaDisplay => splitTimeDeltaDisplay;
        public TimeDisplay TargetTimeDisplay => targetTimeDisplay;
        public Text TargetTimeTypeDisplay => targetTimeTypeDisplay;

        private void OnEnable() {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public void ShowTimers(bool animate = true) {
            // TODO: shiny blendy magic yay
            _canvasGroup.alpha = 1;
        }

        public void HideTimers(bool animate = true) {
            _canvasGroup.alpha = 0;
        }
    }
}
