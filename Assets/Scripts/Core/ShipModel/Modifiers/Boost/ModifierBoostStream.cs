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
            var distance = streamTransform.position - shipRigidBody.transform.position;
            var effectOverDisantceNormalised = 1 - distance.magnitude / (lengthMeters - streamCapsuleEndCapRadius);

            effects.shipForce += Vector3.Lerp(Vector3.zero, streamTransform.forward * shipForceAdd, effectOverDisantceNormalised);

            // apply additional thrust and max speed if the ship vector is facing the correct direction
            if (Vector3.Dot(transform.forward, shipRigidBody.velocity) > 0) {
                effects.shipDeltaSpeedCap += Mathf.Lerp(0, shipSpeedAdd, effectOverDisantceNormalised);
                effects.shipDeltaThrust += Mathf.Lerp(0, shipThrustAdd, effectOverDisantceNormalised);
            }
        }
    }
}