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

        public static float Oscillate(float min, float max, float periodSeconds, float offsetSeconds = 0) {
            return Mathf.PingPong(offsetSeconds + Time.time / periodSeconds, 1).Remap(0, 1, min, max);
        }

        // Clamp a quaternion to a given euler bounds 
        // e.g. ClampRotation(q, new Vector3(-30, -50, 0), new Vector3(30, 50, 0) clamps x to forward 60 degree arc, y to 100 degree arc and z to 0
        public static Quaternion ClampRotation(Quaternion q, Vector3 min, Vector3 max) {
            if (q.w == 0) return Quaternion.identity;

            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            var angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);
            angleX = Mathf.Clamp(angleX, min.x, max.x);
            q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

            var angleY = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.y);
            angleY = Mathf.Clamp(angleY, min.y, max.y);
            q.y = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleY);

            var angleZ = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.z);
            angleZ = Mathf.Clamp(angleZ, min.z, max.z);
            q.z = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleZ);

            return q;
        }

        /// <summary>
        /// Framerate-independent smoothing (lerp replacement)
        /// https://www.youtube.com/watch?v=LSNQuFEDOyQ
        ///
        /// Usage:
        ///     float newValue = prevValue.ExpDecay(targetValue, decay, Time.deltaTime)
        /// </summary>
        /// <param name="a">starting or previous frame position</param>
        /// <param name="b">target position</param>
        /// <param name="decay">decay time, useful range approx. 1-25 (slow-fast)</param>
        /// <param name="deltaTime">time per frame, contingent on usage
        ///     (Time.deltaTime in Update(), Time.fixedDeltaTime in FixedUpdate()</param>
        /// <returns>new decayed value</returns>
        public static float ExpDecay(this float a, float b, float decay, float deltaTime) {
            return b + (a - b) * Mathf.Exp(-decay * deltaTime);
        }
        
        public static Vector3 ExpDecay(this Vector3 a, Vector3 b, float decay, float deltaTime) {
            return b + (a - b) * Mathf.Exp(-decay * deltaTime);
        }
        
        public static Color ExpDecay(this Color a, Color b, float decay, float deltaTime) {
            return b + (a - b) * Mathf.Exp(-decay * deltaTime);
        }
    }
}