using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GraphicsPanel : MonoBehaviour {

    [SerializeField] private Dropdown resolutionDropdown;
    [SerializeField] private Dropdown screenModeDropdown;

    private Resolution[] _resolutions;
    private FullScreenMode _screenMode;

    private void OnEnable() {
        _resolutions = Screen.resolutions;
        _screenMode = Screen.fullScreenMode;
        
        resolutionDropdown.ClearOptions();

        var options = new List<string>();
        var currentResolutionIndex = 0;
        
        for (int i = 0; i < _resolutions.Length; i++) {
            var resolution = _resolutions[i];
            var option = resolution.width + " x " + resolution.height + " @ " + resolution.refreshRate + "Hz";
            options.Add(option);

            if (resolution.width == Screen.currentResolution.width &&
                resolution.height == Screen.currentResolution.height) {
                currentResolutionIndex = i;
            }
        }
        
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void OnResolutionChange(int resolutionIndex) {
        Screen.SetResolution(_resolutions[resolutionIndex].width, _resolutions[resolutionIndex].height, _screenMode);
    }

    public void OnScreenModeChange(int screenModeIndex) {
        switch (screenModeIndex) {
            case 0: _screenMode = FullScreenMode.FullScreenWindow;
                break;
            case 1: _screenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 2: _screenMode = FullScreenMode.Windowed;
                break;
            default: _screenMode = FullScreenMode.FullScreenWindow;
                break;
        }

        Screen.fullScreenMode = _screenMode;
    }
}
