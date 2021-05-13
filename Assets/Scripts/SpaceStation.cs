using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceStation : MonoBehaviour {
    public float rotationAmountDegrees = 0.1f;
    public Transform centralSection;

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
        centralSection.RotateAround(transform.position, transform.forward, rotationAmountDegrees);
    }
}
