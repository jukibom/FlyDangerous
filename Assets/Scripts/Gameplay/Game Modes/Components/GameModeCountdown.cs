using System;
using System.Collections;
using Audio;
using Misc;
using UnityEngine;

namespace Gameplay.Game_Modes.Components {
    public class GameModeCountdown {
        /**
         * Perform a countdown with sound firing a callback whenever a multiple of 1 second remains (and when it hits 0)
         */
        public IEnumerator CountdownWithSound(float countdownTimeSeconds, Action<float> onSecondsRemaining) {
            var edgeTime = countdownTimeSeconds % 1;
            var remainingSeconds = Mathf.Round(countdownTimeSeconds - edgeTime);

            // handle fractions of seconds (e.g. 2.5 seconds, edge time becomes 0.5)
            if (edgeTime != 0) {
                yield return YieldExtensions.WaitForFixedFrames(YieldExtensions.SecondsToFixedFrames(edgeTime));
                UIAudioManager.Instance.Play("tt-countdown-1");
                onSecondsRemaining(remainingSeconds);
            }

            while (remainingSeconds > 0) {
                yield return YieldExtensions.WaitForFixedFrames(YieldExtensions.SecondsToFixedFrames(1));
                remainingSeconds -= 1;
                if (remainingSeconds > 0)
                    UIAudioManager.Instance.Play("tt-countdown-1");
                else
                    UIAudioManager.Instance.Play("tt-countdown-2");

                onSecondsRemaining(remainingSeconds);
            }
        }
    }
}