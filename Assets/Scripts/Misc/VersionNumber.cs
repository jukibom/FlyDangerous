using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class VersionNumber : MonoBehaviour
{
    public void Awake() {
        GetComponent<Text>().text = $"FlyDangerous {Application.version} (feedback alpha)";
    }
}
