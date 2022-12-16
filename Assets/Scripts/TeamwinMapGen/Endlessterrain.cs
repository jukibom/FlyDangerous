using Den.Tools;
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using Unity.Mathematics;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;

public class Endlessterrain : MonoBehaviour
{
    public int Inseed;
    public float mainStructScale = 10f;
    static float staticscale;
    static int seed;
    const float scale = 60;

    const float vieweroffsetthresholdforUpdate = 1f;
    const float sqrvieweroffsetthresholdforUpdate = vieweroffsetthresholdforUpdate * vieweroffsetthresholdforUpdate;

    public LODinfo[] detaillevels;
    public static float maxViewDist;

    public enum trackingmode { Transform, Tag}
    public trackingmode Trackingmode;

    public GameObject viewer;

    public Material MapMaterial;


    public static Vector2 viewerPosition;
    Vector2 viewerpositionold;
    static MapGenerate MapGenerator;

    int chunkSize;
    int chunksVisibleInViewDist;

    Dictionary<Vector2,TerrainChunk> TerrainchunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainchunksvisiblelastupdate = new List<TerrainChunk>();

    GameObject taggedobj;

    public GameObject structprefab;
    public GameObject subStructprefab;

    void Start()
    {
     /*
        TerrainchunkDictionary = new Dictionary<Vector2, TerrainChunk> ();
        terrainchunksvisiblelastupdate = new List<TerrainChunk> ();
        Initialize();
        */
    }

    void Awake()
    {
        TerrainchunkDictionary = new Dictionary<Vector2, TerrainChunk>();
        terrainchunksvisiblelastupdate = new List<TerrainChunk>();
        Initialize();
    }

    void Initialize()
    {


        staticscale = mainStructScale;

        seed = Inseed;
        GameObject.Find("Mesh").SetActive(false);


        updatedtrackedposition();

        maxViewDist = detaillevels[detaillevels.Length - 1].visibleDistThreshold;
        chunkSize = MapGenerate.MapChunkSize - 1;
        chunksVisibleInViewDist = Mathf.RoundToInt((maxViewDist / chunkSize) + 1f);
        MapGenerator = FindObjectOfType<MapGenerate>();
        UpdateVisibleChunks();
    }

    void updatedtrackedposition()
    {
        taggedobj = GameObject.FindGameObjectWithTag("Terrain Gen Marker");

        if (Trackingmode == trackingmode.Transform || taggedobj == null)
        {
            viewerPosition = new Vector2(viewer.transform.position.x, viewer.transform.position.z) / scale;
        }
        else
        {
            viewerPosition = new Vector2(taggedobj.transform.position.x,
            taggedobj.transform.position.z);
            viewerPosition -= new Vector2(gameObject.transform.position.x - gameObject.transform.localPosition.x, gameObject.transform.position.z - gameObject.transform.localPosition.z);
            viewerPosition /= scale;
        }
    }

    void Update()
    {
        updatedtrackedposition();

        if ((viewerpositionold - viewerPosition).sqrMagnitude > sqrvieweroffsetthresholdforUpdate)
        {
            
            if (taggedobj.transform.position.sqrMagnitude > 625 && taggedobj.transform.position.sqrMagnitude < 950625)
            {
                viewerpositionold = viewerPosition;
                UpdateVisibleChunks();
            }
            else
            {
                UpdateVisibleChunks();
            }
        }
    }


