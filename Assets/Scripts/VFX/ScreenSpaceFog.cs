using Engine;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Volume))]
public class ScreenSpaceFog : MonoBehaviour {
    private Volume _volume;

    public void Start() {
        _volume = GetComponent<Volume>();
    }

    public void OnEnable() {
        Game.OnGraphicsSettingsApplied += OnGraphicsSettingsApplied;
    }
    
    public void OnDisable() {
        Game.OnGraphicsSettingsApplied -= OnGraphicsSettingsApplied;
    }

    private void OnGraphicsSettingsApplied() {
        if (_volume.profile.TryGet<SCPE.Fog>(out var fog)) {
            var distance = Preferences.Instance.GetFloat("graphics-fog-draw-distance");
            fog.fogStartDistance.Override(distance - 4000f);
            fog.fogEndDistance.Override(distance);
        }
    }
}
