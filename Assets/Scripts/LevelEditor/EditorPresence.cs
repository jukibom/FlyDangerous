using Core;
using UnityEngine;

namespace FlyDangerous.LevelEditor {
    /// <summary>
    /// The author's body in the world: the thing the camera follows and the floating origin centres on.
    /// Must live ROOT-LEVEL for floating origin.
    /// Subscribes to the origin correction so its world-space position is preserved while it is NOT the focal 
    /// (the same idiom as ShipPlayer.OnFloatingOriginCorrection).
    /// </summary>
    [DisallowMultipleComponent]
    public class EditorPresence : MonoBehaviour {
        // Real-world (absolute) position, independent of the floating origin.
        public Vector3 AbsolutePosition =>
            FloatingOrigin.Instance.GetAbsoluteWorldPosition(transform);

        /// <summary>Teleport to an absolute (real-world) position; the floating origin catches up.</summary>
        public void Goto(Vector3 absolutePosition) {
            FloatingOrigin.Instance.SetAbsoluteWorldPosition(transform, absolutePosition);
        }
    }
}
