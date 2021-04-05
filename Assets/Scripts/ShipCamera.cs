using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipCamera : MonoBehaviour {

    public Rigidbody target;
    public float smoothSpeed = 0.5f;
    public Vector3 offset;

    private Vector3 _velocity = Vector3.zero;

    private Vector3 m_LastVelocity;
    
    // Update is called once per frame
    void FixedUpdate() {
        Quaternion targetRotation = target.rotation;
        Transform thisTransform = transform;
        thisTransform.rotation = targetRotation;

        if (m_LastVelocity != null) {
            var acceleration = (target.velocity - m_LastVelocity) / Time.fixedDeltaTime;
            var accelerationDelta = acceleration / 50;
            
            Vector3 desiredPosition = target.position + (targetRotation * offset) - accelerationDelta;
            thisTransform.position = Vector3.SmoothDamp(thisTransform.position, desiredPosition, ref _velocity, smoothSpeed);
        }

        m_LastVelocity = target.velocity;
    }
}
