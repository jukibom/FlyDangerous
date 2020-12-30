using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(Rigidbody))]
public class Ship : MonoBehaviour {
    
    // TODO: split this into various thruster powers
    [SerializeField] private float maxThrust = 100;
    [SerializeField] private float thrustBoostMultiplier = 2;
    [SerializeField] private float torqueThrustDivider = 5;
    [SerializeField] private float torqueBoostMultiplier = 1.2f;
    
    private FlyDangerousActions _shipActions;
    private bool _isBoosting = false;
    private bool _flightAssist = false;

    // input axes -1 to 1
    private float _throttle = 0;
    private float _pitch = 0;
    private float _yaw = 0;
    private float _roll = 0;
    private float _latV = 0;
    private float _latH = 0;

    private Transform _transformComponent;
    private Rigidbody _rigidBodyComponent;
    
    // Start is called before the first frame update
    private void Awake() {
        _transformComponent = GetComponent<Transform>();
        _rigidBodyComponent = GetComponent<Rigidbody>();

        // TODO: revisit the canonical method of doing this junk with the preview package (sigh)
        _shipActions = new FlyDangerousActions();
        _shipActions.Ship.Pitch.performed += SetPitch;
        _shipActions.Ship.Pitch.canceled += SetPitch;
        _shipActions.Ship.Roll.performed += SetRoll;
        _shipActions.Ship.Roll.canceled += SetRoll;
        _shipActions.Ship.Yaw.performed += SetYaw;
        _shipActions.Ship.Yaw.canceled += SetYaw;
        _shipActions.Ship.Throttle.performed += SetThrottle;
        _shipActions.Ship.Throttle.canceled += SetThrottle;
        _shipActions.Ship.LateralH.performed += SetLateralH;
        _shipActions.Ship.LateralH.canceled += SetLateralH;
        _shipActions.Ship.LateralV.performed += SetLateralV;
        _shipActions.Ship.LateralV.canceled += SetLateralV;
        _shipActions.Ship.Boost.performed += Boost;
        _shipActions.Ship.Boost.canceled += Boost;
        _shipActions.Ship.FlightAssistToggle.performed += ToggleFlightAssist;
        _shipActions.Ship.FlightAssistToggle.canceled += ToggleFlightAssist;
    }

    private void OnEnable() {
        _shipActions.Enable();
    }

    private void OnDisable() {
        _shipActions.Disable();
    }

    public void SetPitch(InputAction.CallbackContext context) {
        _pitch = ClampInput(context.ReadValue<float>());
    }

    public void SetRoll(InputAction.CallbackContext context) {
        _roll = ClampInput(context.ReadValue<float>());
    }

    public void SetYaw(InputAction.CallbackContext context) {
        _yaw = ClampInput(context.ReadValue<float>());
    }

    public void SetThrottle(InputAction.CallbackContext context) {
        _throttle = ClampInput(context.ReadValue<float>());
    }
    
    public void SetLateralH(InputAction.CallbackContext context) {
        _latH = ClampInput(context.ReadValue<float>());
    }
    
    public void SetLateralV(InputAction.CallbackContext context) {
        _latV = ClampInput(context.ReadValue<float>());
    }

    public void Boost(InputAction.CallbackContext context) {
        _isBoosting = context.ReadValueAsButton();
        if (_isBoosting) {
            Debug.Log("Boost!");
        }
    }

    public void ToggleFlightAssist(InputAction.CallbackContext context) {
        if (context.ReadValueAsButton()) {
            _flightAssist = !_flightAssist;
            Debug.Log("Flight Assist " + (_flightAssist ? "ON" : "OFF"));
        }
    }

    // Update is called once per frame - poll for input and game activity here (READ)
    private void Update()
    {

    }

    // Apply all physics updates in fixed intervals (WRITE)
    private void FixedUpdate() {

        // TODO: max thrust available to the system must be evenly split between the axes ?
        // otherwise we'll have the old goldeneye problem of travelling diagonally being the optimal play :|
        float thrustMultiplier = _isBoosting ? maxThrust * thrustBoostMultiplier : maxThrust;
        float torqueMultiplier = _isBoosting ? torqueBoostMultiplier * maxThrust / torqueThrustDivider : maxThrust / torqueThrustDivider;
        
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
