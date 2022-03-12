using UnityEngine;

public class Ring : MonoBehaviour {
    [SerializeField] private float rotationAmount;

    private Transform _transform;

    // Start is called before the first frame update
    private void Start() {
        _transform = transform;
        if (rotationAmount == 0) rotationAmount = Random.Range(-0.5f, 0.5f);
    }

    // Update is called once per frame
    private void FixedUpdate() {
        _transform.RotateAround(_transform.position, _transform.forward, rotationAmount);
    }
}