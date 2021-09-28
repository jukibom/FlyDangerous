using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailPreviewRenderer : MonoBehaviour {
    public Vector3 rotation = new Vector3(0, 0, -1);
    private void FixedUpdate() {
        transform.Rotate(rotation);
    }
}
