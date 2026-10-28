using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI {
    public class InputDisplay : MonoBehaviour {
        [SerializeField] private TMPro.TMP_Text pitch;
        [SerializeField] private TMPro.TMP_Text roll;
        [SerializeField] private TMPro.TMP_Text yaw;
        [SerializeField] private TMPro.TMP_Text throttle;
        [SerializeField] private TMPro.TMP_Text vertical;
        [SerializeField] private TMPro.TMP_Text lateral;

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
            pitch.SetText("{0:0.00}", Mathf.Round(pitchVal * multiplier) / multiplier);
            roll.SetText("{0:0.00}", Mathf.Round(rollVal * multiplier) / multiplier);
            yaw.SetText("{0:0.00}", Mathf.Round(yawVal * multiplier) / multiplier);
            throttle.SetText("{0:0.00}", Mathf.Round(throttleVal * multiplier) / multiplier);
            vertical.SetText("{0:0.00}", Mathf.Round(verticalVal * multiplier) / multiplier);
            lateral.SetText("{0:0.00}", Mathf.Round(lateralVal * multiplier) / multiplier);
        }
    }
}