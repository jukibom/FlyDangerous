using System;
using System.Collections;
using System.Collections.Generic;
using Engine;
using UnityEngine;
using UnityEngine.UI;

public class MouseWidget : MonoBehaviour {
    
    public GameObject crosshair;
    public GameObject arrow;

    private RawImage _crosshairImage;
    private RawImage _arrowImage;

    public Vector2 mousePositionNormalised = Vector2.zero;
    
    public float maxDistanceUnits = 60f;
    
    // Start is called before the first frame update
    void Start() {
        _crosshairImage = crosshair.GetComponent<RawImage>();
        _arrowImage = arrow.GetComponent<RawImage>();
    }

    // Update is called once per frame
    void Update() {
        
        // pref determines draw active
        var shouldShow = Preferences.Instance.GetBool("showMouseWidget");
        crosshair.SetActive(shouldShow);
        arrow.SetActive(shouldShow);

        // position
        arrow.transform.localPosition = Vector3.ClampMagnitude(new Vector3(
            mousePositionNormalised.x * maxDistanceUnits,
            mousePositionNormalised.y * maxDistanceUnits,
            0
        ), maxDistanceUnits);
        
        // rotation
        if (mousePositionNormalised != Vector2.zero) {
            var dir = arrow.transform.localPosition;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            arrow.transform.localRotation = Quaternion.AngleAxis(angle - 90, Vector3.forward);
        }

        var arrowImageColor = _arrowImage.color;
        var crosshairImageColor = _crosshairImage.color;
        var normalisedMagnitude = arrow.transform.localPosition.magnitude / maxDistanceUnits;
        arrowImageColor.a = normalisedMagnitude;
        crosshairImageColor.a = Mathf.Pow(1f - normalisedMagnitude, 2);

        _arrowImage.color = arrowImageColor;
        _crosshairImage.color = crosshairImageColor;
    }

    public void UpdateWidgetSprites(Vector2 mousePositionNormalised) {
        if (mousePositionNormalised != Vector2.zero) {
            this.mousePositionNormalised = mousePositionNormalised;
        }
    }
}
