using Core;
using Core.Player;
using Core.ShipModel;
using Misc;
using UnityEngine;
using UnityEngine.VFX;

namespace VFX {
    public class SpaceDust : MonoBehaviour {
        [SerializeField] private bool forceOn;
        [SerializeField] private Vector3 forceVelocity;
        
        private VisualEffect _vfx;
        
        public ShipPhysics ActiveShipPhysics { get; set; }

        private void Update() {
            if (!forceOn) _vfx.enabled = Preferences.Instance.GetBool("showSpaceDust");

            if (ActiveShipPhysics) {
                _vfx.SetVector3("_playerVelocity",
                    ActiveShipPhysics.transform.InverseTransformDirection(ActiveShipPhysics.Rigidbody.velocity));
                _vfx.SetVector3("_playerVelocity", ActiveShipPhysics.Rigidbody.velocity);
                _vfx.SetFloat("_alphaMultiplier", ActiveShipPhysics.VelocityNormalised.Remap(0.1f, 1, 0, 0.4f));
            }

            if (forceOn && forceVelocity != Vector3.zero) {
                _vfx.SetVector3("_playerVelocity", forceVelocity);
                _vfx.SetFloat("_alphaMultiplier", 1);
            }

            // lock the transform in world space so we don't rotate with the ship
            transform.rotation = Quaternion.identity;
        }

        private void OnEnable() {
            _vfx = GetComponent<VisualEffect>();
        }
    }
}