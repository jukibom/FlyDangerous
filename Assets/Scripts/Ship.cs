using System;
using System.Collections;
using System.Globalization;
using Audio;
using Engine;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class ShipParameters {
    public float maxSpeed;
    public float maxBoostSpeed;
    public float maxThrust;
    public float torqueThrustMultiplier;
    public float pitchMultiplier;
    public float rollMultiplier;
    public float yawMultiplier;
    public float thrustBoostMultiplier;
    public float torqueBoostMultiplier;
    public float totalBoostTime;
    public float totalBoostRotationalTime;
    public float boostRechargeTime;
    public float minUserLimitedVelocity;

    public string ToJsonString() {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    [CanBeNull]
    public static ShipParameters FromJsonString(string json) {
        try {
            return JsonConvert.DeserializeObject<ShipParameters>(json);
        }
        catch (Exception e){
            Debug.LogWarning(e.Message);
            return null;
        }
    }
}

[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(Rigidbody))]
public class Ship : MonoBehaviour {
    
    // TODO: remove this stuff once params are finalised (this is for debug panel in release)
    public static ShipParameters ShipParameterDefaults {
        get {
            var shipDefaults = new ShipParameters();
            shipDefaults.maxSpeed = 800;
            shipDefaults.maxBoostSpeed = 932;
            shipDefaults.maxThrust = 100000;
            shipDefaults.torqueThrustMultiplier = 0.2f;
            shipDefaults.pitchMultiplier = 1;
            shipDefaults.rollMultiplier = 0.3f;
            shipDefaults.yawMultiplier = 0.5f;
            shipDefaults.thrustBoostMultiplier = 5;
            shipDefaults.torqueBoostMultiplier = 2f;
            shipDefaults.totalBoostTime = 6f;
            shipDefaults.totalBoostRotationalTime = 7f;
            shipDefaults.boostRechargeTime = 5f;
            shipDefaults.minUserLimitedVelocity = 250f;
            return shipDefaults;
        }
    }
    public ShipParameters Parameters {
        get {
            var parameters = new ShipParameters();
            parameters.maxSpeed = maxSpeed; 
            parameters.maxBoostSpeed = maxBoostSpeed;
            parameters.maxThrust = maxThrust;
            parameters.torqueThrustMultiplier = torqueThrustMultiplier;
            parameters.pitchMultiplier = pitchMultiplier;
            parameters.rollMultiplier = rollMultiplier;
            parameters.yawMultiplier = yawMultiplier;
            parameters.thrustBoostMultiplier = thrustBoostMultiplier;
            parameters.torqueBoostMultiplier = torqueBoostMultiplier;
            parameters.totalBoostTime = totalBoostTime;
            parameters.totalBoostRotationalTime = totalBoostRotationalTime;
            parameters.boostRechargeTime = boostRechargeTime;
            parameters.minUserLimitedVelocity = minUserLimitedVelocity;
            return parameters;
        }
        set {
            maxSpeed = value.maxSpeed;
            maxBoostSpeed = value.maxBoostSpeed;
            maxThrust = value.maxThrust;
            torqueThrustMultiplier = value.torqueThrustMultiplier;
            pitchMultiplier = value.pitchMultiplier;
            rollMultiplier = value.rollMultiplier;
            yawMultiplier = value.yawMultiplier;
            thrustBoostMultiplier = value.thrustBoostMultiplier;
            torqueBoostMultiplier = value.torqueBoostMultiplier;
            totalBoostTime = value.totalBoostTime;
            totalBoostRotationalTime = value.totalBoostRotationalTime;
            boostRechargeTime = value.boostRechargeTime;
            minUserLimitedVelocity = value.minUserLimitedVelocity;
        }
    }
    
    // TODO: split this into various thruster powers
    [SerializeField] private Text velocityIndicator;
    [SerializeField] private float maxSpeed = 800;
    [SerializeField] private float maxBoostSpeed = 932;
    [SerializeField] private float maxThrust = 100000;
    [SerializeField] private float torqueThrustMultiplier = 0.2f;
    [SerializeField] private float pitchMultiplier = 1;
    [SerializeField] private float rollMultiplier = 0.3f;
    [SerializeField] private float yawMultiplier = 0.5f;
    [SerializeField] private float thrustBoostMultiplier = 5;
    [SerializeField] private float torqueBoostMultiplier = 2f;
    [SerializeField] private float totalBoostTime = 6f;
    [SerializeField] private float totalBoostRotationalTime = 7f;
    [SerializeField] private float boostRechargeTime = 5f;
    [SerializeField] private float minUserLimitedVelocity = 250f;

    private bool _boostReady;
    private bool _isBoosting;
    private float _currentBoostTime;

