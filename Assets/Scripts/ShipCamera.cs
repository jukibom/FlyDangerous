using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipCamera : MonoBehaviour {

    public Rigidbody target;
    public float smoothSpeed = 0.5f;
    public Vector3 offset;
    public float accelerationDampener = 500f;

    private Vector3 _velocity = Vector3.zero;

    private Vector3 m_LastVelocity;
    
    // Update is called once per frame
    void FixedUpdate() {

        var angularVelocity = target.angularVelocity;
        var angularMomentumModifier =
            target.rotation * Quaternion.Euler(-angularVelocity.x / 2, -angularVelocity.y / 2, -angularVelocity.z / 2);
        
        Vector3 targetRotation = angularMomentumModifier * offset;
        
        Transform thisTransform = transform;

        if (m_LastVelocity != null) {
            var acceleration = (target.velocity - m_LastVelocity) / Time.fixedDeltaTime;
            var accelerationDelta = acceleration / accelerationDampener;
            
            Vector3 desiredPosition = target.position + targetRotation - accelerationDelta;
            thisTransform.position = Vector3.SmoothDamp(thisTransform.position, desiredPosition, ref _velocity, smoothSpeed);
        }

        m_LastVelocity = target.velocity;
    }
}
