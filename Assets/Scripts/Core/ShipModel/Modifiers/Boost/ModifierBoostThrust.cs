using Core.Player;
using UnityEngine;

namespace Core.ShipModel.Modifiers.Boost {
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ModifierBoostThrust : MonoBehaviour, IModifier {
        [SerializeField] private float shipForceAdd = 50000000;
        [SerializeField] private float shipSpeedAdd = 150;
        [SerializeField] private float shipThrustAdd = 50000;

        private AudioSource _boostSound;
        private MeshRenderer _meshRenderer;
        private bool _useDistortion;
        private static readonly int includeDistortion = Shader.PropertyToID("_includeDistortion");

        public bool UseDistortion {
            get => _useDistortion;
            set {
                _useDistortion = value;
                MeshRenderer.material.SetInt(includeDistortion, _useDistortion ? 1 : 0);
            }
        }

        private MeshRenderer MeshRenderer {
            get {
                if (_meshRenderer == null)
                    _meshRenderer = GetComponent<MeshRenderer>();
                return _meshRenderer;
            }
        }

        public void Awake() {
            _boostSound = GetComponent<AudioSource>();
        }

        public void ApplyModifierEffect(Rigidbody shipRigidBody, ref AppliedEffects effects) {
            //Gets executed every tick the ship spends inside a modifier.
            if (!_boostSound.isPlaying) _boostSound.Play();

            var parameters = shipRigidBody.gameObject.GetComponent<ShipPlayer>().ShipPhysics.FlightParameters;
            if (parameters.useAltBoosters) {
                var norm =  parameters.mass / ShipParameters.Defaults.mass;
                effects.shipDeltaSpeedCap += parameters.boosterVelocityMultiplier * shipSpeedAdd;
                if (Vector3.Dot(transform.forward, shipRigidBody.transform.forward) > 0) effects.shipDeltaThrust += shipThrustAdd * parameters.boosterThrustMultiplier * norm;
            }
            else {
                effects.shipForce += transform.forward * shipForceAdd;
                effects.shipDeltaSpeedCap += shipSpeedAdd;
                // apply additional thrust if the ship is facing the correct direction
                if (Vector3.Dot(transform.forward, shipRigidBody.transform.forward) > 0) effects.shipDeltaThrust += shipThrustAdd;
            }
        }

        public void ApplyInitialEffect(Rigidbody shipRigidBody, ref AppliedEffects effects) {
            var parameters = shipRigidBody.gameObject.GetComponent<ShipPlayer>().ShipPhysics.FlightParameters;
            if (parameters.useAltBoosters) {
                var targetSpeed = 2500 * parameters.boosterForceMultiplier;
                var bleed = 0.95f;
                var velPara = Mathf.Abs(Vector3.Dot(shipRigidBody.velocity, transform.forward)) * transform.forward;
                var velPerp = shipRigidBody.velocity - velPara;
                var speed_dampling = Mathf.Exp(-(velPara.magnitude / targetSpeed + Mathf.Pow(velPara.magnitude / 6250f, 2)/2));
                effects.shipForce += 28.125f * parameters.mass * (targetSpeed * speed_dampling * transform.forward  - bleed*velPerp);
            }
        }
    }
}