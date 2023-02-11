using Core;
using MapMagic.Core;
using UnityEngine;
using UnityEngine.Rendering;
#if !NO_PAID_ASSETS
using SCPE;
#endif

namespace VFX {
    [RequireComponent(typeof(Volume))]
    public class ScreenSpaceFog : MonoBehaviour {
        private Volume _volume;

        public void Awake() {
            _volume = GetComponent<Volume>();
            UseRadialFog(!Game.IsVREnabled);
        }

        public void OnEnable() {
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
            Game.OnVRStatus += OnVRStatus;
        }

        public void OnDisable() {
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
            Game.OnVRStatus -= OnVRStatus;
        }

        private void OnGameSettingsApplied() {
#if !NO_PAID_ASSETS
            var mapMagic = FindObjectOfType<MapMagicObject>();
            if (mapMagic && _volume.profile.TryGet<Fog>(out var fog)) {
                var tileChunkCount = Preferences.Instance.GetFloat("graphics-terrain-chunks") + 1; // include drafts
                var tileSize = mapMagic.tileSize.x;
                var tileGenBuffer = 1.7f; // a little leeway for time to generate tiles in the distance

                var fogEndDistance = (tileSize * tileChunkCount - tileSize / 2) / tileGenBuffer;
                var fogStartDistance = Mathf.Max(fogEndDistance / 2, fogEndDistance - tileSize);

                fog.fogEndDistance.Override(fogEndDistance);
                fog.fogStartDistance.Override(fogStartDistance);
            }
#endif
        }

        private void OnVRStatus(bool vrEnabled) {
            UseRadialFog(!vrEnabled);
        }

        private void UseRadialFog(bool useRadialFog) {
#if !NO_PAID_ASSETS
            if (_volume && _volume.profile.TryGet<Fog>(out var fog)) fog.useRadialDistance.Override(useRadialFog);
#endif
        }
    }
}