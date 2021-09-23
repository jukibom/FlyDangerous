using System;
using System.Collections;
using System.Collections.Generic;
using Core.Player;
using Misc;
using UnityEngine;

namespace Core.Ship {
    
    /**
     * Provide some basic ship functionality expected from all mesh objects in an override-able fashion.
     */
    
    public class SimpleShip : MonoBehaviour, IShip {
        
        [SerializeField] private ThrusterController thrusterController;
        [SerializeField] private Light shipLights;
        [SerializeField] private GameObject cockpitInternal;
        [SerializeField] private GameObject cockpitExternal;
        [SerializeField] private List<TrailRenderer> trailRenderers;
        
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
                engineBoostAudioSource.Play();
                externalBoostAudioSource.Play();
                
                yield return new WaitForSeconds(1);
                _shipShake.Shake(boostTime - 1, 0.005f);
                
                externalBoostThrusterAudioSource.Play();
                thrusterController.AnimateBoostThrusters();
                trailRenderers.ForEach(trailRenderer => trailRenderer.emitting = true);
                yield return new WaitForSeconds(boostTime - 1);
                trailRenderers.ForEach(trailRenderer => trailRenderer.emitting = false);
            }

            _boostCoroutine = StartCoroutine(DoBoost());
        }

        #endregion

        #region Rolling Updates

        public virtual void UpdateIndicators(ShipIndicatorData shipIndicatorData) { /* Indicators are entirely model-specific and should be implemented. */ }

        public virtual void UpdateMotionInformation(float velocity, Vector3 force, Vector3 torque) {
            thrusterController.UpdateThrusters(force, torque);
        }

        #endregion

        #region Mesh Quirks

        public void SetCockpitMode(CockpitMode cockpitMode) {
            cockpitInternal.SetActive(cockpitMode == CockpitMode.Internal);
            cockpitExternal.SetActive(cockpitMode == CockpitMode.External);
        }

        #endregion
        
        #region User Preferences

        public virtual void SetPrimaryColor(string htmlColor) {
            if (!ColorUtility.TryParseHtmlString(htmlColor, out var primaryColor)) {
                primaryColor = Color.red;
                Debug.LogWarning("Failed to parse primary html color " + primaryColor);
            }
            primaryColorMeshes.ForEach(mesh => {
                var mat = mesh.material;
                mat.color = primaryColor;
            });
        }
        
        public virtual void SetAccentColor(string htmlColor) {
            if (!ColorUtility.TryParseHtmlString(htmlColor, out var accentColor)) {
                accentColor = Color.black;
                Debug.LogWarning("Failed to parse accent html color " + accentColor);
            }
            accentColorMeshes.ForEach(mesh => {
                var mat = mesh.material;
                mat.color = accentColor;
            });
        }
        
        #endregion

        #region Internal Helper

        private void PauseAudio(bool paused) {
            if (Game.Instance.SessionType == SessionType.Singleplayer) {
                foreach (var audioSource in GetComponentsInChildren<AudioSource>()) {
                    if (paused) audioSource.Pause();
                    else audioSource.UnPause();
                }
            }
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