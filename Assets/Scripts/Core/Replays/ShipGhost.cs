using Core.ShipModel;
using UnityEngine;

namespace Core.Replays {
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ReplayTimeline))]
    public class ShipGhost : MonoBehaviour, IReplayShip {
        [SerializeField] private ShipPhysics shipPhysics;

        public string PlayerName { get; set; }
        public ReplayTimeline ReplayTimeline { get; private set; }

        private void Awake() {
            Transform = transform;
            Rigidbody = GetComponent<Rigidbody>();
            ReplayTimeline = GetComponent<ReplayTimeline>();
        }

        private void OnEnable() {
            FloatingOrigin.OnFloatingOriginCorrection += PerformCorrection;
            ShipPhysics.OnBoost += ShowBoost;
        }

        private void OnDisable() {
            FloatingOrigin.OnFloatingOriginCorrection -= PerformCorrection;
            ShipPhysics.OnBoost -= ShowBoost;
        }

        public Transform Transform { get; private set; }

        public Rigidbody Rigidbody { get; private set; }

        public ShipPhysics ShipPhysics => shipPhysics;

        public void SetAbsolutePosition(Vector3 ghostFloatingOrigin, Vector3 position) {
            var offset = ghostFloatingOrigin - FloatingOrigin.Instance.Origin;
            transform.position = offset + position;
        }

        public void LoadReplay(Replay replay) {
            ReplayTimeline.LoadReplay(this, replay);
        }

        private void PerformCorrection(Vector3 offset) {
            transform.position -= offset;
        }

        private void ShowBoost(float boostTime) {
            ShipPhysics.ShipModel?.Boost(boostTime);
        }
    }
}