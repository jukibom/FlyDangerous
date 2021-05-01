using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceStation : MonoBehaviour {
    public float rotationAmountDegrees = 0.1f;
    public Transform centralSection;
    
    private void FixedUpdate() {
        centralSection.RotateAround(transform.position, transform.forward, rotationAmountDegrees);
    }
}
