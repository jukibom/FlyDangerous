using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingOrigin : MonoBehaviour {
    public GameObject focalObject;  // this should be the client player ship
    private Transform _worldTransform;
    private Transform _focalTransform;
    
    // Start is called before the first frame update
    void Start() {
        this._focalTransform = focalObject.transform;
        this._worldTransform = this.transform;
        this._focalTransform.position = Vector3.zero;
    }

    void Update() {
        this._worldTransform.position -= _focalTransform.position;
        this._focalTransform.position = Vector3.zero;
    }
}
