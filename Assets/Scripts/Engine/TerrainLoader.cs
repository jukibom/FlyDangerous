using System.Collections;
using Den.Tools;
using Engine;
using MapMagic.Core;
using Misc;
using UnityEngine;

[RequireComponent(typeof(MapMagicObject))]
public class TerrainLoader : MonoBehaviour {

    private MapMagicObject _mapMagicTerrain;

    public void Start() {
        _mapMagicTerrain = GetComponent<MapMagicObject>();
        OnGraphicsOptionsApplied();
    }

    private void OnEnable() {
        Game.OnGraphicsSettingsApplied += OnGraphicsOptionsApplied;
    }

    void OnDisable() {
        Game.OnGraphicsSettingsApplied -= OnGraphicsOptionsApplied;
    }

    void OnGraphicsOptionsApplied() {
        var terrainLOD = Preferences.Instance.GetFloat("graphics-terrain-geometry-lod");
        var pixelError = MathfExtensions.Remap(10, 100, 50, 0, terrainLOD);
        var textureHQDistance = Preferences.Instance.GetFloat("graphics-terrain-texture-distance");
        var terrainChunks = Preferences.Instance.GetFloat("graphics-terrain-chunks");
        var terrainHighRes = Preferences.Instance.GetBool("enableExperimentalHighResTerrain");

        // set map magic preferences
        _mapMagicTerrain.terrainSettings.pixelError = (int) pixelError;
        _mapMagicTerrain.terrainSettings.baseMapDist = (int) textureHQDistance;
        if (terrainHighRes) {
            _mapMagicTerrain.tileResolution = MapMagicObject.Resolution._513;
            _mapMagicTerrain.tileMargins = 6;
        }
        
        _mapMagicTerrain.mainRange = (int) terrainChunks;
        _mapMagicTerrain.tiles.generateRange = (int) terrainChunks;

        _mapMagicTerrain.terrainSettings.detailDraw = Preferences.Instance.GetBool("graphics-terrain-details");
        
        // update all existing terrain too
        foreach (var terrainTile in _mapMagicTerrain.tiles.All()) {
            _mapMagicTerrain.terrainSettings.ApplySettings(terrainTile.GetTerrain(false));
        }
    }
}
