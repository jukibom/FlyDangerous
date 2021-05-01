using System;
using System.Collections;
using System.Collections.Generic;
using Engine;
using UnityEngine;
using UnityEngine.VFX;

public class SpaceDust : MonoBehaviour
{
    private VisualEffect m_VFX;

    private void Start() {
        m_VFX = GetComponent<VisualEffect>();
    }

    // Update is called once per frame
    void Update() {
        m_VFX.enabled = Preferences.Instance.GetBool("showSpaceDust");
    }
}
