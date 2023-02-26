using System;
using UnityEngine;

namespace Core.ShipModel.Modifiers {
    public struct AppliedEffects {
        internal Vector3 shipForce; // used to directly influence the force acting on the ship (e.g. attractors)
        internal float shipDeltaSpeedCap; // how much OVER the speed cap the ship can go
        internal float shipDeltaThrust; // how much OVER the thrust cap the ship can output
        internal float shipDeltaDrag; // additional rigid body drag
        internal float shipDeltaAngularDrag; // additional rigid body angular drag
    }

    public class ModifierEngine : MonoBehaviour {
        [SerializeField] private float shipDeltaSpeedCapDamping = 0.99f;
        [SerializeField] private float shipDeltaThrustCapDamping = 0.99f;
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
            _appliedEffects.shipDeltaDrag *= shipDragDamping;
            _appliedEffects.shipDeltaAngularDrag *= shipAngularDragDamping;

            _appliedEffects.shipForce = Vector3.ClampMagnitude(_appliedEffects.shipForce, maxShipForce);
            _appliedEffects.shipDeltaSpeedCap = Mathf.Min(_appliedEffects.shipDeltaSpeedCap, maxShipDeltaSpeed);
            _appliedEffects.shipDeltaThrust = Mathf.Min(_appliedEffects.shipDeltaThrust, maxShipDeltaThrust);
            _appliedEffects.shipDeltaDrag = Mathf.Min(_appliedEffects.shipDeltaDrag, maxShipDrag);
            _appliedEffects.shipDeltaAngularDrag = Mathf.Min(_appliedEffects.shipDeltaAngularDrag, maxShipAngularDrag);
        }

        public void ApplyModifier(Rigidbody shipRigidBody, IModifier modifier) {
            modifier.ApplyModifierEffect(shipRigidBody, ref _appliedEffects);
        }

        public void SetDirect(Vector3 shipForce, float shipDeltaSpeedCap, float shipDeltaThrust, float shipDrag, float shipAngularDrag) {
            _appliedEffects.shipForce = shipForce;
            _appliedEffects.shipDeltaSpeedCap = shipDeltaSpeedCap;
            _appliedEffects.shipDeltaThrust = shipDeltaThrust;
            _appliedEffects.shipDeltaDrag = shipDrag;
            _appliedEffects.shipDeltaAngularDrag = shipAngularDrag;
        }
    }
}