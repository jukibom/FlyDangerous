using Core;
using MapMagic.Core;
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
        #if (NO_PAID_ASSETS == false)
            var mapMagic = FindObjectOfType<MapMagicObject>();
            if (mapMagic && _volume.profile.TryGet<SCPE.Fog>(out var fog)) {
                var tileChunkCount = Preferences.Instance.GetFloat("graphics-terrain-chunks") + 1; // include drafts
                var tileSize = mapMagic.tileSize.x;
                
                var fogEndDistance = (tileSize * tileChunkCount) - tileSize / 2;
                var fogStartDistance = Mathf.Max(1000f, fogEndDistance - fogEndDistance / 1.5f); 
                
                fog.fogEndDistance.Override(fogEndDistance);
                fog.fogStartDistance.Override(fogStartDistance);
            }
        #endif
    }

    private void OnVRStatus(bool vrEnabled) {
        UseRadialFog(!vrEnabled);
    }

    private void UseRadialFog(bool useRadialFog) {
        #if (NO_PAID_ASSETS == false)
            if (_volume && _volume.profile.TryGet<SCPE.Fog>(out var fog)) {
                fog.useRadialDistance.Override(useRadialFog);
            }
        #endif
    }
}
