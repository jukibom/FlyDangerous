using Misc;
using UnityEngine;
using UnityEngine.Rendering;

namespace Core.ShipModel {
    public class Thruster : MonoBehaviour {
        private static readonly int thrustProperty = Shader.PropertyToID("_Thrust");
        private static readonly int thrustColorProperty = Shader.PropertyToID("_Base_Color");
        private static readonly int thrustRingColorProperty = Shader.PropertyToID("_Ring_Color");

        [SerializeField] private MeshRenderer thrusterRenderer;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Light lightSource;
        [SerializeField] private LensFlareComponentSRP lensFlare;

        [SerializeField] [ColorUsage(true, true)]
        public Color thrustColor;

        [SerializeField] private float thrustColorIntensity = 1;
        [SerializeField] private Color thrustColorBuffer = new(0.1f, 0.1f, 0.1f);
        [SerializeField] public Color thrustRingColor;

        [SerializeField] private bool isLarge;
        [SerializeField] private bool isPreview;
        [Range(0, 1)] [SerializeField] private float targetThrust;

        private float _thrust;

        private Material _thrusterMaterial;

        public GameObject ThrusterMesh => thrusterRenderer.gameObject;

        public Color ThrustColor {
            get => thrustColor;
            set {
                thrustColor = value;
                UpdateMaterials();
            }
        }

        public float TargetThrust {
            get => targetThrust;
            set => targetThrust = Mathf.Clamp(value, 0, 1);
        }

        private void Awake() {
            _thrusterMaterial = thrusterRenderer.material;
        }

        private void Update() {
            _thrust = Mathf.Lerp(_thrust, targetThrust, 0.03f);

            _thrusterMaterial.SetFloat(thrustProperty, _thrust);

            audioSource.volume = MathfExtensions.Remap(0, 1, 0, 0.2f, _thrust);
            audioSource.pitch = MathfExtensions.Remap(0, 1, 0.8f, 2f, _thrust);
            lightSource.intensity = MathfExtensions.Remap(0, 1, 0, 3, _thrust * thrusterRenderer.transform.localScale.z);
            if (lensFlare != null) lensFlare.intensity = lightSource.intensity;

            if (isLarge) {
                audioSource.volume *= 2;
                audioSource.pitch /= 3;
            }
        }

        private void OnEnable() {
            UpdateMaterials();
        }

        private void UpdateMaterials() {
            lightSource.color = ThrustColor;
            // use base level (no black thrusters) and intensity if it's not a preview (on the profile menu)
            var color = (ThrustColor + thrustColorBuffer) * (isPreview ? 1 : Mathf.Pow(2, thrustColorIntensity));
            if (_thrusterMaterial != null) {
                _thrusterMaterial.SetColor(thrustColorProperty, color);
                _thrusterMaterial.SetColor(thrustRingColorProperty, thrustRingColor);
            }
        }
    }
}