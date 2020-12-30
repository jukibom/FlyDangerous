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
    [SerializeField] private float boostThrustMultiplier = 2;
    
    private FlyDangerousActions _shipActions;
    private bool _isBoosting = false;
    
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
    }

    private void OnEnable() {
        _shipActions.Enable();
    }

    private void OnDisable() {
        _shipActions.Disable();
    }

    public void SetPitch(InputAction.CallbackContext context) {
        _pitch = context.ReadValue<float>();
    }

    public void SetRoll(InputAction.CallbackContext context) {
        _roll = context.ReadValue<float>();
    }

    public void SetYaw(InputAction.CallbackContext context) {
        _yaw = context.ReadValue<float>();
    }

    public void SetThrottle(InputAction.CallbackContext context) {
        _throttle = context.ReadValue<float>();
    }
    
    public void SetLateralH(InputAction.CallbackContext context) {
        _latH = context.ReadValue<float>();
    }
    
    public void SetLateralV(InputAction.CallbackContext context) {
        _latV = context.ReadValue<float>();
    }

    public void Boost(InputAction.CallbackContext context) {
        _isBoosting = context.ReadValueAsButton();
    }

    // Update is called once per frame - poll for input and game activity here (READ)
    private void Update()
    {

    }

    // Apply all physics updates in fixed intervals (WRITE)
    private void FixedUpdate() {

        // TODO: max thrust available to the system must be evenly split between the axes ?
        // otherwise we'll have the old goldeneye problem of travelling diagonally being the optimal play :|
        float thrustMultiplier = _isBoosting ? maxThrust * boostThrustMultiplier : maxThrust;
        float torqueMultiplier = _isBoosting ? maxThrust / 5 : maxThrust / 10;
        
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
            _rigidBodyComponent.AddTorque(_transformComponent.right * (_pitch * torqueMultiplier / 10), ForceMode.Force);
        }
        if (_yaw != 0) {
            _rigidBodyComponent.AddTorque(_transformComponent.up * (_yaw * torqueMultiplier / 10), ForceMode.Force);
        }
        if (_roll != 0) {
            _rigidBodyComponent.AddTorque(_transformComponent.forward * (_roll * torqueMultiplier / 10 * -1), ForceMode.Force);
        }
    }
}
