using System;
using Den.Tools;
using Misc;
using UnityEngine;

namespace Core.Ship {
    public class Thruster : MonoBehaviour {

        [SerializeField] private MeshRenderer thrusterRenderer;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Light lightSource;
        [SerializeField] private Color thrustColor;
        [SerializeField] private Color thrustRingColor;

        public bool isLarge;
        
        public float TargetThrust {
            get => targetThrust;
            set => targetThrust = MathfExtensions.Clamp(0, 1, value);
        }
        [Range(0, 1)] [SerializeField] private float targetThrust;
        
        private float _thrust;
        
        private Material _thrusterMaterial;
        private static readonly int thrustProperty = Shader.PropertyToID("_Thrust");
        private static readonly int thrustColorProperty = Shader.PropertyToID("_Base_Color");
        private static readonly int thrustRingColorProperty = Shader.PropertyToID("_Ring_Color");


        private void Start() {
            _thrusterMaterial = thrusterRenderer.material;
            _thrusterMaterial.SetColor(thrustColorProperty, thrustColor);
            _thrusterMaterial.SetColor(thrustRingColorProperty, thrustRingColor);
            lightSource.color = thrustColor * 2;
        }

        private void Update() {

            _thrust = Mathf.Lerp(_thrust, targetThrust, 0.1f);
            
            _thrusterMaterial.SetFloat(thrustProperty, _thrust);

            audioSource.volume = MathfExtensions.Remap(0, 1, 0, 0.2f, _thrust);
            audioSource.pitch = MathfExtensions.Remap(0, 1, 0.8f, 2f, _thrust);
            lightSource.intensity = MathfExtensions.Remap(0, 1, 0, 2, _thrust);
            
            if (isLarge) {
                audioSource.volume *= 2;
                audioSource.pitch /= 3;
            }
            
        }
    }
}