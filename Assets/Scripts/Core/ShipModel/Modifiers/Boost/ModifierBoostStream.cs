using UnityEngine;
using UnityEngine.VFX;

namespace Core.ShipModel.Modifiers.Boost {
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(VisualEffect))]
    public class ModifierBoostStream : MonoBehaviour, IModifier {
        [SerializeField] private float shipForceAdd = 200000;
        [SerializeField] private float shipSpeedAdd = 15;
        [SerializeField] private float shipThrustAdd = 5000;

        [SerializeField] private float lengthMeters = 15000;
        [SerializeField] private float streamCapsuleEndCapRadius = 500;

        private CapsuleCollider _capsuleCollider;
        private VisualEffect _visualEffect;

        public float TrailLengthMeters {
            get => lengthMeters;
            set {
                var scale = transform.lossyScale.z;
                var capsuleCollider = GetComponent<CapsuleCollider>();
                var vfx = GetComponent<VisualEffect>();

                lengthMeters = value;
                var lengthLocal = lengthMeters / scale;

                capsuleCollider.height = lengthLocal;
                capsuleCollider.center = new Vector3(0, 0, lengthLocal / 2 - capsuleCollider.radius);
                vfx.SetFloat("_streamLength", lengthLocal);
            }
        }

        public void ApplyModifierEffect(Rigidbody shipRigidBody, ref AppliedEffects effects) {
            var streamTransform = transform;

            // apply force along the funnel with force linear equivalent to the distance from the source
            var streamPosition = streamTransform.position;
            var shipPosition = shipRigidBody.transform.position;
            var distance = streamPosition - shipPosition;
            var effectOverDistanceNormalised = 1 - distance.magnitude / (lengthMeters - streamCapsuleEndCapRadius);
            effects.shipForce += Vector3.Lerp(Vector3.zero, streamTransform.forward * shipForceAdd, effectOverDistanceNormalised);

            // apply force along the x / y pulling the ship into the centre
            var directionToCenter = new Vector3(streamPosition.x, streamPosition.y, 0) -
                                    new Vector3(shipPosition.x, shipPosition.y, 0);
            effects.shipForce += Vector3.Lerp(Vector3.zero, directionToCenter.normalized * shipForceAdd,
                effectOverDistanceNormalised * directionToCenter.normalized.magnitude);

            // apply additional thrust and max speed if the ship vector is facing the correct direction
            if (Vector3.Dot(transform.forward, shipRigidBody.velocity) > 0) {
                effects.shipDeltaSpeedCap += Mathf.Lerp(0, shipSpeedAdd, effectOverDistanceNormalised);
                effects.shipDeltaThrust += Mathf.Lerp(0, shipThrustAdd, effectOverDistanceNormalised);
            }
        }
    }
}