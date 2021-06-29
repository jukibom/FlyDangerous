using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;
using UnityEngine.VFX;

public class SpaceDust : MonoBehaviour {
    [SerializeField]
    private bool forceOn;
    private VisualEffect _VFX;

    private void Start() {
        _VFX = GetComponent<VisualEffect>();
    }

    // Update is called once per frame
    void Update() {
        if (!forceOn) {
            _VFX.enabled = Preferences.Instance.GetBool("showSpaceDust");
        }
    }
}
