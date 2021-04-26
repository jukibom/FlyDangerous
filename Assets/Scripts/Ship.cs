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
    [SerializeField] private float maxThrust = 100;
    [SerializeField] private float thrustBoostMultiplier = 2;
    [SerializeField] private float torqueThrustDivider = 5;
    [SerializeField] private float torqueBoostMultiplier = 1.2f;
    [SerializeField] private Text velocityIndicator;

    private bool _boostStarted = false;
    private bool _isBoosting = false;
    private float _currentBoostTime = 0f;
    private float _totalBoostTime = 4f;
    private float _totalBoostRotationalTime = 5f; 
    
    private bool _velocityLimit = false;
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
    
    public void Awake() {
        _transformComponent = GetComponent<Transform>();
        _rigidBodyComponent = GetComponent<Rigidbody>();
    }

    public void Start() {
        _flightAssist = Preferences.Instance.GetBool("flightAssistOnByDefault");
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
        if (boost && !_boostStarted && !_isBoosting) {
            _boostStarted = true;
            Debug.Log("Boost Charge");

            IEnumerator Buildup() {
                yield return new WaitForSeconds(1);
                Debug.Log("Boost!");
                _isBoosting = true;
            }
            AudioManager.Instance.Play("ship-boost");
            StartCoroutine(Buildup());
        }
    }

    public void OnFlightAssistToggle(InputValue value) {
        _flightAssist = !_flightAssist;
        Debug.Log("Flight Assist " + (_flightAssist ? "ON" : "OFF") + " (partially implemented)");
    }

    public void OnVelocityLimiter(InputValue value) {
        _velocityLimit = value.isPressed;
        Debug.Log("Velocity Limit " + (_velocityLimit ? "ON" : "OFF") + " (not implemented)");
    }

    // Apply all physics updates in fixed intervals (WRITE)
    private void FixedUpdate() {
        
        float thrustMultiplier = maxThrust;
        float torqueMultiplier = maxThrust / torqueThrustDivider;

        _currentBoostTime += Time.fixedDeltaTime;
        if (_isBoosting && _boostStarted) {
            _boostStarted = false;
            _currentBoostTime = 0f;
        }

        if (_isBoosting) {
            // reduce boost potency over time period
            thrustMultiplier *= Mathf.Lerp(thrustBoostMultiplier, 1, _currentBoostTime / _totalBoostTime);
            torqueMultiplier *= Mathf.Lerp(torqueBoostMultiplier, 1, _currentBoostTime / _totalBoostRotationalTime);
            
            // cannot reverse while boosting! sorry mate 
            _throttle = 1;
        }
        
        if (_currentBoostTime > _totalBoostRotationalTime) {
            _isBoosting = false; 
        }


        // TODO: max thrust available to the system must be evenly split between the axes ?
        // otherwise we'll have the old goldeneye problem of travelling diagonally being the optimal play :|
        // float thrustMultiplier = _isBoosting ? maxThrust * thrustBoostMultiplier : maxThrust;
        // float torqueMultiplier = _isBoosting ? torqueBoostMultiplier * maxThrust / torqueThrustDivider : maxThrust / torqueThrustDivider;
        
        if (_throttle != 0) {
            _rigidBodyComponent.AddForce(_transformComponent.forward * (_throttle * thrustMultiplier), ForceMode.Force);
        }
        if (_latH != 0) {
            _rigidBodyComponent.AddForce(_transformComponent.right * (_latH * thrustMultiplier), ForceMode.Force);
        }
        if (_latV != 0) {
            _rigidBodyComponent.AddForce(_transformComponent.up * (_latV * thrustMultiplier), ForceMode.Force);
        }
        if (_pitch != 0) {
            _rigidBodyComponent.AddTorque(_transformComponent.right * (_pitch * torqueMultiplier), ForceMode.Force);
        }
        if (_yaw != 0) {
            _rigidBodyComponent.AddTorque(_transformComponent.up * (_yaw * torqueMultiplier), ForceMode.Force);
        }
        if (_roll != 0) {
            _rigidBodyComponent.AddTorque(_transformComponent.forward * (_roll * torqueMultiplier * -1), ForceMode.Force);
        }
        
        CalculateFlightAssist();
        UpdateIndicators();
    }

    private void UpdateIndicators() {
        if (velocityIndicator != null) {
            velocityIndicator.text = Math.Floor(_rigidBodyComponent.velocity.magnitude).ToString(CultureInfo.InvariantCulture);
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
