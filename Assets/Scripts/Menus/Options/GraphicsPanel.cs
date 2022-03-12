using System.Collections.Generic;
using Core;
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

        // Strip non-unique values (we don't care about refresh rate)
        var uniqueResolutions = new List<Resolution>();
        foreach (var resolution in _resolutions)
            if (!uniqueResolutions.Exists(r => r.width == resolution.width && r.height == resolution.height))
                uniqueResolutions.Add(resolution);
        _resolutions = uniqueResolutions.ToArray();

        resolutionDropdown.ClearOptions();

        var options = new List<string>();
        var currentResolutionIndex = 0;

        for (var i = 0; i < _resolutions.Length; i++) {
            var resolution = _resolutions[i];
            var option = resolution.width + " x " + resolution.height;
            options.Add(option);

            if (resolution.width == Screen.width &&
                resolution.height == Screen.height
               )
                currentResolutionIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // handle screen mode (hard-coded ids)
        var mode = Screen.fullScreenMode;
        switch (mode) {
            case FullScreenMode.FullScreenWindow:
                screenModeDropdown.value = 0;
                break;
            case FullScreenMode.ExclusiveFullScreen:
                screenModeDropdown.value = 1;
                break;
            case FullScreenMode.Windowed:
                screenModeDropdown.value = 2;
                break;
            default:
                screenModeDropdown.value = 0;
                break;
        }

        screenModeDropdown.RefreshShownValue();
    }

    public void OnResolutionChange(int resolutionIndex) {
        if (!Game.Instance.IsVREnabled) Screen.SetResolution(_resolutions[resolutionIndex].width, _resolutions[resolutionIndex].height, _screenMode);
    }

    public void OnScreenModeChange(int screenModeIndex) {
        switch (screenModeIndex) {
            case 0:
                _screenMode = FullScreenMode.FullScreenWindow;
                break;
            case 1:
                _screenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 2:
                _screenMode = FullScreenMode.Windowed;
                break;
            default:
                _screenMode = FullScreenMode.FullScreenWindow;
                break;
        }

        Screen.fullScreenMode = _screenMode;
    }
}