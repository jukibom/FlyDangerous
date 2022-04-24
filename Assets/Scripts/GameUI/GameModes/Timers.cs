using Game_UI;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI.GameModes {
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
            HideTimers(false);
        }

        public void ShowTimers(bool animate = true) {
            gameObject.SetActive(true);
            if (!animate) {
                _canvasGroup.alpha = 1;
                return;
            }

            _canvasGroup.alpha = 0;
            StartCoroutine(YieldExtensions.SimpleAnimationTween(val => _canvasGroup.alpha = val, 0.5f));
        }

        public void HideTimers(bool animate = true) {
            if (!animate) {
                _canvasGroup.alpha = 0;
                return;
            }

            _canvasGroup.alpha = 1;
            StartCoroutine(YieldExtensions.SimpleAnimationTween(val => _canvasGroup.alpha = 1 / val, 0.5f));
        }
    }
}