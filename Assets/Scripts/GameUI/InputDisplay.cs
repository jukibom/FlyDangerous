using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI {
    public class InputDisplay : MonoBehaviour {
        [SerializeField] private Text pitch;
        [SerializeField] private Text roll;
        [SerializeField] private Text yaw;
        [SerializeField] private Text throttle;
        [SerializeField] private Text vertical;
        [SerializeField] private Text lateral;

        public void UpdateAxisDisplay(
            float pitchVal,
            float rollVal,
            float yawVal,
            float throttleVal,
            float verticalVal,
            float lateralVal,
            bool useDecimals
        ) {
            var multiplier = useDecimals ? 100 : 1;
            pitch.text = (Mathf.Round(pitchVal * multiplier) / multiplier).ToString(CultureInfo.CurrentCulture);
            roll.text = (Mathf.Round(rollVal * multiplier) / multiplier).ToString(CultureInfo.CurrentCulture);
            yaw.text = (Mathf.Round(yawVal * multiplier) / multiplier).ToString(CultureInfo.CurrentCulture);
            throttle.text = (Mathf.Round(throttleVal * multiplier) / multiplier).ToString(CultureInfo.CurrentCulture);
            vertical.text = (Mathf.Round(verticalVal * multiplier) / multiplier).ToString(CultureInfo.CurrentCulture);
            lateral.text = (Mathf.Round(lateralVal * multiplier) / multiplier).ToString(CultureInfo.CurrentCulture);
        }
    }
}