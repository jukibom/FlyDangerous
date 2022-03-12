using UnityEngine;

namespace Misc {
    public static class TransformExtensions {
        public static void SetLayer(this Transform trans, int layer) {
            trans.gameObject.layer = layer;
            foreach (Transform child in trans) child.SetLayer(layer);
        }
    }
}