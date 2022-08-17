using UnityEngine;

namespace Core.ShipModel {
    public class Shield : MonoBehaviour {

        [SerializeField] private MeshRenderer shieldMesh;
        [SerializeField] private MeshRenderer shieldImpactMesh;
        
        private Material _shieldMaterial;
        private Material _shieldImpactMaterial;
        private static readonly int alpha = Shader.PropertyToID("_Alpha");
        private static readonly int impactCenter = Shader.PropertyToID("_ImpactCenter");

        private float _targetTurbulence;
        private float _targetShieldAlpha;
        private float _targetShieldImpactAlpha;
        private Vector3 _targetDirection;
        
        private void Awake() {
            _shieldMaterial = shieldMesh.material;
            _shieldImpactMaterial = shieldImpactMesh.material;
        }

        public void OnImpact(float impactForce, Vector3 impactDirection) {
            Debug.Log("Impact! " + impactForce + " " + impactDirection);
            _targetShieldImpactAlpha = impactForce;
            _targetDirection = impactDirection;
        }

        public void OnContinuousCollision(Vector3 collisionDirection) {
            _targetDirection = Vector3.Lerp(_targetDirection, collisionDirection, 0.01f);
        }

        private void FixedUpdate() {
            _shieldImpactMaterial.SetFloat(alpha, _targetShieldImpactAlpha);
            _shieldImpactMaterial.SetVector(impactCenter, new Vector4(_targetDirection.x, _targetDirection.y, _targetDirection.z, 0));
        }
    }
}