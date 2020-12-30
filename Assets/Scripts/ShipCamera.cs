using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipCamera : MonoBehaviour {

    public Transform target;
    public float smoothSpeed = 0.5f;
    public Vector3 offset;

    private Vector3 _velocity = Vector3.zero;
    
    // Update is called once per frame
    void FixedUpdate() {
        Quaternion targetRotation = target.rotation;
        Transform thisTransform = transform;
        
        Vector3 desiredPosition = target.position + (targetRotation * offset);
        transform.position = Vector3.SmoothDamp(thisTransform.position, desiredPosition, ref _velocity, smoothSpeed);
        thisTransform.rotation = targetRotation;
    }
}
