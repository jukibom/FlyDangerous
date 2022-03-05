using System;
using Misc;
using UnityEngine;
using UnityEngine.VFX;

namespace Core.Ship {

    [RequireComponent(typeof(VisualEffect))]
    [RequireComponent(typeof(VfxFloatingOriginHandler))]
    public class ThrustTrail : MonoBehaviour {

        private VisualEffect _trailEffect;
        private Transform _transform;
        
        [SerializeField] private Vector3 minEjectionSpeed = new Vector3(0, 0, -10);
        [SerializeField] private Vector3 maxEjectionSpeed = new Vector3(0, 0, -30);
        [SerializeField] private int minSpawnRate = 3;
        [SerializeField] private int maxSpawnRate = 150;

        private void OnEnable() {
            _trailEffect = GetComponent<VisualEffect>();
            _transform = transform;
        }

        public void UpdateColor(Color color) {
            _trailEffect.SetVector4("_color", color);
        }

        public void UpdateThrustTrail(Vector3 vesselSpeed, float maxSpeed, Vector3 force) {
            var vesselSpeedLocal = _transform.InverseTransformDirection(vesselSpeed);

            _trailEffect.SetVector3("_startingVelocityMin", vesselSpeedLocal + minEjectionSpeed * Math.Max(0.1f, force.z));
            _trailEffect.SetVector3("_startingVelocityMax", vesselSpeedLocal + maxEjectionSpeed * Math.Max(0.1f, force.z));
            
            // only show with forward thrust and set the spawn rate to the ratio of thrust over max 
            int spawnRate = force.z > 0.001f
                ? Mathf.FloorToInt(
                    MathfExtensions.Remap(
                        0,
                        1,
                        minSpawnRate,
                        maxSpawnRate,
                        force.z
                    )
                ) 
                : minSpawnRate;

            _trailEffect.SetInt("_spawnRate", spawnRate);
            _trailEffect.SetVector3("_startingPositionJitter", new Vector3(0, 0, MathfExtensions.Remap(0, 1, 0, -5, vesselSpeedLocal.z / maxSpeed)));
        }
    }
}
