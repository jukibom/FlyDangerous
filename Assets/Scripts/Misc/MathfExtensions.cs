using UnityEngine;

namespace Misc {
    public static class MathfExtensions {
        // Remap a value between two values into a value between two other values.
        // Usage: val.Remap(min, max, outputMin, outputMax)
        public static float Remap(this float v, float iMin, float iMax, float oMin, float oMax) {
            return Mathf.Lerp(oMin, oMax, Mathf.InverseLerp(iMin, iMax, v));
        }

        public static float Remap(this int v, float iMin, float iMax, float oMin, float oMax) {
            return Remap((float)v, iMin, iMax, oMin, oMax);
        }

        public static float Oscillate(float iMin, float iMax, float periodSeconds, float offsetSeconds = 0) {
            return Mathf.PingPong(offsetSeconds + Time.time / periodSeconds, 1).Remap(0, 1, iMin, iMax);
        }
    }
}