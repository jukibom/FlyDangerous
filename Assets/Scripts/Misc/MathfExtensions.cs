using UnityEngine;

namespace Misc {
    public static class MathfExtensions {
        // remap a value between two values into a value between two other values
        public static float Remap(float iMin, float iMax, float oMin, float oMax, float v) {
            return Mathf.Lerp(oMin, oMax, Mathf.InverseLerp(iMin, iMax, v));
        }
    }
}