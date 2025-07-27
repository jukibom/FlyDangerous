using Core.Player;
using Core.ShipModel;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Audio;

namespace Core.Replays {
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ReplayTimeline))]
    public class ShipGhost : MonoBehaviour, IReplayShip {
        [SerializeField] private ShipPhysics shipPhysics;
        [SerializeField] private AudioMixerGroup ghostAudioMixer;
        
        public ReplayTimeline ReplayTimeline { get; private set; }
        public string PlayerName { get; set; }
        public Flag PlayerFlag { get; set; }
        public Transform Transform { get; private set; }
        public Rigidbody Rigidbody { get; private set; }
        public ShipPhysics ShipPhysics => shipPhysics;
        public bool SpectatorActive { get; private set; }
        
        private void Awake() {
            Transform = transform;
            Rigidbody = GetComponent<Rigidbody>();
            ReplayTimeline = GetComponent<ReplayTimeline>();
            shipPhysics.ShipActive = true;
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
                var distance = Vector3.Distance(transform.position, player.transform.position);
                var shouldShow = distance > 8;
                if (shipPhysics.ShipModel != null) shipPhysics.ShipModel.SetVisible(shouldShow);
            }
        }

        /// <summary>
        /// Move the camera from wherever it is to this ghost. This works from the player or another spectated ghost. 
        /// </summary>
        [Button]
        public void Spectate() {
            SpectatorActive = true;

        public Rigidbody Rigidbody { get; private set; }

        public ShipPhysics ShipPhysics => shipPhysics;
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                player.User.TargetTransform = transform;
            }
            else {
                Debug.LogWarning("Failed to set ghost spectator, player does not exist!");
            }
        }

        /// <summary>
        /// Stop spectating the ghost and return control to the player. This does not need to be called first in order
        /// to switch to a different ghost, just call Spectate on that ghost.
        /// </summary>
        [Button]
        public void StopSpectating() {
            SpectatorActive = false;
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                player.User.TargetTransform = player.transform;
            }
            else {
                Debug.LogWarning("Failed to restore player logic from ghost spectator, player does not exist!");
            }
        }

        public void SetAbsolutePosition(Vector3 ghostFloatingOrigin, Vector3 offset) {
            if (!SpectatorActive) {
                var currentOffset = ghostFloatingOrigin - FloatingOrigin.Instance.Origin;
                transform.position = currentOffset + offset;
            }
            else {
                FloatingOrigin.Instance.SetAbsoluteWorldPosition(transform, ghostFloatingOrigin + offset);
                FloatingOrigin.Instance.CheckNeedsUpdate();
                Rigidbody.MovePosition(transform.position);
            }
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
    }
}