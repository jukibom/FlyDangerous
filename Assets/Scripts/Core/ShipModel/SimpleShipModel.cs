using System.Collections;
using System.Collections.Generic;
using Core.Player;
using Core.ShipModel.Feedback.interfaces;
using Core.ShipModel.ShipIndicator;
using JetBrains.Annotations;
using Misc;
using UnityEngine;
using VFX;
#if !NO_PAID_ASSETS
using GPUInstancer;
#endif

namespace Core.ShipModel {
    /**
     * Provide some basic ship functionality expected from all mesh objects in an override-able fashion.
     */
    public class SimpleShipModel : MonoBehaviour, IShipModel {
        [SerializeField] private GameObject drawableContainer;
        [SerializeField] private ThrusterController thrusterController;
        [SerializeField] private Light shipLights;
        [SerializeField] private SmokeEmitter smokeEmitter;
        [SerializeField] public CapsuleCollider foliageCollider;

        [SerializeField] private List<MeshRenderer> primaryColorMeshes = new();
        [SerializeField] private List<MeshRenderer> accentColorMeshes = new();
        [SerializeField] private Shield shield;

        [SerializeField] private AudioSource engineBoostAudioSource;
        [SerializeField] private AudioSource externalBoostAudioSource;
        [SerializeField] private AudioSource externalBoostThrusterAudioSource;
        [SerializeField] private AudioSource externalBoostInterruptedAudioSource;
        [SerializeField] private AudioSource simpleToggleAudioSource;
        [SerializeField] private AudioSource assistActivateAudioSource;
        [SerializeField] private AudioSource assistDeactivateAudioSource;
        [SerializeField] private AudioSource velocityLimitActivateAudioSource;
        [SerializeField] private AudioSource velocityLimitDeactivateAudioSource;
        [SerializeField] private AudioSource nightVisionActivateAudioSource;
        [SerializeField] private AudioSource nightVisionDeactivateAudioSource;

        private Coroutine _boostCoroutine;
        private bool _velocityLimiterActive;

        public virtual void Start() {
            // Init GPU Instance removal colliders 
#if !NO_PAID_ASSETS
            if (FindObjectOfType<GPUInstancerDetailManager>()) {
                var grassInstanceRemover = foliageCollider.gameObject.AddComponent<GPUInstancerInstanceRemover>();
                grassInstanceRemover.selectedColliders = new List<Collider> { foliageCollider };
                grassInstanceRemover.removeFromDetailManagers = true;
                grassInstanceRemover.removeFromPrefabManagers = false;
                grassInstanceRemover.removeFromTreeManagers = false;
                grassInstanceRemover.offset = 10;
                grassInstanceRemover.removeAtUpdate = true;
                grassInstanceRemover.useBounds = true;
            }

            if (FindObjectOfType<GPUInstancerTreeManager>()) {
                var treeInstanceRemover = foliageCollider.gameObject.AddComponent<GPUInstancerInstanceRemover>();
                treeInstanceRemover.selectedColliders = new List<Collider> { foliageCollider };
                treeInstanceRemover.removeFromDetailManagers = false;
                treeInstanceRemover.removeFromPrefabManagers = false;
                treeInstanceRemover.removeFromTreeManagers = true;
                treeInstanceRemover.removeAtUpdate = true;
                treeInstanceRemover.useBounds = true;
            }
#endif
        }

        public virtual void FixedUpdate() {
            ShipShake?.FixedUpdate();
        }

        public virtual void OnEnable() {
            Game.OnPauseToggle += PauseAudio;
            Game.OnRestart += Restart;
        }

        public virtual void OnDisable() {
            Game.OnPauseToggle -= PauseAudio;
            Game.OnRestart -= Restart;
        }

        public ShipCameraRig ShipCameraRig { get; set; }

        [CanBeNull] public ShipShake ShipShake { get; private set; }

        public MonoBehaviour Entity() {
            return this;
        }

