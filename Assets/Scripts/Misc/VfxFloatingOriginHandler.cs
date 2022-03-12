using Core;
using UnityEngine;
using UnityEngine.VFX;

namespace Misc {
    /**
     * Subscribes to Floating Origin and, on correction, attempts to set the `_worldOffset` attribute to the
     * inverse Vec3 value for a single frame. Accompanying VFX should add this attribute to the particle position
     * every frame (which most of the time will be `Vector3.Zero`)
     */
    [RequireComponent(typeof(VisualEffect))]
    public class VfxFloatingOriginHandler : MonoBehaviour {
        private bool _unsetAttribute;

        private bool _unsetAttributeOnNextUpdate;

        private VisualEffect _vfx;

        /**
         * This is completely fucking bonkers but it's the ONLY way I've found to set a value in the VFX graph for
         * only one frame. What this does is allow the frame to play out and, if we just set the attribute, set a
         * flag to check on the frame after that in order to unset it again.
         * Yes this is shit.
         * Yes I hate it.
         * It works. LEAVE IT ALONE.
         */
        private void LateUpdate() {
            if (_unsetAttributeOnNextUpdate) {
                _vfx.SetVector3("_worldOffset", Vector3.zero);
                _unsetAttribute = false;
                _unsetAttributeOnNextUpdate = false;
            }

            if (_unsetAttribute) _unsetAttributeOnNextUpdate = true;
        }

        private void OnEnable() {
            _vfx = GetComponent<VisualEffect>();
            FloatingOrigin.OnFloatingOriginCorrection += UpdatePosition;
        }

        private void OnDisable() {
            FloatingOrigin.OnFloatingOriginCorrection -= UpdatePosition;
        }

        private void UpdatePosition(Vector3 position) {
            _vfx.SetVector3("_worldOffset", -position);
            _unsetAttribute = true;
        }
    }
}