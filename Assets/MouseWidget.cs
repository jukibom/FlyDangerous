using System.Collections;
using System.Collections.Generic;
using Engine;
using UnityEngine;
using UnityEngine.UI;

public class MouseWidget : MonoBehaviour {

    public Vector2 mousePositionScreen = Vector2.zero;
    public GameObject crosshair;
    public GameObject arrow;

    private RawImage _crosshairImage;
    private RawImage _arrowImage;
    
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
        var mousePositionNormalised = new Vector2(
            (mousePositionScreen.x / Screen.width * 2) - 1,
            (mousePositionScreen.y / Screen.height * 2 - 1)
        );
        
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
        Debug.Log(normalisedMagnitude);
        arrowImageColor.a = Mathf.Pow(normalisedMagnitude, 2);
        crosshairImageColor.a = Mathf.Pow(1f - normalisedMagnitude, 2);

        _arrowImage.color = arrowImageColor;
        _crosshairImage.color = crosshairImageColor;
    }
    
    public void SetMousePositionScreen(Vector2 mousePos) {
        mousePositionScreen = mousePos;
    }
}
