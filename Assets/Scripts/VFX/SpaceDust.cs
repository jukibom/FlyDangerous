using Core;
using Core.Player;
using Misc;
using UnityEngine;
using UnityEngine.VFX;

namespace VFX {
    public class SpaceDust : MonoBehaviour {
        [SerializeField] private bool forceOn;
        [SerializeField] private Vector3 forceVelocity;
        [SerializeField] private ShipPlayer player;

        private Rigidbody _playerShipRigidbody;
        private Transform _playerShipTransform;
        private VisualEffect _vfx;

        private void Update() {
            if (!forceOn) _vfx.enabled = Preferences.Instance.GetBool("showSpaceDust");

            if (player) {
                _vfx.SetVector3("_playerVelocity",
                    _playerShipTransform.InverseTransformDirection(_playerShipRigidbody.velocity));
                _vfx.SetVector3("_playerVelocity", _playerShipRigidbody.velocity);
                _vfx.SetFloat("_alphaMultiplier", MathfExtensions.Remap(0.1f, 1, 0, 0.4f, player.ShipPhysics.VelocityNormalised));
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
            if (player) {
                _playerShipRigidbody = player.GetComponent<Rigidbody>();
                _playerShipTransform = player.transform;
            }
        }
    }
}