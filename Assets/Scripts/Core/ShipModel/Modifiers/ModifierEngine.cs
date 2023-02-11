using System;
using UnityEngine;

namespace Core.ShipModel.Modifiers {
    public struct AppliedEffects {
        internal Vector3 shipForce;
        internal float shipDeltaSpeedCap;
        internal float shipDeltaThrust;
        internal float shipDrag;
        internal float shipAngularDrag;
    }

    public class ModifierEngine : MonoBehaviour {
        [SerializeField] private float shipDeltaSpeedCapDamping = 0.99f;
        [SerializeField] private float shipDeltaThrustCapDamping = 0.993f;
        [SerializeField] private float shipForceDamping = 0.8f;
        [SerializeField] private float shipDragDamping = 0.95f;
        [SerializeField] private float shipAngularDragDamping = 0.95f;

        [SerializeField] private float maxShipForce = 50000000;
        [SerializeField] private float maxShipDeltaSpeed = 5000;
        [SerializeField] private float maxShipDeltaThrust = 1000000;
        [SerializeField] private float maxShipDrag = 10;
        [SerializeField] private float maxShipAngularDrag = 10;

        private AppliedEffects _appliedEffects;
        private Func<bool> _isBoosting = () => false;

        public ref AppliedEffects AppliedEffects => ref _appliedEffects;

        public void Initialize(Func<bool> isShipBoosting) {
            _isBoosting = isShipBoosting;
        }

        public void Reset() {
            _appliedEffects = new AppliedEffects();
        }

        public void FixedUpdate() {
            if (!Game.Instance.GameModeHandler.HasStarted) return;

            // if boosting, reduce the damping of max speed (e.g. 0.99 becomes 0.995)
            var currentSpeedCapDamping = _isBoosting() ? Mathf.Sqrt(shipDeltaSpeedCapDamping) : shipDeltaSpeedCapDamping;

            _appliedEffects.shipDeltaSpeedCap *= currentSpeedCapDamping;
            _appliedEffects.shipForce *= shipForceDamping;
            _appliedEffects.shipDeltaThrust *= shipDeltaThrustCapDamping;
            _appliedEffects.shipDrag *= shipDragDamping;
            _appliedEffects.shipAngularDrag *= shipAngularDragDamping;

            _appliedEffects.shipForce = Vector3.ClampMagnitude(_appliedEffects.shipForce, maxShipForce);
            _appliedEffects.shipDeltaSpeedCap = Mathf.Min(_appliedEffects.shipDeltaSpeedCap, maxShipDeltaSpeed);
            _appliedEffects.shipDeltaThrust = Mathf.Min(_appliedEffects.shipDeltaThrust, maxShipDeltaThrust);
            _appliedEffects.shipDrag = Mathf.Min(_appliedEffects.shipDrag, maxShipDrag);
            _appliedEffects.shipAngularDrag = Mathf.Min(_appliedEffects.shipAngularDrag, maxShipAngularDrag);
        }

        public void ApplyModifier(Rigidbody shipRigidBody, IModifier modifier) {
            modifier.ApplyModifierEffect(shipRigidBody, ref _appliedEffects);
        }

        public void SetDirect(Vector3 shipForce, float shipDeltaSpeedCap, float shipDeltaThrust, float shipDrag, float shipAngularDrag) {
            _appliedEffects.shipForce = shipForce;
            _appliedEffects.shipDeltaSpeedCap = shipDeltaSpeedCap;
            _appliedEffects.shipDeltaThrust = shipDeltaThrust;
            _appliedEffects.shipDrag = shipDrag;
            _appliedEffects.shipAngularDrag = shipAngularDrag;
        }
    }
}