    void UpdateVisibleChunks()
    {
        for(int i = 0; i < terrainchunksvisiblelastupdate.Count; i++)
        {
            terrainchunksvisiblelastupdate[i].Setvisible(false);
        }
        terrainchunksvisiblelastupdate.Clear();
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDist; yOffset <= chunksVisibleInViewDist; yOffset++)
        {

            for (int xOffset = -chunksVisibleInViewDist; xOffset <= chunksVisibleInViewDist; xOffset++)
            {
                Vector2 viewdChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (TerrainchunkDictionary.ContainsKey(viewdChunkCoord))
                {
                    TerrainchunkDictionary[viewdChunkCoord].UpdateTerrainChunk();
                    if (TerrainchunkDictionary[viewdChunkCoord].IsVisible())
                    {
                        TerrainchunkDictionary[viewdChunkCoord].UpdatePosition(viewdChunkCoord, chunkSize, transform);
                    }
                }
                else
                {
                    Profiler.BeginSample("Generating New Chunk");
                    TerrainchunkDictionary.Add(viewdChunkCoord, new TerrainChunk(viewdChunkCoord,chunkSize,detaillevels ,transform,MapMaterial,structprefab, subStructprefab));
                    Profiler.EndSample();
                }
            }
        }
    }
    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;
        MeshRenderer meshrenderer;
        MeshFilter meshfilter;
        MeshCollider meshCollider;
        LODinfo[] detaillevels;
        LODMesh[] lodmeshes;

        float height;

        mapdata mapdata;
        bool mapdataRecieved = false;
        int previousLODIndex = -1;

        public GameObject Structprefab;
        public GameObject subStructPrefab;

        readonly StructureInfo[] structures;

        public TerrainChunk(Vector2 coord, int size, LODinfo[] detaillevels, Transform parent, Material mat, GameObject structToInstance, GameObject subStruct)
        {
            this.detaillevels = detaillevels;

            Structprefab = structToInstance;
            subStructPrefab = subStruct;

            height = 0;
            
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");

            meshrenderer = meshObject.AddComponent<MeshRenderer>();
            meshrenderer.material = mat;
            meshfilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshObject.layer = 11;
            
            meshObject.transform.localPosition = positionV3 * scale;
            meshObject.transform.parent = parent;

            meshObject.transform.localScale = Vector3.one * scale;

            Setvisible(false);

            lodmeshes = new LODMesh[detaillevels.Length];
            for (int i = 0; i < detaillevels.Length; i++)
            {
                lodmeshes[i] = new LODMesh(detaillevels[i].LOD,UpdateTerrainChunk);
            }

            MapGenerator.RequestMapData( position ,OnMapDataRecieved);
            structures = new StructureInfo[120];
        }
        public void UpdatePosition(Vector2 coord,int size, Transform parent)
        {
            position = coord * size;
            Vector3 positionV3 = new Vector3(position.x, height, position.y);

            meshObject.transform.localPosition = positionV3 * scale;
            meshObject.transform.parent = parent;
        } 
        void OnMapDataRecieved(mapdata mapdata)
        {
            this.mapdata = mapdata;
            mapdataRecieved = true;

            Texture2D texture = TextureGenerator.TexturefromeColormap(mapdata.colormap,MapGenerate.MapChunkSize,MapGenerate.MapChunkSize);
            meshrenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }
        public void UpdateTerrainChunk()
        {
            if (mapdataRecieved)
            {

                float viewerdstfromnearestedge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = (viewerdstfromnearestedge) <= maxViewDist;

                if (visible)
                {
                    int Lodindex = 0;
                    for (int i = 0; i < detaillevels.Length - 1; i++)
                    {
                        if (viewerdstfromnearestedge > detaillevels[i].visibleDistThreshold)
                        {
                            Lodindex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (Lodindex != previousLODIndex)
                    {
                        LODMesh lODMesh = lodmeshes[Lodindex];
                        if (lODMesh.hasmesh)
                        {
                            previousLODIndex = Lodindex;
                            meshfilter.mesh = lODMesh.mesh;
                            meshCollider.sharedMesh = lODMesh.mesh;
                            if (Lodindex == 0)
                            {
                                meshCollider.enabled = true;

                                UpdateStructures(true);

                            }
                            else
                            {
                                meshCollider.enabled = false;

                                UpdateStructures(false);
                            }
                            
                        }
                        else if (!lODMesh.hasRequestedMesh)
                        {
                            lODMesh.RequestMesh(mapdata);
                        }
                    }

                    terrainchunksvisiblelastupdate.Add(this);
                }

                Setvisible(visible);
            }
        }
        void UpdateStructures(bool Collision)
        {
            if (structures[0] == null)
            {
                PRNG PRNG = new PRNG(Mathf.RoundToInt(seed + position.y * position.x));
                float size = bounds.size.x;
                Vector3 lastposition = new Vector3();
                for (int i = 0; i < structures.Length - 1; i++)
                {
                    if (i % 40 == 0)
                    {
                        lastposition = new Vector3(PRNG.NextInt(Mathf.RoundToInt(size), 0) - size / 2, PRNG.NextInt(300, 0) - 300, PRNG.NextInt(Mathf.RoundToInt(size), 0) - size / 2);
                        InitializeStructure(false, i, staticscale, PRNG, size, lastposition);
                        lastposition = structures[i].StructureOffset;
                    }
                    else
                    {
                        Vector3 nearposition = new Vector3((float)PRNG.NextFloat() - 0.5f, (float)PRNG.NextFloat() - 0.5f, (float)PRNG.NextFloat() - 0.5f) * 70f;

                        InitializeStructure(true, i, math.pow(Mathf.Clamp01(1f - nearposition.magnitude * 0.022f), 2) * 8f, PRNG, size, nearposition + lastposition);
                    }
                }
            }

            for (int i = 0; i < structures.Length - 1; i++)
            {
                if (structures[i].isActive != Collision)
                {
                    structures[i].isActive = Collision;
                    structures[i].gameObject.SetActive(Collision);
                }
            }
            
        }
        public void InitializeStructure(bool isSimple,int Index, float structscale,PRNG Seed, float size, Vector3 startingPoint)
        {

            structures[Index] = new StructureInfo();
            structures[Index].StructureOffset = startingPoint;
            pointInfo pointInfo = new pointInfo();

            pointInfo.SetClosestPoint(startingPoint, meshfilter.mesh);

            structures[Index].StructureOffset = pointInfo.position;

            structures[Index].StructureRotation = quaternion.LookRotation(pointInfo.tangent * (Seed.NextFloat()-0.5f) * 2f + Vector3.Cross(pointInfo.tangent,pointInfo.normal)*((float)Seed.NextFloat()-0.5f)*3,pointInfo.normal);
            structures[Index].StructureScale = Vector3.one * structscale;
            structures[Index].StructureID = 0;
            structures[Index].isDefinied = true;
            structures[Index].isCube = isSimple;


            structures[Index].parent = meshObject.transform;

            if (!isSimple)
            {
                structures[Index].prefab = (GameObject)Instantiate(Structprefab);
            }
            else
            {
                structures[Index].prefab = (GameObject)Instantiate(subStructPrefab);
            }
            structures[Index].SetObject();

        }
        public void Setvisible(bool visible)
        {
            meshObject.SetActive(visible);
        }
        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }

    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasmesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int Lod, System.Action updateCallback)
        {
            this.lod = Lod;
            this.updateCallback = updateCallback;
        }
        void OnMeshDataRecieved(MeshData meshdata)
        {
            mesh = meshdata.createmesh();
            hasmesh = true;

            updateCallback();
        }
        public void RequestMesh(mapdata mapdata)
        {
            hasRequestedMesh = true;
            MapGenerator.RequestMeshData(lod, mapdata, OnMeshDataRecieved);

        }
    }
    [System.Serializable]
    public struct LODinfo
    {
        public int LOD;
        public float visibleDistThreshold;
    }
    [System.Serializable]
    public class StructureInfo
    {
        public GameObject prefab;

        public bool isDefinied;
        public int StructureID;
        public Vector3 StructureScale;
        public Vector3 StructureOffset;
        public quaternion StructureRotation;
        public Mesh mesh;
        public Material material;
        public Transform parent;
        public bool isActive;
        public GameObject gameObject;
        public bool isCube;
        public void SetObject()
        {

                
                gameObject = prefab;
                gameObject.transform.SetParent(parent);
                gameObject.transform.parent = parent;
                gameObject.transform.localPosition = StructureOffset;
                gameObject.transform.rotation = StructureRotation;
                gameObject.transform.localScale = StructureScale;
                gameObject.SetActive(isActive);
            
        }
    }
    public class pointInfo
    {
        public Vector3 position;
        public Vector3 tangent;
        public Vector3 normal;

        public void SetClosestPoint(Vector3 startingPoint, Mesh mesh)
        {
            float[] closestdistances = new float[3];
            Array.Fill(closestdistances, int.MaxValue);
            int[] closestindecies = new int[3];
            Vector3[] vertices = mesh.vertices;



            for (int i = 0; i < vertices.Length; i++)
            {
                float dist = (vertices[i] - startingPoint).magnitude;
                if (dist < closestdistances[0])
                {
                    closestdistances[2] = closestdistances[1];
                    closestindecies[2] = closestindecies[1];
                    closestdistances[1] = closestdistances[0];
                    closestindecies[1] = closestindecies[0];
                    closestindecies[0] = i;
                    closestdistances[0] = dist;
                }
                else if(dist < closestdistances[1])
                {
                    closestdistances[2] = closestdistances[1];
                    closestindecies[2] = closestindecies[1];
                    closestindecies[1] = i;
                    closestdistances[1] = dist;
                }
                else if(dist < closestdistances[2])
                {
                    closestindecies[2] = i;
                    closestdistances[2] = dist;
                }
            }

            float[] indexweights = new float[3];
            float totalweight = 0;
            for (int i = 0; i < indexweights.Length; i++)
            {
                indexweights[i] = 1f / closestdistances[i];
                totalweight += indexweights[i];
            }

            Vector4[] tangents = mesh.tangents;
            Vector3[] normals = mesh.normals;

            tangent =
                (tangents[closestindecies[0]] * indexweights[0] + 
                tangents[closestindecies[1]] * indexweights[1] +
                tangents[closestindecies[2]] * indexweights[2]) / totalweight;
            normal =
                (normals[closestindecies[0]] * indexweights[0] +
                normals[closestindecies[1]] * indexweights[1] +
                normals[closestindecies[2]] * indexweights[2]) / totalweight;
            position =
               (vertices[closestindecies[0]] * indexweights[0] +
               vertices[closestindecies[1]] * indexweights[1] +
               vertices[closestindecies[2]] * indexweights[2]) / totalweight;

        }

    }
    public class PRNG
    {
        int seed;
        int iterator = 0;

        public PRNG(int seed)
        {
            this.seed = seed;
        }
        public int NextInt(int Max, int Min)
        {
            int num = Next();
            num = math.abs(num % (Max + Min)) + Min;
            return num;
            
        }
        public int Next()
        {
            Profiler.BeginSample("PRNG Next");
            seed += 5321;

            int num = seed;
            num = seed >> (iterator % 32) ^ seed << 5 + num;
            num = seed >> ((~iterator + 7) % 32) ^ num;

            iterator = num;
            Profiler.EndSample();
            return seed;
        }
        public float NextFloat()
        {
            Profiler.BeginSample("PRNG NextFloat");
            float num = Next();
            num = math.abs((Next() / 1.65486532f) % 1f);
            Profiler.EndSample();
            return num;
        }

    }
}
