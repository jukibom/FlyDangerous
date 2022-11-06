using Den.Tools;
using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using Unity.Mathematics;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine;

public class Endlessterrain : MonoBehaviour
{
    public int Inseed;
    static int seed;
    const float scale = 15;

    const float vieweroffsetthresholdforUpdate = 40f;
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
        Instancedmaterial = MapMaterial;

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
            structures = new StructureInfo[20];
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
                            }
                            else
                            {
                                meshCollider.enabled = false;
                            }
                        }
                        else if (!lODMesh.hasRequestedMesh)
                        {
                            lODMesh.RequestMesh(mapdata);
                        }
                    }
                    terrainchunksvisiblelastupdate.Add(this);

                    if(meshObject.activeSelf)
                    {
                        UpdateStructures();
                    }
                }

                Setvisible(visible);
            }
        }
        void UpdateStructures()
        {
            float viewerdstfromnearestedge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            System.Random PRNG = new System.Random(Mathf.RoundToInt(seed + position.y * position.x));
            float size = bounds.size.x;
            for (int i = 0; i < structures.Length - 1; i++)
            {
                if (!structures[i].isDefinied)
                {
                    structures[i].StructureOffset = new Vector3(PRNG.Next(Mathf.RoundToInt(size)) - size/2, PRNG.Next(Mathf.RoundToInt(size)) - size, PRNG.Next(Mathf.RoundToInt(size)) - size/2);

                    Vector3 mapOffset;
                    mapOffset = mapdata.heightmap[Mathf.RoundToInt(structures[i].StructureOffset.x + size / 2), Mathf.RoundToInt(-structures[i].StructureOffset.z + size / 2)];

                    structures[i].StructureOffset.y = mapOffset.y;
                    structures[i].StructureOffset.z = structures[i].StructureOffset.z + mapOffset.z;
                    structures[i].StructureOffset.x = structures[i].StructureOffset.x + mapOffset.x;

                    structures[i].StructureScale = new Vector3(1,1,1) * scale;
                    structures[i].StructureRotation = quaternion.LookRotation(Vector3.forward, Vector3.up);
                    structures[i].StructureID = 0;
                    structures[i].isDefinied = true;
                    structures[i].gameobject = new GameObject();
                    structures[i].gameobject.AddComponent<MeshRenderer>();
                    structures[i].gameobject.AddComponent<MeshFilter>();
                    structures[i].gameobject.GetComponent<MeshRenderer>().material = Instancedmaterial;
                    structures[i].gameobject.GetComponent<MeshFilter>().mesh = staticstructuremesh;
                    structures[i].gameobject.AddComponent<MeshCollider>();
                    structures[i].gameobject.GetComponent<MeshCollider>().sharedMesh = staticstructuremesh;
                    structures[i].gameobject.transform.parent = meshObject.transform;
                    structures[i].gameobject.transform.localScale = structures[i].StructureScale;
                    structures[i].gameobject.transform.localPosition = (structures[i].StructureOffset);
                    structures[i].gameobject.transform.rotation = structures[i].StructureRotation;

                }
                structures[i].gameobject.SetActive(viewerdstfromnearestedge < 300);
            }
        }
        public void Setvisible(bool visible)
        {
            meshObject.SetActive(visible);
        }
        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
        public int GetClosestIndex(Vector3 coord)
        {
            Mesh mesh = meshfilter.mesh;
            float closestpoint = int.MaxValue;
            int closestpointindex = 0;
            for(int i = 0; i < mesh.vertices.Length;i++)
            {
                if ((mesh.vertices[i] - coord).sqrMagnitude < closestpoint) 
                {
                    closestpoint = (mesh.vertices[i] - coord).sqrMagnitude;
                    closestpointindex = i;
                }
            }

            return closestpointindex;
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
    public struct StructureInfo
    {
        public bool isDefinied;
        public int StructureID;
        public Vector3 StructureScale;
        public Vector3 StructureOffset;
        public quaternion StructureRotation;
        public GameObject gameobject;
    }
}