        public void SetVisible(bool visible) {
            drawableContainer.SetActive(visible);
        }

        public void SetIsLocalPlayer(bool isLocalPlayer) {
            engineBoostAudioSource.priority = isLocalPlayer ? 0 : 128;
            externalBoostAudioSource.priority = isLocalPlayer ? 0 : 128;
            externalBoostThrusterAudioSource.priority = isLocalPlayer ? 0 : 128;
            simpleToggleAudioSource.priority = isLocalPlayer ? 1 : 128;
            assistActivateAudioSource.priority = isLocalPlayer ? 1 : 128;
            assistDeactivateAudioSource.priority = isLocalPlayer ? 1 : 128;
            velocityLimitActivateAudioSource.priority = isLocalPlayer ? 1 : 128;
            velocityLimitDeactivateAudioSource.priority = isLocalPlayer ? 1 : 128;
            if (isLocalPlayer) ShipShake = new ShipShake(transform, ShipCameraRig);
        }

        #region IShip Basic Functions

        public virtual void SetNightVision(bool active) {
            if (active) nightVisionActivateAudioSource.Play();
            else nightVisionDeactivateAudioSource.Play();

            // enable the associated light
            shipLights.enabled = !shipLights.enabled;

            // ensure that the local player ship lights take priority over all others
            var player = GetComponentInParent<ShipPlayer>();
            if (player && player.isLocalPlayer) shipLights.renderMode = LightRenderMode.ForcePixel;
        }

        public virtual void SetAssist(AssistToggleType assistToggleType, bool active) {
            if (active) assistActivateAudioSource.Play();
            else assistDeactivateAudioSource.Play();
        }

        public virtual void SetVelocityLimiter(bool active) {
            _velocityLimiterActive = active;
            if (active) velocityLimitActivateAudioSource.Play();
            else velocityLimitDeactivateAudioSource.Play();
        }

        public void Boost(float spoolTime, float boostTime) {
            IEnumerator AnimateBoost() {
                ShipShake?.AddShake(spoolTime, 0.005f, false, new AnimationCurve(new Keyframe(0, 1), new Keyframe(spoolTime, 1)));
                yield return new WaitForSeconds(spoolTime);
                externalBoostThrusterAudioSource.Play();
                ShipShake?.AddShake(boostTime, 0.01f, true);
                thrusterController.AnimateBoostThrusters();
            }

            engineBoostAudioSource.Play();
            externalBoostAudioSource.Play();
            _boostCoroutine = StartCoroutine(AnimateBoost());
        }

        public void BoostCancel() {
            if (_boostCoroutine != null) {
                StopCoroutine(_boostCoroutine);

                externalBoostInterruptedAudioSource.pitch = Random.value.Remap(0, 1, 0.7f, 1.3f);
                externalBoostInterruptedAudioSource.Play();
                externalBoostAudioSource.Stop();
                externalBoostThrusterAudioSource.Stop();
            }
        }

        #endregion

        #region Rolling Updates

        public virtual void OnShipInstrumentUpdate(IShipInstrumentData shipInstrumentData) {
            /* Indicators are entirely model-specific and should be implemented. */
        }

