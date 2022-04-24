using System;
using System.Collections;
using UnityEngine;

namespace Misc {
    public static class YieldExtensions {
        public static IEnumerator WaitForFixedFrames(uint frames) {
            var frame = 0;
            while (frames > frame) {
                frame++;
                yield return new WaitForFixedUpdate();
            }
        }

        public static uint SecondsToFixedFrames(float seconds) {
            return Convert.ToUInt32(seconds / Time.fixedDeltaTime);
        }

        // Execute a function every render frame with a value between 0 and 1 of the total animation.
        public static IEnumerator SimpleAnimationTween(Action<float> action, float animationTimeSeconds, Action onComplete = null) {
            var timer = 0f;
            while (timer < animationTimeSeconds) {
                timer += Time.deltaTime;
                action(timer / animationTimeSeconds);
                yield return new WaitForEndOfFrame();
            }

            onComplete?.Invoke();
        }
    }
}