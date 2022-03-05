using System;
using System.Collections;
using System.Collections.Generic;
using Core.Player;
using Misc;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using LightType = UnityEngine.LightType;

namespace Core.Ship {
    
    /**
     * Provide some basic ship functionality expected from all mesh objects in an override-able fashion.
     */
    
    public class SimpleShip : MonoBehaviour, IShip {
        
        [SerializeField] private ThrusterController thrusterController;
        [SerializeField] private Light shipLights;
        [SerializeField] private List<TrailRenderer> trailRenderers;
        [SerializeField] private ThrustTrail thrustTrail;
        
        [SerializeField] private List<MeshRenderer> primaryColorMeshes = new List<MeshRenderer>();
        [SerializeField] private List<MeshRenderer> accentColorMeshes = new List<MeshRenderer>();
        
        [SerializeField] private AudioSource engineBoostAudioSource;
        [SerializeField] private AudioSource externalBoostAudioSource;
        [SerializeField] private AudioSource externalBoostThrusterAudioSource;
        [SerializeField] private AudioSource simpleToggleAudioSource;
        [SerializeField] private AudioSource assistActivateAudioSource;
        [SerializeField] private AudioSource assistDeactivateAudioSource;
        [SerializeField] private AudioSource velocityLimitActivateAudioSource;
        [SerializeField] private AudioSource velocityLimitDeactivateAudioSource;

        private Coroutine _boostCoroutine;
        private ShipShake _shipShake;
        
        public MonoBehaviour Entity() => this;

        public virtual void OnEnable() {
            Game.OnPauseToggle += PauseAudio;
            Game.OnRestart += Restart;
            trailRenderers.ForEach(trailRenderer => trailRenderer.emitting = false);
            _shipShake = new ShipShake(transform);
        }
        
        public virtual void OnDisable() {
            Game.OnPauseToggle -= PauseAudio;
            Game.OnRestart -= Restart;
        }

        public virtual void FixedUpdate() {
            _shipShake.Update();
        }

        #region IShip Basic Functions

        public virtual void SetLights(bool active) {
            simpleToggleAudioSource.Play();
            shipLights.enabled = !shipLights.enabled;
            
            // ensure that the local player ship lights take priority over all others
            var player = GetComponentInParent<ShipPlayer>();
            if (player && player.isLocalPlayer) {
                shipLights.renderMode = LightRenderMode.ForcePixel;
            }
        }
        
        public virtual void SetAssist(bool active) {
            if (active) assistActivateAudioSource.Play();
            else assistDeactivateAudioSource.Play();
        }
        
        public virtual void SetVelocityLimiter(bool active) {
            if (active) velocityLimitActivateAudioSource.Play();
            else velocityLimitDeactivateAudioSource.Play();
        }
        
        public void Boost(float boostTime) {
            IEnumerator DoBoost() {
                // using real, non-scaled time here because this primarily affects the audio and frame time 
                // inconsistencies are really noticeable! 
                yield return new WaitForEndOfFrame();
                yield return new WaitForSecondsRealtime(1);
                yield return new WaitForEndOfFrame();
                _shipShake.Shake(boostTime - 1, 0.005f);
                
                externalBoostThrusterAudioSource.Play();
                thrusterController.AnimateBoostThrusters();
                trailRenderers.ForEach(trailRenderer => trailRenderer.emitting = true);
                yield return new WaitForSeconds(boostTime - 1);
                trailRenderers.ForEach(trailRenderer => trailRenderer.emitting = false);
            }
            
            engineBoostAudioSource.Play();
            externalBoostAudioSource.Play();
            _boostCoroutine = StartCoroutine(DoBoost());
        }

        #endregion

        #region Rolling Updates

        public virtual void UpdateIndicators(ShipIndicatorData shipIndicatorData) { /* Indicators are entirely model-specific and should be implemented. */ }

        public virtual void UpdateMotionInformation(Vector3 velocity, float maxVelocity, Vector3 force, Vector3 torque) {
            thrusterController.UpdateThrusters(force, torque);
            thrustTrail.UpdateThrustTrail(velocity, maxVelocity, force);
        }

        #endregion

        #region Mesh Quirks

        #endregion
        
        #region User Preferences

        public virtual void SetPrimaryColor(string htmlColor) {
            var color = ParseColor(htmlColor);
            primaryColorMeshes.ForEach(mesh => {
                var mat = mesh.material;
                mat.color = color;
            });
        }
        
        public virtual void SetAccentColor(string htmlColor) {
            var color = ParseColor(htmlColor);
            accentColorMeshes.ForEach(mesh => {
                var mat = mesh.material;
                mat.color = color;
            });
        }

        public virtual void SetThrusterColor(string htmlColor) {
            var color = ParseColor(htmlColor);
            foreach (var thruster in GetComponentsInChildren<Thruster>()) {
                thruster.ThrustColor = color;
            }
        }

        /** Set the color of the trails which occur under boost */
        public virtual void SetTrailColor(string htmlColor) {
            var thrusterColor = ParseColor(htmlColor);
            foreach (var thruster in GetComponentsInChildren<TrailRenderer>()) {
                thruster.startColor = thrusterColor;
                thruster.endColor = thrusterColor;
            }
        }

        /** Set the color of the ship head-lights */
        public virtual void SetHeadLightsColor(string htmlColor) {
            var color = ParseColor(htmlColor);
            foreach (var shipLight in GetComponentsInChildren<Light>()) {
                if (shipLight.type == LightType.Spot) {
                    shipLight.color = color;
                }
            }
        }
        
        #endregion

        #region Internal Helper

        protected void PauseAudio(bool paused) {
            if (Game.Instance.SessionType == SessionType.Singleplayer) {
                foreach (var audioSource in GetComponentsInChildren<AudioSource>()) {
                    if (paused) audioSource.Pause();
                    else audioSource.UnPause();
                }
            }
        }

        protected Color ParseColor(string htmlColor) {
            if (!ColorUtility.TryParseHtmlString(htmlColor, out var color)) {
                color = Color.red;
                Debug.LogWarning("Failed to parse html color " + htmlColor);
            }

            return color;
        }

        private void Restart() {
            _shipShake.Reset();
            trailRenderers.ForEach(trail => {
                trail.Clear();
                trail.emitting = false;
            });
            engineBoostAudioSource.Stop();
            externalBoostAudioSource.Stop();
            externalBoostThrusterAudioSource.Stop();
            if (_boostCoroutine != null) {
                StopCoroutine(_boostCoroutine);
            }
        }

        #endregion
    }
}