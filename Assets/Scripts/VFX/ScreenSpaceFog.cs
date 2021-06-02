using Engine;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Volume))]
public class ScreenSpaceFog : MonoBehaviour {
    private Volume _volume;

    public void Start() {
        _volume = GetComponent<Volume>();
        UseRadialFog(!Game.Instance.IsVREnabled);
    }

    public void OnEnable() {
        Game.OnGraphicsSettingsApplied += OnGraphicsSettingsApplied;
        Game.OnVRStatus += OnVRStatus;
    }
    
    public void OnDisable() {
        Game.OnGraphicsSettingsApplied -= OnGraphicsSettingsApplied;
        Game.OnVRStatus -= OnVRStatus;
    }

    private void OnGraphicsSettingsApplied() {
        if (_volume.profile.TryGet<SCPE.Fog>(out var fog)) {
            var distance = Preferences.Instance.GetFloat("graphics-fog-draw-distance");
            fog.fogStartDistance.Override(distance - 4000f);
            fog.fogEndDistance.Override(distance);
        }
    }

    private void OnVRStatus(bool vrEnabled) {
        UseRadialFog(!vrEnabled);
    }

    private void UseRadialFog(bool useRadialFog) {
        if (_volume && _volume.profile.TryGet<SCPE.Fog>(out var fog)) {
            fog.useRadialDistance.Override(useRadialFog);
        }
    }
}
