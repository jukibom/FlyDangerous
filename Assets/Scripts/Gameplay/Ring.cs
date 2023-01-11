using UnityEngine;

namespace Gameplay {
    public class Ring : MonoBehaviour {
        [SerializeField] private float rotationAmount;

        private Transform _transform;

        private void Start() {
            _transform = transform;
            if (rotationAmount == 0) rotationAmount = Random.Range(-0.5f, 0.5f);
        }

        private void FixedUpdate() {
            _transform.RotateAround(_transform.position, _transform.forward, rotationAmount);
        }
    }
}