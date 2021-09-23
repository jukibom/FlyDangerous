using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Misc;
using UnityEngine;

public class ShipCamera : MonoBehaviour {

    public Rigidbody target;
    public float smoothSpeed = 0.5f;
    public float accelerationDampener = 5f;
    public float angularMomentumDampener = 5f;

    private Vector3 _velocity = Vector3.zero;
    private Vector3 _lastVelocity;

    private readonly Vector3 _minPos = new Vector3(-0.1175f, -0.0678f, -0.2856f);
    private readonly Vector3 _maxPos = new Vector3(0.1175f, 0.04f, 0.0412f);

    private float _baseFov;
    private Camera[] _cameras;
    
    public void OnEnable() {
        Game.OnGraphicsSettingsApplied += SetBaseFov;
        _cameras = GetComponentsInChildren<Camera>();
        SetBaseFov();
    }
    
    public void OnDisable() {
        Game.OnGraphicsSettingsApplied -= SetBaseFov;
    }

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
        
        // Fov
        foreach (var playerCamera in _cameras) {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, MathfExtensions.Remap(0, _minPos.z, _baseFov, _baseFov + 20, cameraPosition.z), 0.1f);
        }
    }

    private void SetBaseFov() {
        _baseFov = Preferences.Instance.GetFloat("graphics-field-of-view");
    }
}
