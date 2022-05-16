using MapMagic.Core;
using Misc;
using UnityEngine;

namespace Core.MapData {
    [RequireComponent(typeof(MapMagicObject))]
    public class TerrainLoader : MonoBehaviour {
        private MapMagicObject _mapMagicTerrain;

        public void Awake() {
            _mapMagicTerrain = GetComponent<MapMagicObject>();
            OnGameOptionsApplied();
        }

        private void OnEnable() {
            Game.OnGameSettingsApplied += OnGameOptionsApplied;
        }

        private void OnDisable() {
            Game.OnGameSettingsApplied -= OnGameOptionsApplied;
        }

        private void OnGameOptionsApplied() {
            var terrainLOD = Preferences.Instance.GetFloat("graphics-terrain-geometry-lod");
            var pixelError = MathfExtensions.Remap(10, 100, 50, 5, terrainLOD);
            var textureHqDistance = Preferences.Instance.GetFloat("graphics-terrain-texture-distance");
            var terrainChunks = Preferences.Instance.GetFloat("graphics-terrain-chunks");

            // set map magic preferences
            _mapMagicTerrain.terrainSettings.pixelError = (int)pixelError;
            _mapMagicTerrain.terrainSettings.baseMapDist = (int)textureHqDistance;

            // main terrain chunks
            _mapMagicTerrain.mainRange = (int)terrainChunks;
            // draw draft tiles +1 out
            _mapMagicTerrain.tiles.generateRange = (int)terrainChunks + 1;

            _mapMagicTerrain.terrainSettings.detailDraw = Preferences.Instance.GetBool("graphics-terrain-details");

            // update all existing terrain too
            foreach (var terrainTile in _mapMagicTerrain.tiles.All()) _mapMagicTerrain.terrainSettings.ApplySettings(terrainTile.GetTerrain(false));
        }
    }
}