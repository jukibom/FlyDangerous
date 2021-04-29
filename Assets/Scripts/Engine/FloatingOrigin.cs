using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingOrigin : MonoBehaviour {
    
    [SerializeField]
    public GameObject focalObject;  // this should be the client player ship
    
    // Distance required to perform a correction. If 0, will occur every frame.
    [SerializeField]
    public float correctionDistance = 0.0f;
    
    private Transform _worldTransform;
    private Transform _focalTransform;

    public Vector3 focalObjectPosition {
        get { return -this._worldTransform.position;  }
    }
    
    // Start is called before the first frame update
    void Start() {
        this._focalTransform = focalObject.transform;
        this._worldTransform = this.transform;
    }

    void Update() {
        if (_focalTransform.position.magnitude > correctionDistance) {
            this._worldTransform.position -= _focalTransform.position;
            this._focalTransform.position = Vector3.zero;
        }
    }
}
