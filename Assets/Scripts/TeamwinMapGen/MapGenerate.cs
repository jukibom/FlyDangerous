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

    static bool isPlaying;

    public MapNoise.NoiseData[] NoiseArray;
    static MapNoise.NoiseData[] noisedata;

    Queue<MapThreadInfo<mapdata>> mapdatathreadsinfoqueue = new Queue<MapThreadInfo<mapdata>>();
    Queue<MapThreadInfo<MeshData>> meshdatathreadinfoqueue = new Queue<MapThreadInfo<MeshData>>();

    int MaxThreads = Environment.ProcessorCount - 3;
    int ThreadsActive = 0;

    int MaxChunks = 81;

    Queue<ThreadStart> threadPool = new Queue<ThreadStart>();

    


    private void Start()
    {
        isPlaying = true;
        noisedata = NoiseArray;
    }
    private void Awake()
    {
        setseed();
    }
    void Update()
    {
        CleanupThreads();
    }
    public void CleanupThreads()
    {
        // Count Active Threads
        if (ThreadsActive > MaxThreads)
        {
            print("Max Threads Exceeded: " + ThreadsActive);
        }
        else
        {
            print("Current Threads: " + ThreadsActive);
            //Go through thread pool
            if (threadPool.Count > 0)
            {
                for (int i = 0; i < threadPool.Count; i++)
                {
                    if (ThreadsActive < MaxThreads)
                    {
                        ThreadStart threadstart = threadPool.Dequeue();
                        new Thread(threadstart).Start();
                        ThreadsActive++;
                    }
                }
            }
        }

        //Finalize Map Info Threads
        if (mapdatathreadsinfoqueue.Count > 0)
        {
            for (int i = 0; i < mapdatathreadsinfoqueue.Count; i++)
            {
                MapThreadInfo<mapdata> threadinfo = mapdatathreadsinfoqueue.Dequeue();
                threadinfo.callback(threadinfo.parameter);
                ThreadsActive--;
            }
        }
        //Finalize Mesh Data Threads
        if (meshdatathreadinfoqueue.Count > 0)
        {
            for (int i = 0; i < meshdatathreadinfoqueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadinfo = meshdatathreadinfoqueue.Dequeue();
                threadinfo.callback(threadinfo.parameter);
                ThreadsActive--;
            }
        }
    }
    void setseed()
    {
        if(Game.Instance.SessionStatus == SessionStatus.Loading)
        {

            for(int i=0;i<NoiseArray.Length;i++)
            {
                NoiseArray[i].seed = int.Parse(HashGenerator.ComputeSha256Hash(Game.Instance.Seed).Remove(0, 56), System.Globalization.NumberStyles.HexNumber)-i;
            }
        }
        else
        {
           // Noisearray[1].seed = Noisearray[1].seed;
        }
    }
    
    public void drawmapineditor()
    {
        isPlaying = false;
        mapdata mapdata = Generatemapdata(Offset);
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
    }

    public void RequestMapData(Vector2 center, Action<mapdata> callback)
    {
        ThreadStart threadstart = delegate
        {
            mapdataThread(center ,callback);
        };
        if (ThreadsActive < MaxThreads)
        {
            new Thread(threadstart).Start();
            ThreadsActive++;
        }
        else
        {
            threadPool.Enqueue(threadstart);
        }
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
        if (ThreadsActive < MaxThreads)
        {
            new Thread(threadstart).Start();
            ThreadsActive++;
        }
        else
        {
            threadPool.Enqueue(threadstart);
        }
    }
    void Meshdatathread(int LOD, mapdata mapdata, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapdata.heightmap, LOD);
        lock (meshdatathreadinfoqueue)
        {
            meshdatathreadinfoqueue.Enqueue(new MapThreadInfo<MeshData>(callback,meshData));
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
        Vector3[,] noisemap;
        if (!isPlaying)
        {
            noisemap = MapNoise.GenerateNoiseMap(MapChunkSize, center, NoiseArray);
        }
        else
        {
            noisemap = MapNoise.GenerateNoiseMap(MapChunkSize, center, noisedata);
        }
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
    public bool IsGenerating()
    {
        return ThreadsActive > 0;
    }
    public float GenerationProgress()
    {
        return (ThreadsActive + threadPool.Count) / MaxChunks;
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