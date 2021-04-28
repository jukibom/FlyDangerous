using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Audio;
using Engine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(Rigidbody))]
public class Ship : MonoBehaviour {
    
    // TODO: split this into various thruster powers
    [SerializeField] private Text velocityIndicator;
    [SerializeField] private float maxSpeed = 800;
    [SerializeField] private float maxBoostSpeed = 932;
    [SerializeField] private float maxThrust = 100;
    [SerializeField] private float pitchMultiplier = 1;
    [SerializeField] private float rollMultiplier = 0.8f;
    [SerializeField] private float yawMultiplier = 0.7f;
    [SerializeField] private float thrustBoostMultiplier = 2;
    [SerializeField] private float torqueThrustDivider = 5;
    [SerializeField] private float torqueBoostMultiplier = 1.2f;
    [SerializeField] private float totalBoostTime = 4f;
    [SerializeField] private float totalBoostRotationalTime = 5f;
    [SerializeField] private float boostRechargeTime = 5f;
    [SerializeField] private float minUserLimitedVelocity = 250f;

    private bool _boostReady = false;
    private bool _isBoosting = false;
    private float _currentBoostTime = 0f;

    private bool _userVelocityLimit = false;
    private float _velocityLimitCap = 0f;
    private bool _flightAssist = false;

    // input axes -1 to 1
    private float _throttle = 0;
    private float _latV = 0;
    private float _latH = 0;
    private float _pitch = 0;
    private float _yaw = 0;
    private float _roll = 0;

    private Transform _transformComponent;
    private Rigidbody _rigidBodyComponent;
    
    public float Velocity {
        get {
            return Mathf.Round(_rigidBodyComponent.velocity.magnitude);
        }
    } 
    
    public void Awake() {
        _transformComponent = GetComponent<Transform>();
        _rigidBodyComponent = GetComponent<Rigidbody>();
    }

    public void Start() {
        _flightAssist = Preferences.Instance.GetBool("flightAssistOnByDefault");
        _rigidBodyComponent.centerOfMass = Vector3.zero;
        _rigidBodyComponent.inertiaTensorRotation = Quaternion.identity;
    }

    public void OnPitch(InputValue value) {
        _pitch = ClampInput(value.Get<float>());
    }

    public void OnRoll(InputValue value) {
        _roll = ClampInput(value.Get<float>());
    }

    public void OnYaw(InputValue value) {
        _yaw = ClampInput(value.Get<float>());
    }

    public void OnThrottle(InputValue value) {
        _throttle = ClampInput(value.Get<float>());
    }
    
    public void OnLateralH(InputValue value) {
        _latH = ClampInput(value.Get<float>());
    }
    
    public void OnLateralV(InputValue value) {
        _latV = ClampInput(value.Get<float>());
    }

    public void OnBoost(InputValue value) {
        var boost = value.isPressed;
        if (boost && !_boostReady) {
            _boostReady = true;
            Debug.Log("Boost Charge");

            IEnumerator Boost() {
                AudioManager.Instance.Play("ship-boost");
                yield return new WaitForSeconds(1);
                Debug.Log("Boost!");
                _currentBoostTime = 0f;
                _isBoosting = true;
                yield return new WaitForSeconds(boostRechargeTime);
                _boostReady = false;
            }
            StartCoroutine(Boost());
        }
    }

    public void OnFlightAssistToggle(InputValue value) {
        _flightAssist = !_flightAssist;
        Debug.Log("Flight Assist " + (_flightAssist ? "ON" : "OFF") + " (partially implemented)");
    }

    public void OnVelocityLimiter(InputValue value) {
        _userVelocityLimit = value.isPressed;
        _velocityLimitCap = Math.Max(_rigidBodyComponent.velocity.magnitude, minUserLimitedVelocity);
        Debug.Log("Velocity Limit " + (_userVelocityLimit ? "ON" : "OFF") + " (not implemented)");
    }

