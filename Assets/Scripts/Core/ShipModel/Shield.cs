using Misc;
using UnityEngine;

namespace Core.ShipModel {
    public class Shield : MonoBehaviour {
        private static readonly int alpha = Shader.PropertyToID("_Alpha");
        private static readonly int impactCenter = Shader.PropertyToID("_ImpactCenter");
        private static readonly int shieldOffset = Shader.PropertyToID("_ShieldOffset");
        private static readonly int shieldFresnel = Shader.PropertyToID("_FresnelPower");

        [SerializeField] private MeshRenderer shieldMesh;
        [SerializeField] private MeshRenderer shieldImpactMesh;
        [SerializeField] private float minTurbulenceOffset = 0.2f;
        [SerializeField] private float maxTurbulenceOffset = 1.5f;
        [SerializeField] private float maxShieldAlpha = 50f;
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
        }

        private void FixedUpdate() {
            _targetShieldImpactAlpha = Mathf.Lerp(_targetShieldImpactAlpha, 0, 0.05f);
            _targetShieldAlpha = Mathf.Lerp(_targetShieldAlpha, 0, 0.01f);
            _targetTurbulenceOffset = Mathf.Lerp(_targetTurbulenceOffset, 0, 0.01f);
            _targetFresnel = Mathf.Lerp(_targetFresnel, maxShieldFresnel, 0.01f);

            _targetShieldAlpha = Mathf.Clamp(_targetShieldAlpha, 0.2f, maxShieldAlpha);
            _targetShieldImpactAlpha = Mathf.Clamp(_targetShieldImpactAlpha, 0, 1);
            _targetTurbulenceOffset = Mathf.Clamp(_targetTurbulenceOffset, minTurbulenceOffset, maxTurbulenceOffset);
            _targetFresnel = Mathf.Clamp(_targetFresnel, 10, maxShieldFresnel);

            _shieldMaterial.SetFloat(alpha, _targetShieldAlpha);
            _shieldImpactMaterial.SetFloat(alpha, _targetShieldImpactAlpha);
            _shieldMaterial.SetFloat(shieldOffset, _targetTurbulenceOffset);
            _shieldImpactMaterial.SetFloat(shieldOffset, _targetTurbulenceOffset);
            _shieldMaterial.SetFloat(shieldFresnel, _targetFresnel);
            _shieldImpactMaterial.SetVector(impactCenter, new Vector4(_targetDirection.x, _targetDirection.y, _targetDirection.z, 0));
        }

        public void OnImpact(float impactForce, Vector3 impactDirection) {
            _targetShieldImpactAlpha += impactForce;
            _targetShieldAlpha += impactForce * maxShieldAlpha;
            _targetTurbulenceOffset = MathfExtensions.Remap(0, 1, minTurbulenceOffset, maxTurbulenceOffset, impactForce);
            _targetDirection = impactDirection;
            _targetFresnel -= impactForce * maxShieldFresnel;
        }

        public void OnContinuousCollision(Vector3 collisionDirection) {
            _targetDirection = Vector3.Lerp(_targetDirection, collisionDirection, 0.01f);
            _targetShieldImpactAlpha += 0.01f;
            _targetShieldAlpha += 0.01f * maxShieldAlpha;
            _targetTurbulenceOffset += 0.01f;
            _targetFresnel -= 0.01f * maxShieldFresnel;
        }
    }
}