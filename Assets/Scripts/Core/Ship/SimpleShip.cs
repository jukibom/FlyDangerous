using System;
using System.Collections;
using Core.Player;
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
        [SerializeField] private AudioSource engineBoostAudioSource;
        [SerializeField] private AudioSource externalBoostAudioSource;
        
        [SerializeField] private AudioSource simpleToggle;
        [SerializeField] private AudioSource assistActivate;
        [SerializeField] private AudioSource assistDeactivate;
        [SerializeField] private AudioSource velocityLimitActivate;
        [SerializeField] private AudioSource velocityLimitDeactivate;

        public MonoBehaviour Entity() => this;

        public void OnEnable() {
            Game.OnPauseToggle += PauseAudio;
        }

        #region IShip Basic Functions

        public virtual void SetLights(bool active) {
            simpleToggle.Play();
            shipLights.enabled = !shipLights.enabled;
            
            // ensure that the local player ship lights take priority over all others
            var player = GetComponentInParent<ShipPlayer>();
            if (player && player.isLocalPlayer) {
                shipLights.renderMode = LightRenderMode.ForcePixel;
            }
        }
        
        public virtual void SetAssist(bool active) {
            if (active) assistActivate.Play();
            else assistDeactivate.Play();
        }
        
        public virtual void SetVelocityLimiter(bool active) {
            if (active) velocityLimitActivate.Play();
            else velocityLimitDeactivate.Play();
        }
        
        public void Boost(float boostTime) {
            IEnumerator DoBoost() {
                engineBoostAudioSource.Play();
                // TODO: make two external sources and mix them alternately? (the clip is long and may be played before finished)
                externalBoostAudioSource.Play();
                yield return new WaitForSeconds(1);
                thrusterController.AnimateBoostThrusters();
            }

            StartCoroutine(DoBoost());
        }

        #endregion

        #region Rolling Updates
        
        public virtual void UpdateIndicators(ShipIndicatorData shipIndicatorData) { }

        public virtual void UpdateThrusters(Vector3 force, Vector3 torque) {
            thrusterController.UpdateThrusters(force, torque);
        }

        #endregion

        #region Mesh Quirks

        public void SetCockpitMode(CockpitMode cockpitMode) {
            cockpitInternal.SetActive(cockpitMode == CockpitMode.Internal);
            cockpitExternal.SetActive(cockpitMode == CockpitMode.External);
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

        #endregion
    }
}