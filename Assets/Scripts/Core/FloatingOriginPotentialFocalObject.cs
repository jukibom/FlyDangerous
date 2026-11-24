using Core.Tools;
using UnityEngine;

namespace Core { 
    /// <summary>
    /// Attach this to objects which may need to update their origin outside the @world prefab.
    /// Use this on any root transform of an object which may be the focal transform and thus shouldn't change (they
    /// get reset to 0,0,0 instead!)
    /// </summary>
    public class FloatingOriginPotentialFocalObject : MonoBehaviour {
        
        private Optional<Rigidbody> _rigidbody = Optional<Rigidbody>.Empty;
        
        private void OnEnable() {
            if (TryGetComponent<Rigidbody>(out var rb)) {
                _rigidbody = new(rb);
            }
            FloatingOrigin.OnFloatingOriginCorrection += OnFloatingOriginCorrection;
        }

        private void OnDisable() {
            FloatingOrigin.OnFloatingOriginCorrection -= OnFloatingOriginCorrection;
        }

        private void OnFloatingOriginCorrection(Vector3 offset) {
            if (FloatingOrigin.Instance.FocalTransform == transform) return;
            transform.position -= offset;
            if (_rigidbody.TryGetValue(out var rb)) {
                rb.position = transform.position;
            }
        }
    }
}