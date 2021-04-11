using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipCamera : MonoBehaviour {

    public Rigidbody target;
    public float smoothSpeed = 0.5f;
    public Vector3 offset;
    public float accelerationDampener = 5f;
    public float angularMomentumDampener = 5f;

    private Vector3 _velocity = Vector3.zero;
    private Vector3 _lastVelocity;

    void Update() {
        var angularVelocity = target.angularVelocity;
        var angularMomentumModifier = target.rotation *
            Quaternion.Euler(
                angularVelocity.x / angularMomentumDampener, 
                angularVelocity.y / angularMomentumDampener, 
                angularVelocity.z / angularMomentumDampener
            ); 
        
        Vector3 rotationModifier = angularMomentumModifier * offset;
        
        var acceleration = (target.velocity - _lastVelocity) / Time.fixedDeltaTime;
        var accelerationDelta = acceleration / accelerationDampener / 100f;
        Vector3 desiredPosition = rotationModifier - accelerationDelta;
        this.transform.localPosition = Vector3.SmoothDamp(this.transform.localPosition, desiredPosition, ref _velocity, smoothSpeed);
        _lastVelocity = target.velocity;
    }
}
