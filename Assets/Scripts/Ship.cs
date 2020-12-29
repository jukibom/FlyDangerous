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
    
    private FlyDangerousActions shipActions;
    private bool isBoosting = false;
    
    // input axes -1 to 1
    private float throttle = 0;
    private float pitch = 0;
    private float yaw = 0;
    private float roll = 0;
    private float latV = 0;
    private float latH = 0;

    private Transform transformComponent;
    private Rigidbody rigidBodyComponent;
    
    // Start is called before the first frame update
    private void Awake() {
        transformComponent = GetComponent<Transform>();
        rigidBodyComponent = GetComponent<Rigidbody>();

        // TODO: revisit the canonical method of doing this junk with the preview package (sigh)
        shipActions = new FlyDangerousActions();
        shipActions.Ship.Pitch.performed += SetPitch;
        shipActions.Ship.Pitch.canceled += SetPitch;
        shipActions.Ship.Roll.performed += SetRoll;
        shipActions.Ship.Roll.canceled += SetRoll;
        shipActions.Ship.Yaw.performed += SetYaw;
        shipActions.Ship.Yaw.canceled += SetYaw;
        shipActions.Ship.Throttle.performed += SetThrottle;
        shipActions.Ship.Throttle.canceled += SetThrottle;
        shipActions.Ship.LateralH.performed += SetLateralH;
        shipActions.Ship.LateralH.canceled += SetLateralH;
        shipActions.Ship.LateralV.performed += SetLateralV;
        shipActions.Ship.LateralV.canceled += SetLateralV;
        shipActions.Ship.Boost.performed += Boost;
        shipActions.Ship.Boost.canceled += Boost;
    }

    private void OnEnable() {
        shipActions.Enable();
    }

    private void OnDisable() {
        shipActions.Disable();
    }

    public void SetPitch(InputAction.CallbackContext context) {
        pitch = context.ReadValue<float>();
    }

    public void SetRoll(InputAction.CallbackContext context) {
        roll = context.ReadValue<float>();
    }

    public void SetYaw(InputAction.CallbackContext context) {
        yaw = context.ReadValue<float>();
    }

    public void SetThrottle(InputAction.CallbackContext context) {
        throttle = context.ReadValue<float>();
    }
    
    public void SetLateralH(InputAction.CallbackContext context) {
        latH = context.ReadValue<float>();
    }
    
    public void SetLateralV(InputAction.CallbackContext context) {
        latV = context.ReadValue<float>();
    }

    public void Boost(InputAction.CallbackContext context) {
        isBoosting = context.ReadValueAsButton();
        if (isBoosting) {
            Debug.Log("ITS WORKING");
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
        float thrustMultiplier = isBoosting ? maxThrust * boostThrustMultiplier : maxThrust;
        float torqueMultiplier = isBoosting ? maxThrust / 5 : maxThrust / 10;
        
        if (throttle != 0) {
            rigidBodyComponent.AddForce(transformComponent.forward * (throttle * thrustMultiplier), ForceMode.Force);
        }
        if (latH != 0) {
            rigidBodyComponent.AddForce(transformComponent.right * (latH * thrustMultiplier), ForceMode.Force);
        }
        if (latV != 0) {
            rigidBodyComponent.AddForce(transformComponent.up * (latV * thrustMultiplier), ForceMode.Force);
        }
        if (pitch != 0) {
            rigidBodyComponent.AddTorque(transformComponent.right * (pitch * torqueMultiplier / 10), ForceMode.Force);
        }
        if (yaw != 0) {
            rigidBodyComponent.AddTorque(transformComponent.up * (yaw * torqueMultiplier / 10), ForceMode.Force);
        }
        if (roll != 0) {
            rigidBodyComponent.AddTorque(transformComponent.forward * (roll * torqueMultiplier / 10 * -1), ForceMode.Force);
        }
    }
}
