namespace Misc {
    public static class VectorExtensions {
        public static System.Numerics.Vector3 ToNumerics(this UnityEngine.Vector3 v) {
            return new System.Numerics.Vector3(v.x, v.y, v.z);
        }
    }
}