using System;
using Core.ShipModel;
using UnityEngine;

namespace Core.Replays {
    public class ShipGhost : MonoBehaviour, IReplayShip {
        [SerializeField] private ShipPhysics shipPhysics;

        public ShipPhysics ShipPhysics => shipPhysics;
        public void SetAbsolutePosition(Vector3 position) {
            transform.position = FloatingOrigin.Instance.Origin + position;
        }
        
        private void OnEnable() {
            FloatingOrigin.OnFloatingOriginCorrection += PerformCorrection;
        }

        private void OnDisable() {
            FloatingOrigin.OnFloatingOriginCorrection -= PerformCorrection;
        }

        private void PerformCorrection(Vector3 offset) {
            transform.position -= offset;
        }
    }
}