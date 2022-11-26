using System.Collections;
using System.Collections.Generic;
using System.Drawing.Imaging;
using Telepathy;
using UnityEngine;
using UnityEngine.Profiling;
using System;
using System.Threading;
using Unity.Mathematics;
using Core;
using UnityEngine.PlayerLoop;
using Misc;
using System.Security.Cryptography;
using Steamworks;

public class MapGenerate : MonoBehaviour
{
    public enum Drawmode {NoiseMap, ColorMap, Mesh};
    public Drawmode drawmode;


    public const int MapChunkSize = 241;
    [Range(0, 6)]
    public int editorpreviewlod;

    public Vector2 Offset;


    public bool autoupdate;
    public bool useRegions;
    public Terraintype[] regions;


    public MapNoise.NoiseData[] Noisearray;

    Queue<MapThreadInfo<mapdata>> mapdatathreadsinfoqueue = new Queue<MapThreadInfo<mapdata>>();
    Queue<MapThreadInfo<MeshData>> meshdatathreadinfoqueue = new Queue<MapThreadInfo<MeshData>>();

    private void Start()
    {

    }
    private void Awake()
    {

        setseed();
    }
    void setseed()
    {
        if(Game.Instance.SessionStatus == SessionStatus.Loading)
        {
            Noisearray[1].seed = int.Parse(HashGenerator.ComputeSha256Hash(Game.Instance.Seed).Remove(0,56),System.Globalization.NumberStyles.HexNumber);
        }
        else
        {
           // Noisearray[1].seed = Noisearray[1].seed;
        }
    }
    
    public void drawmapineditor()
    {
        Profiler.BeginSample("DrawInEditor");
        Profiler.BeginSample("GenerateMapData");
        mapdata mapdata = Generatemapdata(Offset);
        Profiler.EndSample();
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
             display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapdata.heightmap, editorpreviewlod), TextureGenerator.TexturefromeColormap(mapdata.colormap, MapChunkSize, MapChunkSize));
        }
        Profiler.EndSample();
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
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapdata.heightmap, LOD);
        lock (meshdatathreadinfoqueue)
        {
            meshdatathreadinfoqueue.Enqueue(new MapThreadInfo<MeshData>(callback,meshData));
        }
    }
    void Update()
    {

        if (mapdatathreadsinfoqueue.Count > 0)
        {
            Profiler.BeginSample("MapDataCallback");
            for(int i = 0;i<mapdatathreadsinfoqueue.Count;i++)
            {
                MapThreadInfo<mapdata> threadinfo = mapdatathreadsinfoqueue.Dequeue();
                threadinfo.callback(threadinfo.parameter);
            }
            Profiler.EndSample();
        }

        if (meshdatathreadinfoqueue.Count > 0)
        {

            Profiler.BeginSample("MeshDataCallback");
            for (int i = 0; i < meshdatathreadinfoqueue.Count; i++)
            {
                Profiler.BeginSample("set threadinfo");
                MapThreadInfo<MeshData> threadinfo = meshdatathreadinfoqueue.Dequeue();
                Profiler.EndSample();
                Profiler.BeginSample("Executecallback");
                threadinfo.callback(threadinfo.parameter);
                Profiler.EndSample();
            }
            Profiler.EndSample();
        }
    }
    mapdata Generatemapdata(Vector2 center)
    {
        /*
        Endlessterrain.PRNG rng = new Endlessterrain.PRNG(VerticalNoiseData.seed);
        VerticalNoiseData.persistance = math.lerp(0.3f,0.7f, rng.NextFloat());
        VerticalNoiseData.frequencyScale = math.lerp(0.25f, 4.0f, math.sqrt(rng.NextFloat()));
        HorizontalNoisedata.seed = rng.Next(1000000, -100000);
        */

        Vector3[,] noisemap = MapNoise.GenerateNoiseMap(MapChunkSize,center, Noisearray);

        Color[] colormap = new Color[MapChunkSize * MapChunkSize];

        for (int y = 0; y < MapChunkSize; y++)
        {
            for (int x = 0; x < MapChunkSize; x++)
            {
                float currentheight = noisemap[x, y].y;


                if (useRegions)
                {
                    for (int i = 0; i < regions.Length; i++)
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
        }
        return new mapdata(noisemap,colormap,center);

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
    public readonly Vector3[,] heightmap;
    public readonly Color[] colormap;
    public Vector2 chunkcoord;

    public mapdata(Vector3[,] heightmap, Color[] colormap, Vector2 chunkcoord)
    {
        this.chunkcoord = chunkcoord;
        this.heightmap = heightmap;
        this.colormap = colormap;
    }
}