    // Apply all physics updates in fixed intervals (WRITE)
    private void FixedUpdate() {
        
        float thrustMultiplier = maxThrust;
        float torqueMultiplier = maxThrust / torqueThrustDivider;

        _currentBoostTime += Time.fixedDeltaTime;

        if (_isBoosting) {
            // reduce boost potency over time period
            // Ease-in (boost dropoff is more dramatic)
            float tBoost = _currentBoostTime / totalBoostTime;
            tBoost = 1f - Mathf.Cos(tBoost * Mathf.PI * 0.5f);
            
            float tTorque = _currentBoostTime / totalBoostTime;
            tTorque = 1f - Mathf.Cos(tTorque * Mathf.PI * 0.5f);

            thrustMultiplier *= Mathf.Lerp(thrustBoostMultiplier, 1, tBoost);
            torqueMultiplier *= Mathf.Lerp(torqueBoostMultiplier, 1, tTorque);
        }
        
        if (_currentBoostTime > totalBoostRotationalTime) {
            _isBoosting = false; 
        }
        
        // TODO: max thrust available to the system must be evenly split between the axes ?
        // otherwise we'll have the old goldeneye problem of travelling diagonally being the optimal play :|
        
        // special case for throttle - no reverse and full power while boosting! sorry mate 
        var throttle = _isBoosting && _currentBoostTime < totalBoostTime
            ? 1
            : _throttle;

        var tThrust = new Vector3(
            _latH * thrustMultiplier,
            _latV * thrustMultiplier,
            throttle * thrustMultiplier
        );

        var tRot = new Vector3(
            _pitch * pitchMultiplier * torqueMultiplier,
            _yaw * yawMultiplier * torqueMultiplier,
            _roll * rollMultiplier * torqueMultiplier * -1
        );
        
        _rigidBodyComponent.AddForce(transform.TransformDirection(tThrust));
        _rigidBodyComponent.AddTorque(transform.TransformDirection(tRot));

        // clamp max speed if user is holding the velocity limiter button down
        if (_userVelocityLimit) {
            _rigidBodyComponent.velocity = Vector3.ClampMagnitude(_rigidBodyComponent.velocity, _velocityLimitCap);
        }

        // clamp max speed in general
        _rigidBodyComponent.velocity = _isBoosting
            ? Vector3.ClampMagnitude(_rigidBodyComponent.velocity, maxBoostSpeed)
            : Vector3.ClampMagnitude(_rigidBodyComponent.velocity, maxSpeed);    // TODO: reduce this over time
        
        CalculateFlightAssist();
        UpdateIndicators();
    }

    private void UpdateIndicators() {
        if (velocityIndicator != null) {
            velocityIndicator.text = Velocity.ToString(CultureInfo.InvariantCulture);
        }
    }

    /**
     * All axis should be between -1 and 1. This clamps the value and adds a (very) small deadzone (0.05) 
     */
    private float ClampInput(float input) {
        if (input < 0.05 & input > -0.05) input = 0;
        return Mathf.Min(Mathf.Max(input, -1), 1);
    }

    private void CalculateFlightAssist() {
        // TODO: Should this actually modify input instead of directly applying force?
        
        if (_flightAssist) {
            // vector should be pushed back towards forward (apply force to cancel lateral motion)
            float hVelocity = Vector3.Dot(_transformComponent.right, _rigidBodyComponent.velocity);
            float vVelocity = Vector3.Dot(_transformComponent.up, _rigidBodyComponent.velocity);
            float fVelocity = Vector3.Dot(_transformComponent.forward, _rigidBodyComponent.velocity);
            
            if (hVelocity > 0) {
                _rigidBodyComponent.AddForce(_transformComponent.right * (-0.5f * maxThrust), ForceMode.Force);
            }
            else {
                _rigidBodyComponent.AddForce(_transformComponent.right * (0.5f * maxThrust), ForceMode.Force);
            }
            if (vVelocity > 0) {
                _rigidBodyComponent.AddForce(_transformComponent.up * (-0.5f * maxThrust), ForceMode.Force);
            }
            else {
                _rigidBodyComponent.AddForce(_transformComponent.up * (0.5f * maxThrust), ForceMode.Force);
            }
            
            // TODO: Different throttle control for flight assist (throttle becomes a target with a max speed)

            // torque should be reduced to 0 on all axes
            float angularVelocityPitch = Vector3.Dot(_transformComponent.right, _rigidBodyComponent.angularVelocity);
            float angularVelocityRoll = Vector3.Dot(_transformComponent.forward, _rigidBodyComponent.angularVelocity);
            float angularVelocityYaw = Vector3.Dot(_transformComponent.up, _rigidBodyComponent.angularVelocity);

            if (angularVelocityPitch > 0) {
                _rigidBodyComponent.AddTorque(_transformComponent.right * (-0.5f * maxThrust / torqueThrustDivider), ForceMode.Force);
            }
            else {
                _rigidBodyComponent.AddTorque(_transformComponent.right * (0.5f * maxThrust / torqueThrustDivider), ForceMode.Force);
            }
            
            if (angularVelocityRoll > 0) {
                _rigidBodyComponent.AddTorque(_transformComponent.forward * (-0.5f * maxThrust / torqueThrustDivider), ForceMode.Force);
            }
            else {
                _rigidBodyComponent.AddTorque(_transformComponent.forward * (0.5f * maxThrust / torqueThrustDivider), ForceMode.Force);
            }
            
            if (angularVelocityYaw > 0) {
                _rigidBodyComponent.AddTorque(_transformComponent.up * (-0.5f * maxThrust / torqueThrustDivider), ForceMode.Force);
            }
            else {
                _rigidBodyComponent.AddTorque(_transformComponent.up * (0.5f * maxThrust / torqueThrustDivider), ForceMode.Force);
            }
        }
    }
}
