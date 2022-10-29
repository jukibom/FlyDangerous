using UnityEngine;

namespace Core.ShipModel.Modifiers.Boost {
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ModifierBoostThrust : MonoBehaviour, IModifier {
        private static readonly int RenderScale = Shader.PropertyToID("_renderScale");
        [SerializeField] private float shipForceAdd = 50000000;
        [SerializeField] private float shipSpeedAdd = 150;
        [SerializeField] private float shipThrustAdd = 50000;

        private AudioSource _boostSound;

        private MeshRenderer _meshRenderer;

        private void Awake() {
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        public void Start() {
            _boostSound = GetComponent<AudioSource>();
        }

        private void OnEnable() {
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
        }

        private void OnDisable() {
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
        }

        public void ApplyModifierEffect(Rigidbody shipRigidBody, ref AppliedEffects effects) {
            if (!_boostSound.isPlaying) _boostSound.Play();

            effects.shipForce += transform.forward * shipForceAdd;
            effects.shipDeltaSpeedCap += shipSpeedAdd;
            // apply additional thrust if the ship is facing the correct direction
            if (Vector3.Dot(transform.forward, shipRigidBody.transform.forward) > 0) effects.shipDeltaThrust += shipThrustAdd;
        }

        private void OnGameSettingsApplied() {
            _meshRenderer.material.SetFloat(RenderScale, Mathf.Clamp(Preferences.Instance.GetFloat("graphics-render-scale"), 0.5f, 2));
        }
    }
}