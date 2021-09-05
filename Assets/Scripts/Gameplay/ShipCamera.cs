using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipCamera : MonoBehaviour {

    public Rigidbody target;
    public float smoothSpeed = 0.5f;
    public float accelerationDampener = 5f;
    public float angularMomentumDampener = 5f;

    private Vector3 _velocity = Vector3.zero;
    private Vector3 _lastVelocity;

    private readonly Vector3 _minPos = new Vector3(-0.2175f, -0.0678f, -0.2856f);
    private readonly Vector3 _maxPos = new Vector3(0.2175f, 0.0561f, 0.0412f);

    public void Reset() {
        _lastVelocity = Vector3.zero;
        _lastVelocity = Vector3.zero;
        transform.localPosition = Vector3.zero;
    }
    
    void FixedUpdate() {
        var angularVelocity = target.angularVelocity;
        Vector3 rotationCameraModifier = Quaternion.AngleAxis(90, Vector3.back) * (angularMomentumDampener * angularVelocity);

        var acceleration = transform.InverseTransformDirection(target.velocity - _lastVelocity) / Time.fixedDeltaTime;
        var accelerationCameraDelta = -acceleration / accelerationDampener / 100f;
        
        Vector3 desiredPosition = accelerationCameraDelta - rotationCameraModifier;

        var cameraPosition = Vector3.SmoothDamp(transform.localPosition, desiredPosition, ref _velocity, smoothSpeed);
        cameraPosition = Vector3.Min(Vector3.Max(cameraPosition, _minPos), _maxPos);
        transform.localPosition = cameraPosition;
        
        _lastVelocity = target.velocity;
    }
}
