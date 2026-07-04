using Core.Player;
using Core.ShipModel;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Audio;

using UnityEngine.Rendering;

namespace Core.Replays {
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ReplayTimeline))]
    public class ShipGhost : MonoBehaviour, IReplayShip {
        [SerializeField] private ShipPhysics shipPhysics;
        [SerializeField] private AudioMixerGroup ghostAudioMixer;
        [SerializeField] private ReflectionProbe reflectionProbe;
        public ReplayTimeline ReplayTimeline { get; private set; }
        public string PlayerName { get; set; }
        public Flag PlayerFlag { get; set; }
        public Transform Transform { get; private set; }
        public Rigidbody Rigidbody { get; private set; }
        public ShipPhysics ShipPhysics => shipPhysics;
        public bool SpectatorActive { get; set; }
        
        private void Awake() {
            Transform = transform;
            Rigidbody = GetComponent<Rigidbody>();
            ReplayTimeline = GetComponent<ReplayTimeline>();
            shipPhysics.ShipActive = true;
            var reflectionSetting = Game.Instance.reflectionSetting;

            reflectionProbe.gameObject.SetActive(false);
            reflectionProbe.resolution = reflectionSetting switch {
                "ultra" => 512,
                "high" => 512,
                "medium" => 256,
                _ => 128
            };
            reflectionProbe.timeSlicingMode = reflectionSetting switch {
                "ultra" => ReflectionProbeTimeSlicingMode.NoTimeSlicing,
                "high" => ReflectionProbeTimeSlicingMode.AllFacesAtOnce,
                _ => ReflectionProbeTimeSlicingMode.IndividualFaces
            };
        }
        private void Start() {
            // handle binding all sounds to the ghost mixer
            foreach (var audioSource in GetComponentsInChildren<AudioSource>(true)) audioSource.outputAudioMixerGroup = ghostAudioMixer;
        }

        private void OnEnable() {
            FloatingOrigin.OnFloatingOriginCorrection += OnFloatingOriginCorrection;
            ShipPhysics.OnBoost += ShowBoost;
            ShipPhysics.OnBoostCancel += CancelBoost;
        }

        private void OnDisable() {
            FloatingOrigin.OnFloatingOriginCorrection -= OnFloatingOriginCorrection;
            ShipPhysics.OnBoost -= ShowBoost;
            ShipPhysics.OnBoostCancel -= CancelBoost;
        }

        private void OnCollisionEnter(Collision collisionInfo) {
            ShipPhysics.OnCollision(collisionInfo, true);
        }

        private void OnCollisionStay(Collision collisionInfo) {
            ShipPhysics.OnCollision(collisionInfo, false);
        }

        private void FixedUpdate() {
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                var distance = Vector3.Distance(transform.position, player.User.transform.position);
                var shouldShow = distance > 8 || SpectatorActive;
                if (shipPhysics.ShipModel != null) shipPhysics.ShipModel.SetVisible(shouldShow);
            }
        }

        public void SetAbsolutePosition(Vector3 ghostFloatingOrigin, Vector3 offset) {
            FloatingOrigin.Instance.SetAbsoluteWorldPosition(transform, ghostFloatingOrigin + offset);
            if (SpectatorActive) FloatingOrigin.Instance.CheckNeedsUpdate();
            Rigidbody.MovePosition(transform.position);
        }

        public void EnableReflections() {
            reflectionProbe.gameObject.SetActive(Game.Instance.reflectionSetting != "off");
        }
        public void DisableReflections() {
            reflectionProbe.gameObject.SetActive(false);
        }

        public void LoadReplay(Replay replay) {
            ReplayTimeline.LoadReplay(this, replay);
        }

        private void OnFloatingOriginCorrection(Vector3 offset) {
            if (SpectatorActive) return;
            
            transform.position -= offset;
            Rigidbody.MovePosition(transform.position);
        }

        private void ShowBoost(float spoolTime, float boostTime) {
            ShipPhysics.ShipModel?.Boost(spoolTime, boostTime);
        }

        private void CancelBoost() {
            ShipPhysics.ShipModel?.BoostCancel();
        }
        
        [Button]
        private void TestSpectate() {
            ReplayPrioritizer.Instance.SpectateGhost(this);
        }

        [Button]
        private void TestStopSpectating() {
            ReplayPrioritizer.Instance.StopSpectating();
        }
    }
}