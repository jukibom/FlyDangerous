using Den.Tools;
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using Unity.Mathematics;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine;
using UnityEngine.Profiling;

public class Endlessterrain : MonoBehaviour
{
    public int Inseed;
    static int seed;
    const float scale = 15;

    const float vieweroffsetthresholdforUpdate = 10f;
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
    public Mesh meshtoinstance;
    static Mesh staticstructuremesh;
    public Mesh nearInstance;
    static Mesh staticNear;
    public Material MaterialtoInstance;
    static Material Instancedmaterial;

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
        staticstructuremesh = meshtoinstance;
        Instancedmaterial = MaterialtoInstance;
        staticNear = nearInstance;

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
                print("updated at distance" + taggedobj.transform.position.sqrMagnitude);
                viewerpositionold = viewerPosition;
                UpdateVisibleChunks();
            }
            else
            {
                print(taggedobj.transform.position.sqrMagnitude);
             //   UpdateVisibleChunks();
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
                    TerrainchunkDictionary.Add(viewdChunkCoord, new TerrainChunk(viewdChunkCoord,chunkSize,detaillevels ,transform,MapMaterial));
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

        

        StructureInfo[] structures;

        public TerrainChunk(Vector2 coord, int size, LODinfo[] detaillevels, Transform parent, Material mat)
        {
            this.detaillevels = detaillevels;

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
            structures = new StructureInfo[400];
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
                bool visible = viewerdstfromnearestedge <= maxViewDist;

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
            Profiler.BeginSample("setPRNG");
            PRNG PRNG = new PRNG(Mathf.RoundToInt(seed + position.y * position.x));
            Profiler.EndSample();
            float size = bounds.size.x;
            Vector3 lastposition = new Vector3();
            Profiler.BeginSample("start Structure Loop");
            for (int i = 0; i < structures.Length - 1; i++)
            {

                if (structures[i] == null)
                {
                    
                    if(i%40 == 0)
                    {
                        lastposition = new Vector3(PRNG.Next(Mathf.RoundToInt(size),0) - size / 2, PRNG.Next(300,0)-300, PRNG.Next(Mathf.RoundToInt(size),0) - size / 2);
                        InitializeStructure(false,staticstructuremesh,i, 10f, PRNG, size,lastposition);
                        lastposition = structures[i].StructureOffset;
                    }
                    else
                    {
                        Vector3 nearposition = new Vector3((float)PRNG.NextFloat()-0.5f, (float)PRNG.NextFloat() - 0.5f,(float)PRNG.NextFloat()-0.5f) *70f;

                        InitializeStructure(true,staticNear,i,math.pow(Mathf.Clamp01(1f - nearposition.magnitude * 0.022f),2) * 8f, PRNG, size,nearposition + lastposition);
                    }
                   
                }

                structures[i].isActive = Collision;
                structures[i].gameObject.SetActive(Collision);

            }
            Profiler.EndSample();
        }
        public void InitializeStructure(bool isSimple, Mesh meshToUse,int Index, float structscale,PRNG Seed, float size, Vector3 startingPoint)
        {
            Profiler.BeginSample("InitalizeStructures");
            structures[Index] = new StructureInfo();
            structures[Index].StructureOffset = startingPoint;
            pointInfo pointInfo = new pointInfo();
            Profiler.BeginSample("GetClosestPoint");
            pointInfo.SetClosestPoint(startingPoint, meshfilter.mesh);
            Profiler.EndSample();
            Profiler.BeginSample("Set values");
            structures[Index].StructureOffset = pointInfo.position;
            //Vector3 mapOffset = mapdata.heightmap[Mathf.RoundToInt(structures[Index].StructureOffset.x + size / 2), Mathf.RoundToInt(-structures[Index].StructureOffset.z + size / 2)];

           // structures[Index].StructureOffset.y = mapOffset.y;
            //structures[Index].StructureOffset.z = structures[Index].StructureOffset.z + mapOffset.z;
            //structures[Index].StructureOffset.x = structures[Index].StructureOffset.x + mapOffset.x;

            
            structures[Index].StructureRotation = quaternion.LookRotation(pointInfo.tangent + Vector3.Cross(pointInfo.tangent,pointInfo.normal)*((float)Seed.NextFloat()-0.5f),pointInfo.normal);
            structures[Index].StructureScale = Vector3.one * structscale;
            structures[Index].StructureID = 0;
            structures[Index].isDefinied = true;
            structures[Index].isCube = isSimple;

            structures[Index].material = Instancedmaterial;
            structures[Index].mesh = meshToUse;

            structures[Index].parent = meshObject.transform;

            structures[Index].SetObject();
            Profiler.EndSample();
            Profiler.EndSample();
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
            gameObject = new GameObject("Structure");

            MeshFilter filter = gameObject.GetAddComponent<MeshFilter>(); filter.mesh = mesh;
            MeshCollider collider = gameObject.GetAddComponent<MeshCollider>();
            if (isCube)
            {
                collider.convex = true;
            }
            else
            {
                collider.convex = false;
            }
            collider.sharedMesh = mesh;
            MeshRenderer renderer = gameObject.GetAddComponent<MeshRenderer>(); renderer.material = material;
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

            Profiler.BeginSample("finding points");

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
            Profiler.EndSample();
            Profiler.BeginSample("Setting Normals and tangents");
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
            Profiler.EndSample();
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
        public int Next(int Max, int Min)
        {
            seed+=5321;

            int num = seed >> (iterator % 32) ^ ~seed;
            num = seed>>((~iterator + 7)%32) ^ num;
            iterator = num;
            num = math.abs(num % (Max + Min)) + Min;
            return num;
            
        }
        public float NextFloat()
        {
            return math.abs((Next(int.MaxValue,0)/2363f) % 1f);
        }

    }
}
