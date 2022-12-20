using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public class Endlessterrain : MonoBehaviour {
    public int Inseed;
    public float mainStructScale = 10f;
    private static float staticscale;
    private static int seed;
    private const float scale = 60;

    private const float vieweroffsetthresholdforUpdate = 1f;
    private const float sqrvieweroffsetthresholdforUpdate = vieweroffsetthresholdforUpdate * vieweroffsetthresholdforUpdate;

    public LODinfo[] detaillevels;
    public static float maxViewDist;

    public enum trackingmode {
        Transform,
        Tag
    }

    public trackingmode Trackingmode;

    public GameObject viewer;

    public Material MapMaterial;


    public static Vector2 viewerPosition;
    private Vector2 viewerpositionold;
    private static MapGenerate MapGenerator;

    private int chunkSize;
    private int chunksVisibleInViewDist;

    private Dictionary<Vector2, TerrainChunk> TerrainchunkDictionary = new();
    private static List<TerrainChunk> terrainchunksvisiblelastupdate = new();

    private GameObject taggedobj;

    public GameObject structprefab;
    public GameObject subStructprefab;

    private void Start() {
        /*
           TerrainchunkDictionary = new Dictionary<Vector2, TerrainChunk> ();
           terrainchunksvisiblelastupdate = new List<TerrainChunk> ();
           Initialize();
           */
    }

    private void Awake() {
        TerrainchunkDictionary = new Dictionary<Vector2, TerrainChunk>();
        terrainchunksvisiblelastupdate = new List<TerrainChunk>();
        Initialize();
    }

    private void Initialize() {
        staticscale = mainStructScale;

        seed = Inseed;
        GameObject.Find("Mesh").SetActive(false);


        updatedtrackedposition();

        maxViewDist = detaillevels[detaillevels.Length - 1].visibleDistThreshold;
        chunkSize = MapGenerate.MapChunkSize - 1;
        chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize + 1f);
        MapGenerator = FindObjectOfType<MapGenerate>();
        UpdateVisibleChunks();
    }

    private void updatedtrackedposition() {
        taggedobj = GameObject.FindGameObjectWithTag("Terrain Gen Marker");

        if (Trackingmode == trackingmode.Transform || taggedobj == null) {
            viewerPosition = new Vector2(viewer.transform.position.x, viewer.transform.position.z) / scale;
        }
        else {
            viewerPosition = new Vector2(taggedobj.transform.position.x,
                taggedobj.transform.position.z);
            viewerPosition -= new Vector2(gameObject.transform.position.x - gameObject.transform.localPosition.x,
                gameObject.transform.position.z - gameObject.transform.localPosition.z);
            viewerPosition /= scale;
        }
    }

    private void Update() {
        updatedtrackedposition();

        if ((viewerpositionold - viewerPosition).sqrMagnitude > sqrvieweroffsetthresholdforUpdate) {
            if (taggedobj.transform.position.sqrMagnitude > 625 && taggedobj.transform.position.sqrMagnitude < 950625) {
                viewerpositionold = viewerPosition;
                UpdateVisibleChunks();
            }
            else {
                UpdateVisibleChunks();
            }
        }
    }


    private void UpdateVisibleChunks() {
        for (var i = 0; i < terrainchunksvisiblelastupdate.Count; i++) terrainchunksvisiblelastupdate[i].Setvisible(false);
        terrainchunksvisiblelastupdate.Clear();
        var currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        var currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (var yOffset = -chunksVisibleInViewDist; yOffset <= chunksVisibleInViewDist; yOffset++)
        for (var xOffset = -chunksVisibleInViewDist; xOffset <= chunksVisibleInViewDist; xOffset++) {
            var viewdChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
            if (TerrainchunkDictionary.ContainsKey(viewdChunkCoord)) {
                TerrainchunkDictionary[viewdChunkCoord].UpdateTerrainChunk();
                if (TerrainchunkDictionary[viewdChunkCoord].IsVisible())
                    TerrainchunkDictionary[viewdChunkCoord].UpdatePosition(viewdChunkCoord, chunkSize, transform);
            }
            else {
                Profiler.BeginSample("Generating New Chunk");
                TerrainchunkDictionary.Add(viewdChunkCoord,
                    new TerrainChunk(viewdChunkCoord, chunkSize, detaillevels, transform, MapMaterial, structprefab, subStructprefab));
                Profiler.EndSample();
            }
        }
    }

    public class TerrainChunk {
        private readonly GameObject meshObject;
        private Vector2 position;
        private Bounds bounds;
        private readonly MeshRenderer meshrenderer;
        private readonly MeshFilter meshfilter;
        private readonly MeshCollider meshCollider;
        private readonly LODinfo[] detaillevels;
        private readonly LODMesh[] lodmeshes;

        private readonly float height;

        private mapdata mapdata;
        private bool mapdataRecieved;
        private int previousLODIndex = -1;

        public GameObject Structprefab;
        public GameObject subStructPrefab;

        private readonly StructureInfo[] structures;

        public TerrainChunk(Vector2 coord, int size, LODinfo[] detaillevels, Transform parent, Material mat, GameObject structToInstance,
            GameObject subStruct) {
            this.detaillevels = detaillevels;

            Structprefab = structToInstance;
            subStructPrefab = subStruct;

            height = 0;

            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            var positionV3 = new Vector3(position.x, 0, position.y);

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
            for (var i = 0; i < detaillevels.Length; i++) lodmeshes[i] = new LODMesh(detaillevels[i].LOD, UpdateTerrainChunk);

            MapGenerator.RequestMapData(position, OnMapDataRecieved);
            structures = new StructureInfo[120];
        }

        public void UpdatePosition(Vector2 coord, int size, Transform parent) {
            position = coord * size;
            var positionV3 = new Vector3(position.x, height, position.y);

            meshObject.transform.localPosition = positionV3 * scale;
            meshObject.transform.parent = parent;
        }

        private void OnMapDataRecieved(mapdata mapdata) {
            this.mapdata = mapdata;
            mapdataRecieved = true;

            var texture = TextureGenerator.TexturefromeColormap(mapdata.colormap, MapGenerate.MapChunkSize, MapGenerate.MapChunkSize);
            meshrenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk() {
            if (mapdataRecieved) {
                var viewerdstfromnearestedge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                var visible = viewerdstfromnearestedge <= maxViewDist;

                if (visible) {
                    var Lodindex = 0;
                    for (var i = 0; i < detaillevels.Length - 1; i++)
                        if (viewerdstfromnearestedge > detaillevels[i].visibleDistThreshold)
                            Lodindex = i + 1;
                        else
                            break;
                    if (Lodindex != previousLODIndex) {
                        var lODMesh = lodmeshes[Lodindex];
                        if (lODMesh.hasmesh) {
                            previousLODIndex = Lodindex;
                            meshfilter.mesh = lODMesh.mesh;
                            meshCollider.sharedMesh = lODMesh.mesh;
                            if (Lodindex == 0) {
                                meshCollider.enabled = true;

                                UpdateStructures(true);
                            }
                            else {
                                meshCollider.enabled = false;

                                UpdateStructures(false);
                            }
                        }
                        else if (!lODMesh.hasRequestedMesh) {
                            lODMesh.RequestMesh(mapdata);
                        }
                    }

                    terrainchunksvisiblelastupdate.Add(this);
                }

                Setvisible(visible);
            }
        }

        private void UpdateStructures(bool Collision) {
            if (structures[0] == null) {
                var PRNG = new PRNG(Mathf.RoundToInt(seed + position.y * position.x));
                var size = bounds.size.x;
                var lastposition = new Vector3();
                for (var i = 0; i < structures.Length - 1; i++)
                    if (i % 40 == 0) {
                        lastposition = new Vector3(PRNG.NextInt(Mathf.RoundToInt(size), 0) - size / 2, PRNG.NextInt(300, 0) - 300,
                            PRNG.NextInt(Mathf.RoundToInt(size), 0) - size / 2);
                        InitializeStructure(false, i, staticscale, PRNG, size, lastposition);
                        lastposition = structures[i].StructureOffset;
                    }
                    else {
                        var nearposition = new Vector3(PRNG.NextFloat() - 0.5f, PRNG.NextFloat() - 0.5f, PRNG.NextFloat() - 0.5f) * 70f;

                        InitializeStructure(true, i, math.pow(Mathf.Clamp01(1f - nearposition.magnitude * 0.022f), 2) * 8f, PRNG, size,
                            nearposition + lastposition);
                    }
            }

            for (var i = 0; i < structures.Length - 1; i++)
                if (structures[i].isActive != Collision) {
                    structures[i].isActive = Collision;
                    structures[i].gameObject.SetActive(Collision);
                }
        }

        public void InitializeStructure(bool isSimple, int Index, float structscale, PRNG Seed, float size, Vector3 startingPoint) {
            structures[Index] = new StructureInfo();
            structures[Index].StructureOffset = startingPoint;
            var pointInfo = new pointInfo();

            pointInfo.SetClosestPoint(startingPoint, meshfilter.mesh);

            structures[Index].StructureOffset = pointInfo.position;

            structures[Index].StructureRotation =
                quaternion.LookRotation(
                    pointInfo.tangent * (Seed.NextFloat() - 0.5f) * 2f + Vector3.Cross(pointInfo.tangent, pointInfo.normal) * (Seed.NextFloat() - 0.5f) * 3,
                    pointInfo.normal);
            structures[Index].StructureScale = Vector3.one * structscale;
            structures[Index].StructureID = 0;
            structures[Index].isDefinied = true;
            structures[Index].isCube = isSimple;


            structures[Index].parent = meshObject.transform;

            if (!isSimple)
                structures[Index].prefab = Instantiate(Structprefab);
            else
                structures[Index].prefab = Instantiate(subStructPrefab);
            structures[Index].SetObject();
        }

        public void Setvisible(bool visible) {
            meshObject.SetActive(visible);
        }

        public bool IsVisible() {
            return meshObject.activeSelf;
        }
    }

    private class LODMesh {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasmesh;
        private readonly int lod;
        private readonly Action updateCallback;

        public LODMesh(int Lod, Action updateCallback) {
            lod = Lod;
            this.updateCallback = updateCallback;
        }

        private void OnMeshDataRecieved(MeshData meshdata) {
            mesh = meshdata.createmesh();
            hasmesh = true;

            updateCallback();
        }

        public void RequestMesh(mapdata mapdata) {
            hasRequestedMesh = true;
            MapGenerator.RequestMeshData(lod, mapdata, OnMeshDataRecieved);
        }
    }

    [Serializable]
    public struct LODinfo {
        public int LOD;
        public float visibleDistThreshold;
    }

    [Serializable]
    public class StructureInfo {
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

        public void SetObject() {
            gameObject = prefab;
            gameObject.transform.SetParent(parent);
            gameObject.transform.parent = parent;
            gameObject.transform.localPosition = StructureOffset;
            gameObject.transform.rotation = StructureRotation;
            gameObject.transform.localScale = StructureScale;
            gameObject.SetActive(isActive);
        }
    }

    public class pointInfo {
        public Vector3 position;
        public Vector3 tangent;
        public Vector3 normal;

        public void SetClosestPoint(Vector3 startingPoint, Mesh mesh) {
            var closestdistances = new float[3];
            Array.Fill(closestdistances, int.MaxValue);
            var closestindecies = new int[3];
            var vertices = mesh.vertices;


            for (var i = 0; i < vertices.Length; i++) {
                var dist = (vertices[i] - startingPoint).magnitude;
                if (dist < closestdistances[0]) {
                    closestdistances[2] = closestdistances[1];
                    closestindecies[2] = closestindecies[1];
                    closestdistances[1] = closestdistances[0];
                    closestindecies[1] = closestindecies[0];
                    closestindecies[0] = i;
                    closestdistances[0] = dist;
                }
                else if (dist < closestdistances[1]) {
                    closestdistances[2] = closestdistances[1];
                    closestindecies[2] = closestindecies[1];
                    closestindecies[1] = i;
                    closestdistances[1] = dist;
                }
                else if (dist < closestdistances[2]) {
                    closestindecies[2] = i;
                    closestdistances[2] = dist;
                }
            }

            var indexweights = new float[3];
            float totalweight = 0;
            for (var i = 0; i < indexweights.Length; i++) {
                indexweights[i] = 1f / closestdistances[i];
                totalweight += indexweights[i];
            }

            var tangents = mesh.tangents;
            var normals = mesh.normals;

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

    public class PRNG {
        private int seed;
        private int iterator;

        public PRNG(int seed) {
            this.seed = seed;
        }

        public int NextInt(int Max, int Min) {
            var num = Next();
            num = math.abs(num % (Max + Min)) + Min;
            return num;
        }

        public int Next() {
            Profiler.BeginSample("PRNG Next");
            seed += 5321;

            var num = seed;
            num = (seed >> (iterator % 32)) ^ (seed << (5 + num));
            num = (seed >> ((~iterator + 7) % 32)) ^ num;

            iterator = num;
            Profiler.EndSample();
            return seed;
        }

        public float NextFloat() {
            Profiler.BeginSample("PRNG NextFloat");
            float num = Next();
            num = math.abs(Next() / 1.65486532f % 1f);
            Profiler.EndSample();
            return num;
        }
    }
}