        public virtual void OnShipMotionUpdate(IShipMotionData shipMotionData) {
            // TODO: I literally have no idea where this came from or why, maybe do some digging here?
            // At least it's localised now ...
            var torqueVec = new Vector3(
                shipMotionData.CurrentAngularTorqueNormalised.x,
                shipMotionData.CurrentAngularTorqueNormalised.y.Remap(-0.8f, 0.8f, -1, 1),
                shipMotionData.CurrentAngularTorqueNormalised.z.Remap(-0.3f, 0.3f, -1, 1)
            );

            var accelerationNormalised = shipMotionData.CurrentLateralForceNormalised.magnitude;
            if (accelerationNormalised > 0.4f) {
                var accelerationShakeAmountMax = _velocityLimiterActive ? 0.008f : 0.004f;
                ShipShake?.AddShake(Time.fixedDeltaTime, accelerationNormalised.Remap(0.4f, 1, 0, accelerationShakeAmountMax), true);
            }

            thrusterController.UpdateThrusters(shipMotionData.CurrentLateralForceNormalised, torqueVec);
            smokeEmitter.UpdateThrustTrail(shipMotionData.CurrentLateralVelocity, shipMotionData.MaxSpeed,
                shipMotionData.CurrentLateralForceNormalised);
            foliageCollider.radius = shipMotionData.CurrentLateralVelocity.magnitude.Remap(0, shipMotionData.MaxSpeed / 2, 4, 15);

            // if we're going faster than max (boost pad yay!) let's make that shield go woOOoooOoo
            var velocityNormalised = shipMotionData.CurrentLateralVelocityNormalised.magnitude;
            if (shipMotionData.CurrentLateralVelocityNormalised.magnitude > 1.1f) shield.Fizzle();
        }

        public virtual void OnShipFeedbackUpdate(IShipFeedbackData shipFeedbackData) {
            if (shipFeedbackData.CollisionThisFrame) {
                if (shipFeedbackData.CollisionStartedThisFrame) {
                    ShipShake?.AddShake(0.2f, shipFeedbackData.CollisionImpactNormalised * Time.fixedDeltaTime);
                    shield.OnImpact(shipFeedbackData.CollisionImpactNormalised, shipFeedbackData.CollisionDirection);
                }
                else {
                    ShipShake?.AddShake(Time.fixedDeltaTime * 3, shipFeedbackData.CollisionImpactNormalised * 3 * Time.fixedDeltaTime);
                    shield.OnContinuousCollision(shipFeedbackData.CollisionDirection);
                }
            }
        }

        #endregion

        #region User Preferences

        public virtual void SetPrimaryColor(string htmlColor) {
            var color = ColorExtensions.ParseHtmlColor(htmlColor);
            primaryColorMeshes.ForEach(mesh => {
                var mat = mesh.material;
                mat.color = color;
            });
        }

        public virtual void SetAccentColor(string htmlColor) {
            var color = ColorExtensions.ParseHtmlColor(htmlColor);
            accentColorMeshes.ForEach(mesh => {
                var mat = mesh.material;
                mat.color = color;
            });
        }

        public virtual void SetThrusterColor(string htmlColor) {
            var color = ColorExtensions.ParseHtmlColor(htmlColor);
            foreach (var thruster in GetComponentsInChildren<Thruster>()) thruster.ThrustColor = color;
        }

        /**
         * Set the color of the trails which occur under boost
         */
        public virtual void SetTrailColor(string htmlColor) {
            var trailColor = ColorExtensions.ParseHtmlColor(htmlColor);
            smokeEmitter.UpdateColor(trailColor);
        }

        /**
         * Set the color of the ship head-lights
         */
        public virtual void SetHeadLightsColor(string htmlColor) {
            var color = ColorExtensions.ParseHtmlColor(htmlColor);
            foreach (var shipLight in GetComponentsInChildren<Light>())
                if (shipLight.type == LightType.Spot)
                    shipLight.color = color;
        }

        public void BillboardCollision() {
            shield.Fizzle(3);
        }

        #endregion

        #region Internal Helper

        protected void PauseAudio(bool paused) {
            if (Game.Instance.SessionType != SessionType.Singleplayer) return;
            foreach (var audioSource in GetComponentsInChildren<AudioSource>())
                if (paused) audioSource.Pause();
                else audioSource.UnPause();
        }

        private void Restart() {
            ShipShake?.Reset();
            externalBoostInterruptedAudioSource.Stop();
            engineBoostAudioSource.Stop();
            externalBoostAudioSource.Stop();
            externalBoostThrusterAudioSource.Stop();
            if (_boostCoroutine != null) StopCoroutine(_boostCoroutine);
        }

        #endregion
    }
}