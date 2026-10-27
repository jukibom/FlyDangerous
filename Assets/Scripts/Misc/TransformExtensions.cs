using UnityEngine;

namespace Misc {
    public static class TransformExtensions {
        public static void SetLayer(this Transform t, int layer) {
            t.gameObject.layer = layer;
            foreach (Transform child in t) child.SetLayer(layer);
        }

        public static Transform RootTransform(this Transform t) {
            return t.parent != null ? RootTransform(t.parent) : t;
        }
    }
}