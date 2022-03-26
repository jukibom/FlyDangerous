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
        [SerializeField] public Color thrustColor;
        [SerializeField] public Color thrustRingColor;

        public bool isLarge;
        [Range(0, 1)] [SerializeField] private float targetThrust;

        private float _thrust;

        private Material _thrusterMaterial;

        public Color ThrustColor {
            get => thrustColor;
            set {
                thrustColor = value;
                lightSource.color = thrustColor;
                if (_thrusterMaterial != null) _thrusterMaterial.SetColor(thrustColorProperty, thrustColor);
            }
        }

        public float TargetThrust {
            get => targetThrust;
            set => targetThrust = Mathf.Clamp(value, 0, 1);
        }


        private void Awake() {
            _thrusterMaterial = thrusterRenderer.material;
            _thrusterMaterial.SetColor(thrustColorProperty, thrustColor);
            _thrusterMaterial.SetColor(thrustRingColorProperty, thrustRingColor);
            lightSource.color = thrustColor * 2;
        }

        private void Update() {
            _thrust = Mathf.Lerp(_thrust, targetThrust, 0.03f);

            _thrusterMaterial.SetFloat(thrustProperty, _thrust);

            audioSource.volume = MathfExtensions.Remap(0, 1, 0, 0.2f, _thrust);
            audioSource.pitch = MathfExtensions.Remap(0, 1, 0.8f, 2f, _thrust);
            lightSource.intensity = MathfExtensions.Remap(0, 1, 0, 2, _thrust);
            if (lensFlare != null) lensFlare.intensity = lightSource.intensity;

            if (isLarge) {
                audioSource.volume *= 2;
                audioSource.pitch /= 3;
            }
        }
    }
}