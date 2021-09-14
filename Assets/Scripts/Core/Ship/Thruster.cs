using System;
using Den.Tools;
using Misc;
using UnityEngine;

namespace Core.Ship {
    public class Thruster : MonoBehaviour {

        [SerializeField] private MeshRenderer thrusterRenderer;
        [SerializeField] private AudioSource audioSource;
        
        public bool isLarge;
        
        public float Thrust {
            get => thrust;
            set => thrust = MathfExtensions.Clamp(0, 1, value);
        }
        [Range(0, 1)] [SerializeField] private float thrust;
        
        private Material _thrusterMaterial;
        private static readonly int thrustProperty = Shader.PropertyToID("_Thrust");


        private void Start() {
            _thrusterMaterial = thrusterRenderer.material;
        }

        private void Update() {
            _thrusterMaterial.SetFloat(thrustProperty, thrust);
            audioSource.volume = MathfExtensions.Remap(0, 1, 0, 0.2f, thrust);
            audioSource.pitch = MathfExtensions.Remap(0, 1, 0.8f, 2f, thrust);

            if (isLarge) {
                audioSource.volume *= 2;
                audioSource.pitch /= 2;
            }
            
        }
    }
}