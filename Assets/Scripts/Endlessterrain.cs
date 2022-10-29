using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Endlessterrain : MonoBehaviour
{
    const float scale = 15;

    const float vieweroffsetthresholdforUpdate = 5f;
    const float sqrvieweroffsetthresholdforUpdate = vieweroffsetthresholdforUpdate * vieweroffsetthresholdforUpdate;

    public LODinfo[] detaillevels;
    public static float maxViewDist;

    public enum trackingmode { Transform, Tag}
    public trackingmode Trackingmode;
    public Transform viewer;

    public Material MapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerpositionold;
    static MapGenerate MapGenerator;

    int chunkSize;
    int chunksVisibleInViewDist;

    Dictionary<Vector2,TerrainChunk> TerrainchunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainchunksvisiblelastupdate = new List<TerrainChunk>();

    void Start()
    {
        maxViewDist = detaillevels[detaillevels.Length-1].visibleDistThreshold;
        chunkSize = MapGenerate.MapChunkSize - 1;
        chunksVisibleInViewDist = Mathf.RoundToInt((maxViewDist / chunkSize)+1f);
        MapGenerator = FindObjectOfType<MapGenerate>();
        UpdateVisibleChunks();
    }

    void Update()
    {
        if (Trackingmode == trackingmode.Transform)
        {
            viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / scale;
        }
        else
        {
            viewerPosition = new Vector2(GameObject.FindGameObjectWithTag("Terrain Gen Marker").transform.position.x,
            GameObject.FindGameObjectWithTag("Terrain Gen Marker").transform.position.z);
            viewerPosition -= new Vector2(gameObject.transform.position.x - gameObject.transform.localPosition.x, gameObject.transform.position.z - gameObject.transform.localPosition.z);
            viewerPosition /= scale;
        }
        if ((viewerpositionold - viewerPosition).sqrMagnitude > sqrvieweroffsetthresholdforUpdate)
        {
            viewerpositionold = viewerPosition;
            UpdateVisibleChunks();
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

        mapdata mapdata;
        bool mapdataRecieved = false;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODinfo[] detaillevels, Transform parent, Material mat)
        {
            this.detaillevels = detaillevels;
            
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");

            meshrenderer = meshObject.AddComponent<MeshRenderer>();
            meshrenderer.material = mat;
            meshfilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            
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
        }
        public void UpdatePosition(Vector2 coord,int size, Transform parent)
        {
            position = coord * size;
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

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
                        }
                        else if (!lODMesh.hasREquestedMesh)
                        {
                            lODMesh.RequestMesh(mapdata);
                        }
                    }
                    terrainchunksvisiblelastupdate.Add(this);
                }

                Setvisible(visible);
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
    }
    class LODMesh
    {
        public Mesh mesh;
        public bool hasREquestedMesh;
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
            hasREquestedMesh = true;
            MapGenerator.RequestMeshData(lod, mapdata, OnMeshDataRecieved);

        }
    }
    [System.Serializable]
    public struct LODinfo
    {
        public int LOD;
        public float visibleDistThreshold;


    }
}
