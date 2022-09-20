using System;
using Core;
using Misc;
using UnityEngine;
using UnityEngine.VFX;

namespace VFX {
    [RequireComponent(typeof(VisualEffect))]
    [RequireComponent(typeof(VfxFloatingOriginHandler))]
    public class SmokeEmitter : MonoBehaviour {
        [SerializeField] private Vector3 minEjectionSpeed = new(0, 0, -10);
        [SerializeField] private Vector3 maxEjectionSpeed = new(0, 0, -30);
        [SerializeField] private int minSpawnRate = 3;
        [SerializeField] private int maxSpawnRate = 150;
        [SerializeField] private int minLifetime = 2;
        [SerializeField] private int maxLifetime = 25;

        // fix problem with async updating with multiplayer initialisation (client can fail to update trails and disconnect with null exception)
        private bool _ready;

        private VisualEffect _trailEffect;
        private Transform _transform;

        private void Awake() {
            _trailEffect = GetComponent<VisualEffect>();
            _transform = transform;
            _ready = true;
        }

        private void OnEnable() {
            Game.OnRestart += OnReset;
        }

        private void OnDisable() {
            Game.OnRestart -= OnReset;
        }

        public void UpdateColor(Color color) {
            _trailEffect.SetVector4("_color", color);
        }

        public void UpdateThrustTrail(Vector3 vesselSpeed, float maxSpeed, Vector3 forceNormalised) {
            if (_ready) {
                var vesselSpeedLocal = _transform.InverseTransformDirection(vesselSpeed);

                _trailEffect.SetVector3("_startingVelocityMin", vesselSpeedLocal + minEjectionSpeed * Math.Max(0.15f, forceNormalised.z));
                _trailEffect.SetVector3("_startingVelocityMax", vesselSpeedLocal + maxEjectionSpeed * Math.Max(0.2f, forceNormalised.z));

                // only show with forward thrust and set the spawn rate to the ratio of thrust over max 
                var spawnRate = forceNormalised.z > 0.001f
                    ? forceNormalised.z.Remap(0, 1, minSpawnRate, maxSpawnRate)
                    // set minimum spawn rate as a factor of velocity 
                    : vesselSpeedLocal.z.Remap(0, maxSpeed, minSpawnRate, maxSpawnRate / 2f);

                _trailEffect.SetInt("_spawnRate", Mathf.FloorToInt(spawnRate));

                // these get quite large quite quickly so lets cap their life dependent on speed
                // we want them to be very visible at distance and at fast speeds and die quickly when stationary or slow
                var lifetime = forceNormalised.z.Remap(0, 1, minLifetime, maxLifetime);

                _trailEffect.SetInt("_lifetimeMin", Mathf.FloorToInt(lifetime));
                _trailEffect.SetInt("_lifetimeMax", Mathf.FloorToInt(lifetime + 5 * forceNormalised.z));

                var vesselSpeedNormalised = vesselSpeedLocal.z / maxSpeed;

                // set jitter to a factor of the forward local velocity 
                _trailEffect.SetVector3("_startingPositionJitter", new Vector3(0, 0, vesselSpeedNormalised.Remap(0, 1, 0, -5)));
            }
        }

        private void OnReset() {
            _trailEffect.Reinit();
        }
    }
}