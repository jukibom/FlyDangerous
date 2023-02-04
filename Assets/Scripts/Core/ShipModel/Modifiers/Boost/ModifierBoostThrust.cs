using UnityEngine;

namespace Core.ShipModel.Modifiers.Boost {
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ModifierBoostThrust : MonoBehaviour, IModifier {
        [SerializeField] private float shipForceAdd = 50000000;
        [SerializeField] private float shipSpeedAdd = 150;
        [SerializeField] private float shipThrustAdd = 50000;

        private AudioSource _boostSound;

        public void Start() {
            _boostSound = GetComponent<AudioSource>();
        }

        public void ApplyModifierEffect(Rigidbody shipRigidBody, ref AppliedEffects effects) {
            if (!_boostSound.isPlaying) _boostSound.Play();

            effects.shipForce += transform.forward * shipForceAdd;
            effects.shipDeltaSpeedCap += shipSpeedAdd;
            // apply additional thrust if the ship is facing the correct direction
            if (Vector3.Dot(transform.forward, shipRigidBody.transform.forward) > 0) effects.shipDeltaThrust += shipThrustAdd;
        }
    }
}