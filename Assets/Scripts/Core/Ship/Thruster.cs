using System;
using Den.Tools;
using Misc;
using UnityEngine;

namespace Core.Ship {
    public class Thruster : MonoBehaviour {

        [SerializeField] private MeshRenderer thrusterRenderer;
        [SerializeField] private AudioSource audioSource;
        private Material _thrusterMaterial;
        private static readonly int thrustProperty = Shader.PropertyToID("_Thrust");

        [Range(0, 1)] public float thrust;
        public bool isLarge;

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