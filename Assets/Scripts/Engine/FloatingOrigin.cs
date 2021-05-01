using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FloatingOrigin : MonoBehaviour {

    [SerializeField] public Transform focalTransform; // this should be the client player ship

    // Distance required to perform a correction. If 0, will occur every frame.
    [SerializeField] public float correctionDistance = 0.0f; 
    
    // The target world transform to manipulate
     private Transform _worldTransform;

     public Vector3 FocalObjectPosition => -this._worldTransform.position;

    void OnEnable() {
        _worldTransform = GameObject.Find("World")?.transform;
        if (!_worldTransform) {
            Debug.LogWarning("Floating Origin failed to find target World! Is one loaded?");
        }
    }

    void Update() {
        if (_worldTransform && focalTransform.position.magnitude > correctionDistance) {
            _worldTransform.position -= focalTransform.position;
            focalTransform.position = Vector3.zero;
        }
    }

}
