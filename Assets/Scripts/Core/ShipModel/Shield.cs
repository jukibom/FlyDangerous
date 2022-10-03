using System.Collections.Generic;
using Misc;
using UnityEngine;
using Random = System.Random;

namespace Core.ShipModel {
    public class Shield : MonoBehaviour {
        private static readonly int Alpha = Shader.PropertyToID("_Alpha");
        private static readonly int ImpactCenter = Shader.PropertyToID("_ImpactCenter");
        private static readonly int ShieldOffset = Shader.PropertyToID("_ShieldOffset");
        private static readonly int ShieldFresnel = Shader.PropertyToID("_FresnelPower");

        [SerializeField] private MeshRenderer shieldMesh;
        [SerializeField] private MeshRenderer shieldImpactMesh;
        [SerializeField] private AudioSource shieldActivateAudioSource;
        [SerializeField] private AudioSource collisionAudioSource;
        [SerializeField] private List<AudioClip> collisionAudioClips;
        [SerializeField] private float minTurbulenceOffset = 0.2f;
        [SerializeField] private float maxTurbulenceOffset = 1.5f;
        [SerializeField] private float minShieldAlpha = 0.1f;
        [SerializeField] private float maxShieldAlpha = 3f;
        [SerializeField] private float minImpactShieldAlpha;
        [SerializeField] private float maxImpactShieldAlpha = 3f;
        [SerializeField] private float shieldImpactForceAlphaMultiplier = 3f;
        [SerializeField] private float minShieldFresnel = 5f;
        [SerializeField] private float maxShieldFresnel = 40f;

        private Material _shieldImpactMaterial;
        private Material _shieldMaterial;

        private Vector3 _targetDirection;
        private float _targetFresnel;
        private float _targetShieldAlpha;
        private float _targetShieldImpactAlpha;
        private float _targetTurbulenceOffset;

        private void Awake() {
            _shieldMaterial = shieldMesh.material;
            _shieldImpactMaterial = shieldImpactMesh.material;
            ResetShield();
        }

        private void FixedUpdate() {
            _targetShieldImpactAlpha = Mathf.Lerp(_targetShieldImpactAlpha, 0, 0.05f);
            _targetShieldAlpha = Mathf.Lerp(_targetShieldAlpha, minShieldAlpha, 0.01f);
            _targetTurbulenceOffset = Mathf.Lerp(_targetTurbulenceOffset, minTurbulenceOffset, 0.03f);
            _targetFresnel = Mathf.Lerp(_targetFresnel, maxShieldFresnel, 0.01f);

            _targetShieldAlpha = Mathf.Clamp(_targetShieldAlpha, minShieldAlpha, maxShieldAlpha);
            _targetShieldImpactAlpha = Mathf.Clamp(_targetShieldImpactAlpha, minImpactShieldAlpha, maxImpactShieldAlpha);
            _targetTurbulenceOffset = Mathf.Clamp(_targetTurbulenceOffset, minTurbulenceOffset, maxTurbulenceOffset);
            _targetFresnel = Mathf.Clamp(_targetFresnel, minShieldFresnel, maxShieldFresnel);

            _shieldMaterial.SetFloat(Alpha, _targetShieldAlpha);
            _shieldImpactMaterial.SetFloat(Alpha, _targetShieldImpactAlpha);
            _shieldMaterial.SetFloat(ShieldOffset, _targetTurbulenceOffset);
            _shieldImpactMaterial.SetFloat(ShieldOffset, _targetTurbulenceOffset);
            _shieldMaterial.SetFloat(ShieldFresnel, _targetFresnel);
            _shieldImpactMaterial.SetVector(ImpactCenter, new Vector4(_targetDirection.x, _targetDirection.y, _targetDirection.z, 0));
        }

        private void OnEnable() {
            Game.OnRestart += ResetShield;
        }

        private void OnDisable() {
            Game.OnRestart -= ResetShield;
        }

        private void ResetShield() {
            _targetShieldImpactAlpha = 0;
            _targetShieldAlpha = minShieldAlpha;
            _targetTurbulenceOffset = minTurbulenceOffset;
            _targetFresnel = maxShieldFresnel;
            _targetDirection = Vector3.zero;
            collisionAudioSource.Stop();
            shieldActivateAudioSource.Stop();
        }

        public void OnImpact(float impactForceNormalised, Vector3 impactDirection) {
            _targetShieldImpactAlpha += impactForceNormalised * shieldImpactForceAlphaMultiplier;
            _targetShieldAlpha += impactForceNormalised * maxShieldAlpha;
            _targetTurbulenceOffset += impactForceNormalised.Remap(0, 1, minTurbulenceOffset, maxTurbulenceOffset);
            _targetDirection = impactDirection;
            _targetFresnel -= impactForceNormalised * maxShieldFresnel;

            var random = new Random();
            var pitch = ((float)random.NextDouble()).Remap(0, 1, 0.7f, 1.3f);

            shieldActivateAudioSource.transform.localPosition = _targetDirection;
            shieldActivateAudioSource.pitch = pitch;
            shieldActivateAudioSource.Play();

            collisionAudioSource.transform.localPosition = _targetDirection;
            collisionAudioSource.volume = impactForceNormalised * 2;
            collisionAudioSource.clip = collisionAudioClips[random.Next(collisionAudioClips.Count)];
            collisionAudioSource.pitch = pitch;
            collisionAudioSource.Play();
        }

        public void OnContinuousCollision(Vector3 collisionDirection) {
            _targetDirection = Vector3.Lerp(_targetDirection, collisionDirection, 0.01f);
            shieldActivateAudioSource.transform.localPosition = _targetDirection;
            _targetShieldImpactAlpha += 0.01f * shieldImpactForceAlphaMultiplier;
            _targetShieldAlpha += 0.01f * maxShieldAlpha;
            _targetTurbulenceOffset += 0.01f;
            _targetFresnel -= 0.01f * maxShieldFresnel;
        }

        public void Fizzle() {
            _targetDirection = Vector3.zero;
            _targetShieldImpactAlpha = 1;
            _targetTurbulenceOffset = 0.18f;
        }

        // Specifically for viewing ships in a UI, make the shield more visible at a standstill
        public void SetPresentationMode() {
            minShieldAlpha = 1;
            minImpactShieldAlpha = 0.2f;
            minTurbulenceOffset = 0.2f;
            maxShieldFresnel = 50;
        }
    }
}