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
    }
}