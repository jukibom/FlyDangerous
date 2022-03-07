using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;

public class SpaceStation : MonoBehaviour {
    public float rotationAmountDegrees = 0.1f;
    public Rigidbody centralSection;

    private Quaternion _initialRotation;

    private void OnEnable() {
        Game.OnRestart += ResetStation;
    }

    private void OnDisable() {
        Game.OnRestart -= ResetStation;
    }

    private void Start() {
        _initialRotation = centralSection.rotation;
    }

    private void ResetStation() {
        centralSection.rotation = _initialRotation;
    }

    private void FixedUpdate() {
        Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, 0, rotationAmountDegrees));
        centralSection.MoveRotation(centralSection.rotation * deltaRotation); }
}
