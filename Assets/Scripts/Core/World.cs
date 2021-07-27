using System;
using UnityEngine;

namespace Core {
    public class World : MonoBehaviour {
        private Vector3 _aggregateCorrections;
        
        private void OnEnable() {
            FloatingOrigin.OnFloatingOriginCorrection += PerformCorrection;
        }

        private void OnDisable() {
            FloatingOrigin.OnFloatingOriginCorrection -= PerformCorrection;
        }

        public void Reset() {
            PerformCorrection(_aggregateCorrections);
            _aggregateCorrections = Vector3.zero;
        }

        private void PerformCorrection(Vector3 offset) {
            foreach (Transform child in transform) {
                child.position -= offset;
            }

            _aggregateCorrections -= offset;
        }
    }
}