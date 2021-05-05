using System;
using System.Collections;
using System.Collections.Generic;
using Den.Tools;
using Engine;
using MapMagic.Core;
using UnityEngine;
using UnityEngine.Events;

public class TerrainLoader : MonoBehaviour {
    
    public float terrainGrowFrom = -2000f;
    public float minGrowthRate = 5f;
    public float maxGrowthRate = 100f;
    public float maxDistanceFromPlayer = 1000f;
    public float minDistanceFromPlayer = 10000f;

    private Ship _ship;
    
    private void OnEnable() {
        _ship = FindObjectOfType<Ship>();
        MapMagic.Terrains.TerrainTile.OnTileApplied += OnTileApplied;
    }

    void OnDisable() {
        MapMagic.Terrains.TerrainTile.OnTileApplied -= OnTileApplied;
    }

    private void OnTileApplied(MapMagic.Terrains.TerrainTile tile, MapMagic.Products.TileData tileData, MapMagic.Products.StopToken token) {
        if (Preferences.Instance.GetBool("enableTerrainScaling")) {
            StartCoroutine(GrowTerrainTile(tile));
        }
    }

    // generate a value per tick to translate based on distance to the player (closer = faster)
    private float GenerateGrowthRate(MapMagic.Terrains.TerrainTile tile) {

        // var tileRadius = tile.mapMagic.tileSize.Magnitude / 2;
        var tileRadius = tile.mapMagic.tileSize.x / 2;
        
        // position is calculated from the bottom left corner in MM2, add half the size to get back to the centre
        var tilePosition = tile.transform.position + (tile.mapMagic.tileSize / 2);
        var shipPosition = _ship?.transform.position ?? Vector3.zero;
        
        var distance =
            Vector2.Distance(new Vector2(tilePosition.x, tilePosition.z), new Vector2(shipPosition.x, shipPosition.z)) -
            tileRadius;

        var distanceFactor = Mathf.Max(0.1f, (minDistanceFromPlayer - distance) / (minDistanceFromPlayer - maxDistanceFromPlayer));
        var growthRate = Mathf.Max(minGrowthRate, distanceFactor * maxGrowthRate) / (maxGrowthRate - minGrowthRate);
        
        return growthRate * maxGrowthRate;
    }

    IEnumerator GrowTerrainTile(MapMagic.Terrains.TerrainTile tile) {
        var terrainTransform = tile.GetTerrain(false).transform;
        terrainTransform.Translate(0, terrainGrowFrom, 0);
        while (terrainTransform && terrainTransform.localPosition.y < 0) {
            terrainTransform.Translate(0, GenerateGrowthRate(tile), 0);
            
            yield return null;
        }

        if (terrainTransform) {
            var localPosition = terrainTransform.localPosition;
            terrainTransform.localPosition = new Vector3(localPosition.x, 0, localPosition.z);
        }
    }
}