    private float _prevVelocity;
    private bool _userVelocityLimit;
    private float _velocityLimitCap;
    private bool _flightAssist;

    // input axes -1 to 1
    private float _throttle ;
    private float _latV;
    private float _latH;
    private float _pitch;
    private float _yaw;
    private float _roll;

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

    public void SetPitch(float value) {
        _pitch = ClampInput(value);
    }

    public void SetRoll(float value) {
        _roll = ClampInput(value);
    }

    public void SetYaw(float value) {
        _yaw = ClampInput(value);
    }

    public void SetThrottle(float value) {
        _throttle = ClampInput(value);
    }
    
    public void SetLateralH(float value) {
        _latH = ClampInput(value);
    }
    
    public void SetLateralV(float value) {
        _latV = ClampInput(value);
    }

    public void Boost(bool isPressed) {
        var boost = isPressed;
        if (boost && !_boostReady) {
            _boostReady = true;
            Debug.Log("Boost Charge");

            IEnumerator DoBoost() {
                AudioManager.Instance.Play("ship-boost");
                yield return new WaitForSeconds(1);
                Debug.Log("Boost!");
                _currentBoostTime = 0f;
                _isBoosting = true;
                yield return new WaitForSeconds(boostRechargeTime);
                _boostReady = false;
            }
            StartCoroutine(DoBoost());
        }
    }

    public void FlightAssistToggle() {
        _flightAssist = !_flightAssist;
        Debug.Log("Flight Assist " + (_flightAssist ? "ON" : "OFF") + " (partially implemented)");
    }

    public void VelocityLimiterIsPressed(bool isPressed) {
        _userVelocityLimit = isPressed;
        Debug.Log("Velocity Limit " + (_userVelocityLimit ? "ON" : "OFF") + " (not implemented)");
        
        if (_userVelocityLimit) {
            AudioManager.Instance.Play("ship-velocity-limit-on");
        }
        else {
            AudioManager.Instance.Play("ship-velocity-limit-off");
        }
    }

    private void OnTriggerEnter(Collider other) {
        var checkpoint = other.GetComponentInParent<Checkpoint>();
        if (checkpoint) {
            checkpoint.Hit();
        }
    }

    // Apply all physics updates in fixed intervals (WRITE)
    private void FixedUpdate() {
        
        float thrustMultiplier = maxThrust;
        float torqueMultiplier = maxThrust * torqueThrustMultiplier;

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
            _velocityLimitCap = Math.Max(_prevVelocity, minUserLimitedVelocity);
            _rigidBodyComponent.velocity = Vector3.ClampMagnitude(_rigidBodyComponent.velocity, _velocityLimitCap);
        }

        // clamp max speed in general
        _rigidBodyComponent.velocity = _isBoosting
            ? Vector3.ClampMagnitude(_rigidBodyComponent.velocity, maxBoostSpeed)
            : Vector3.ClampMagnitude(_rigidBodyComponent.velocity, maxSpeed);    // TODO: reduce this over time

        _prevVelocity = _rigidBodyComponent.velocity.magnitude;
            
        CalculateFlightAssist();
        UpdateIndicators();
    }

    private void UpdateIndicators() {
        if (velocityIndicator != null) {
            velocityIndicator.text = Velocity.ToString(CultureInfo.InvariantCulture);
        }
    }

    /**
     * All axis should be between -1 and 1. 
     */
    private float ClampInput(float input) {
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

            if (Math.Abs(_pitch) < 0.05) {
                if (angularVelocityPitch > 0) {
                    _rigidBodyComponent.AddTorque(
                        _transformComponent.right * (-0.25f * maxThrust * torqueThrustMultiplier), ForceMode.Force);
                }
                else {
                    _rigidBodyComponent.AddTorque(_transformComponent.right * (0.25f * maxThrust * torqueThrustMultiplier),
                        ForceMode.Force);
                }
            }

            if (Math.Abs(_roll) < 0.05) {
                if (angularVelocityRoll > 0) {
                    _rigidBodyComponent.AddTorque(
                        _transformComponent.forward * (-0.25f * maxThrust * torqueThrustMultiplier), ForceMode.Force);
                }
                else {
                    _rigidBodyComponent.AddTorque(
                        _transformComponent.forward * (0.25f * maxThrust * torqueThrustMultiplier), ForceMode.Force);
                }
            }

            if (Math.Abs(_yaw) < 0.05) {
                if (angularVelocityYaw > 0) {
                    _rigidBodyComponent.AddTorque(_transformComponent.up * (-0.25f * maxThrust * torqueThrustMultiplier),
                        ForceMode.Force);
                }
                else {
                    _rigidBodyComponent.AddTorque(_transformComponent.up * (0.25f * maxThrust * torqueThrustMultiplier),
                        ForceMode.Force);
                }
            }
        }
    }
}
