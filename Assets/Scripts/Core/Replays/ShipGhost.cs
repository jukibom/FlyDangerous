﻿using Core.Player;
using Core.ShipModel;
using UnityEngine;
using UnityEngine.Audio;

namespace Core.Replays {
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ReplayTimeline))]
    public class ShipGhost : MonoBehaviour, IReplayShip {
        [SerializeField] private ShipPhysics shipPhysics;
        [SerializeField] private AudioMixerGroup ghostAudioMixer;
        public ReplayTimeline ReplayTimeline { get; private set; }

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

        private void FixedUpdate() {
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                var distance = Vector3.Distance(transform.position, player.transform.position);
                var shouldShow = distance > 8;
                if (shipPhysics.ShipModel != null) shipPhysics.ShipModel.SetVisible(shouldShow);
            }
        }

        private void OnEnable() {
            FloatingOrigin.OnFloatingOriginCorrection += PerformCorrection;
            ShipPhysics.OnBoost += ShowBoost;
            ShipPhysics.OnBoostCancel += CancelBoost;
        }

        private void OnDisable() {
            FloatingOrigin.OnFloatingOriginCorrection -= PerformCorrection;
            ShipPhysics.OnBoost -= ShowBoost;
            ShipPhysics.OnBoostCancel -= CancelBoost;
        }

        private void OnCollisionEnter(Collision collisionInfo) {
            ShipPhysics.OnCollision(collisionInfo, true);
        }

        private void OnCollisionStay(Collision collisionInfo) {
            ShipPhysics.OnCollision(collisionInfo, false);
        }

        public string PlayerName { get; set; }
        public Flag PlayerFlag { get; set; }

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

        private void ShowBoost(float spoolTime, float boostTime) {
            ShipPhysics.ShipModel?.Boost(spoolTime, boostTime);
        }

        private void CancelBoost() {
            ShipPhysics.ShipModel?.BoostCancel();
        }
    }
}