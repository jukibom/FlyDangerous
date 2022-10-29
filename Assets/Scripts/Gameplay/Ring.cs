using UnityEngine;

public class Ring : MonoBehaviour {
    [SerializeField] private float rotationAmount;
    [SerializeField] private bool isTitleScreen;

    private Transform _transform;

    // Start is called before the first frame update
    private void Start() {
        _transform = transform;
        if (rotationAmount == 0) rotationAmount = Random.Range(-0.5f, 0.5f);
    }

    // Update is called once per frame
    private void FixedUpdate() {
        _transform.RotateAround(_transform.position, _transform.forward, rotationAmount);
        if (isTitleScreen) {
            var position = _transform.position;
            position -= 30 * _transform.forward;
            if (position.z < -20000) position += _transform.forward * 40000;
            _transform.position = position;
        }
    }
}