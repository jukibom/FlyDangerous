using System.Collections;
using System.Collections.Generic;
using System.Drawing.Imaging;
using Telepathy;
using UnityEngine;
using System;
using System.Threading;
using Unity.Mathematics;

public class MapGenerate : MonoBehaviour
{
    public enum Drawmode {NoiseMap, ColorMap, Mesh};
    public Drawmode drawmode;


    public const int MapChunkSize = 241;
    [Range(0, 6)]
    public int editorpreviewlod;
    public float noisescale;

    public int octaves;
    [Range(0f, 1f)]
    public float persistance;

    public int seed;
    public Vector2 Offset;

    public float meshheightmult;
    public AnimationCurve Meshheightcurve;

    public bool autoupdate;
    public Terraintype[] regions;

    Queue<MapThreadInfo<mapdata>> mapdatathreadsinfoqueue = new Queue<MapThreadInfo<mapdata>>();
    Queue<MapThreadInfo<MeshData>> meshdatathreadinfoqueue = new Queue<MapThreadInfo<MeshData>>();
    public void drawmapineditor()
    {
        mapdata mapdata = Generatemapdata(Vector2.zero);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawmode == Drawmode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.texturefromheightmap(mapdata.heightmap));
        }
        else if (drawmode == Drawmode.ColorMap)
        {
             display.DrawTexture(TextureGenerator.TexturefromeColormap(mapdata.colormap, MapChunkSize, MapChunkSize));
        }
        else if (drawmode == Drawmode.Mesh)
        {
             display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapdata.heightmap, meshheightmult, Meshheightcurve, editorpreviewlod,new Vector2(0,0)), TextureGenerator.TexturefromeColormap(mapdata.colormap, MapChunkSize, MapChunkSize));
        }
    }

    public void RequestMapData(Vector2 center, Action<mapdata> callback)
    {
        ThreadStart threadstart = delegate
        {
            mapdataThread(center ,callback);
        };
        new Thread(threadstart).Start();
    }
    void mapdataThread(Vector2 center, Action<mapdata> callback)
    {
        mapdata mapdata = Generatemapdata(center);
        lock (mapdatathreadsinfoqueue)
        {
            mapdatathreadsinfoqueue.Enqueue(new MapThreadInfo<mapdata>(callback, mapdata));
        }
    }
    public void RequestMeshData(int LOD, mapdata mapdata, Action<MeshData> callback)
    {
        ThreadStart threadstart = delegate
        {
            Meshdatathread(LOD, mapdata, callback);
        };
        new Thread(threadstart).Start();
    }
    void Meshdatathread(int LOD, mapdata mapdata, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapdata.heightmap, meshheightmult, Meshheightcurve , LOD,mapdata.chunkcoord);
        lock (meshdatathreadinfoqueue)
        {
            meshdatathreadinfoqueue.Enqueue(new MapThreadInfo<MeshData>(callback,meshData));
        }
    }
    void Update()
    {
        if (mapdatathreadsinfoqueue.Count > 0)
        {
            for(int i = 0;i<mapdatathreadsinfoqueue.Count;i++)
            {
                MapThreadInfo<mapdata> threadinfo = mapdatathreadsinfoqueue.Dequeue();
                threadinfo.callback(threadinfo.parameter);
            }
        }
        if (meshdatathreadinfoqueue.Count > 0)
        {
            for (int i = 0; i < meshdatathreadinfoqueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadinfo = meshdatathreadinfoqueue.Dequeue();
                threadinfo.callback(threadinfo.parameter);
            }
        }
    }
    mapdata Generatemapdata(Vector2 center)
    {
        float[,] noisemap = MapNoise.GenerateNoiseMap(MapChunkSize, MapChunkSize, seed, noisescale, octaves, persistance,center + Offset);
        Color[] colormap = new Color[MapChunkSize * MapChunkSize];

        for (int y = 0; y < MapChunkSize; y++)
        {
            for (int x = 0; x < MapChunkSize; x++)
            {
                float currentheight = noisemap[x, y];
                for (int i = 0; i<regions.Length; i++)
                {
                    if (currentheight >= regions[i].height)
                    {
                        colormap[y * MapChunkSize + x] = regions[i].color;
                        
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        return new mapdata(noisemap,colormap,center);

    }
    private void OnValidate()
    {

        if (octaves < 0)
        {
            octaves = 0;
        }

    }
    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

}
[System.Serializable]
public struct Terraintype
{
    public string name;
    public float height;
    public Color color;

}
public struct mapdata
{
    public readonly float[,] heightmap;
    public readonly Color[] colormap;
    public Vector2 chunkcoord;

    public mapdata(float[,] heightmap, Color[] colormap, Vector2 chunkcoord)
    {
        this.chunkcoord = chunkcoord;
        this.heightmap = heightmap;
        this.colormap = colormap;
    }
}