using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ring : MonoBehaviour
{
    
    [SerializeField] float rotationAmount = 0.0f;

    private Transform _transform;
        
    // Start is called before the first frame update
    void Start() {
        _transform = transform;
        if (rotationAmount == 0) {
            rotationAmount = Random.Range(-0.5f, 0.5f);
        }
    }

    // Update is called once per frame
    void FixedUpdate() {
        _transform.rotation = _transform.rotation * Quaternion.AngleAxis(rotationAmount, _transform.forward);
    }
}
