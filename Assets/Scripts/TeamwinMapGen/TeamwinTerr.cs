using Den.Tools;
using Den.Tools.GUI;
using JetBrains.Annotations;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Mathematics;
using Unity.XR.CoreUtils;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class TeamwinTerr : MonoBehaviour
{
    public float freq;
    public float amp;
    public int detailin;
    public float yoffset;
    public float canyonwidth;
    public Mesh instancedmesh;
    public GameObject terrainempty;

    Vector3[] vertices;
    public float scale;
    public MeshFilter MF;
    public Vector2 cellcount;

    public Vector4 LODdists;

    // Start is called before the first frame update
    void Start()
    {



    }

    // Update is called once per frame
    void Update()
    {

        if (transform.hasChanged)
        {

  //          GenerateTerrain();
            transform.hasChanged = false;
        }

    }

    public void GenerateTerrain()
    {

        if (cellcount.x < 1)
        {
            cellcount.x = 1;
        }
        if (cellcount.y < 1)
        {
            cellcount.y = 1;
        }
        Mesh newmesh = new Mesh();


        GameObject taggedobj = GameObject.FindGameObjectWithTag("Terrain Gen Marker");
        Vector3 taggedobjpos;
        if (taggedobj != null)
        {
            taggedobjpos = taggedobj.transform.position;
        }
        else
        {
            taggedobjpos = new Vector3(0, 0, 0);
        }

        Vector2 actualcelloffset;

        actualcelloffset = new Vector2(cellcount.x / 2 + gameObject.transform.position.x / (2 * scale), cellcount.y / 2 + gameObject.transform.position.z / (2 * scale));



        float snappingval = 1;
        actualcelloffset.x = math.round(actualcelloffset.x / snappingval) * snappingval;
        actualcelloffset.y = math.round(actualcelloffset.y / snappingval) * snappingval;



        CombineInstance[] combineInstance = new CombineInstance[(int)(cellcount.x * cellcount.y)];
      
        /*
        
        cell[] mycells = new cell[(int)(cellcount.x * cellcount.y)];
        // Generate individual cells
        for (int i = 0; i < cellcount.x; i++)
        {
            for (int j = 0; j < cellcount.y; j++)
            {
                int Index = j + i * (int)cellcount.y;


                mycells[Index] = new cell();
                mycells[Index].cellpos = new Vector2(i - actualcelloffset.x, j - actualcelloffset.y);
                mycells[Index].tagpos = taggedobjpos;
                Mesh tempmesh = null;

                var lambdathread = new Thread(
                    () =>
                    {
                        tempmesh = generatecell(mycells[Index].cellpos,mycells[Index].tagpos);
                    });

                lambdathread.Start();
                lambdathread.Join();

                mycells[Index].mesh = tempmesh;

                Thread thr = new Thread(threadtest);
                thr.Start(mycells[Index]);

            }

        }
        // check cell status
        bool AREDEFINED = false;
        while (!AREDEFINED)
        {
            AREDEFINED = true;
            new WaitForSeconds(1);
            for (int i = 0; i < mycells.Length; i++)
            {
                if (mycells[i].mesh == null)
                {
                    AREDEFINED = false;
                }


            }
            Debug.Log(AREDEFINED);
        }
        */
        
        // hope for the best
        for (int i = 0; i < cellcount.x; i++)
        {
            for (int j = 0; j < cellcount.y; j++)
            {
                int Index = j + i * (int)cellcount.y;

                combineInstance[Index].mesh = generatecell(new Vector2(i - actualcelloffset.x, j - actualcelloffset.y), taggedobjpos);
            }

        }

        newmesh.indexFormat = IndexFormat.UInt32;
        newmesh.CombineMeshes(combineInstance, true, false);
        newmesh.RecalculateNormals();
        newmesh.RecalculateTangents();

        Mesh basemesh = newmesh;

        CombineInstance[] newinstances = new CombineInstance[2];
        newinstances[1].mesh = basemesh;


        Mesh cube = new Mesh();
        cube.vertices = instancedmesh.vertices;
        cube.uv = instancedmesh.uv;
        cube.triangles = instancedmesh.triangles;
        cube.normals = instancedmesh.normals;
        cube.tangents = instancedmesh.tangents;
        newinstances[0].mesh = instanceonpoints(cube, distributepoints(basemesh, -0.5f, 500));
      //  newinstances[0].mesh.indexFormat = IndexFormat.UInt32;
        Mesh combined = new Mesh();
        combined.indexFormat = IndexFormat.UInt32;
        combined.CombineMeshes(newinstances, true, false);

        MF.mesh = combined;
        gameObject.GetComponent<MeshCollider>().sharedMesh = combined;
        newmesh = null;
        combined = null;

       

    }
    public void subdmesh()
    {
        //  MeshHelper.Subdivide(MF.mesh);
        MF.mesh = MeshHelper.primplane(scale, 2);


    }
    public Vector3 flatten(Vector3 input)
    {
        return Vector3.Scale(input, new Vector3(1, 0, 1));

    }
    public Mesh generatecell(Vector2 cellpos, Vector3 LODtag)
    {


        Vector2 noiseoffset = (cellpos * scale * 2);
        float prox = math.length(flatten(gameObject.transform.position) - LODtag + new Vector3(noiseoffset.x, 0, noiseoffset.y));
        int lod = 0;


        if (prox < LODdists.x)
        {
            lod = 5;
        }
        else if (prox < LODdists.y)
        {
            lod = 4;
        }
        else if (prox < LODdists.z)
        {
            lod = 3;
        }
        else if (prox < LODdists.w)
        {
            lod = 2;
        }
        else
        {
            lod = -1;
        }


        Mesh cellmesh = new Mesh();
        cellmesh.indexFormat = IndexFormat.UInt16;
        if (lod != -1)
        {
            cellmesh = MeshHelper.primplane(scale, lod);
        }

        float detail = detailin;


        Vector3[] newverts = new Vector3[cellmesh.vertices.Length];

        for (int i = 0; i < cellmesh.vertices.Length; i++)
        {
            newverts[i].x = cellmesh.vertices[i].x + cellpos.x * scale * 2;
            newverts[i].z = cellmesh.vertices[i].z + cellpos.y * scale * 2;
            newverts[i].y = math.pow(math.clamp(math.abs(cnoiseoct(new Vector3(newverts[i].x * freq, 0, newverts[i].z * freq),Mathf.RoundToInt(detail), 0.4f) - 0f) - canyonwidth, 0, 50), 0.4f)
                * amp;
            newverts[i].y = newverts[i].y + yoffset * scale;

            // newverts[i].y = math.length(flatten(gameObject.transform.position + cellmesh.vertices[i]) - taggedobjpos + new Vector3(noiseoffset.x,0,noiseoffset.y)) + yoffset;

        }
        cellmesh.SetVertices(newverts);
        cellmesh.RecalculateNormals();
        return cellmesh;
    }
    public static float cnoiseoct(Vector3 v, int detail, float roughness)
    {
        Vector2 coord2d = new Vector2(v.x, v.y);
        float c = 0f;
        float Scale = 0f;
        for (int i = 1; i < detail; i++)
        {
            c += Mathf.PerlinNoise((coord2d * (float)math.pow(i, 2.0) * (1 / math.pow(i, 1 / roughness))).x, (coord2d * (float)math.pow(i, 2.0) * (1 / math.pow(i, 1 / roughness))).y);
            //    c += noise.cnoise(v * (float)math.pow(i, 2.0)) * (1 / math.pow(i, 1 / roughness));
            Scale += (1 / math.pow(i, 1 / roughness));

        }
        return c / Scale;
    }
    public void threadtest(object cellobj)
    {
        cell mycell = cellobj as cell;
        mycell.mesh = generatecell(mycell.cellpos, mycell.tagpos);

        cellobj = mycell;
        mycell = null;
    }
    public Vector3[] distributepoints(Mesh mesh, float density, float randomness)
    {
        if (mesh == null)
        {
            return null;
        }

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;
            List<Vector3> newverts = new List<Vector3>();
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] += ((noise.cnoise(vertices[i] * 100) * (Vector3)tangents[i]) + 
                    (noise.cnoise(vertices[i] * 1000) * (Vector3)math.cross((Vector3)tangents[i], normals[i]))) * randomness;
                if (noise.cnoise(vertices[i] * 600) < density)
                {
                    newverts.Add(vertices[i]);
                }
            }
            return newverts.ToArray();
    }
    public Mesh instanceonpoints(Mesh Instance, Vector3[] points)
    {
        CombineInstance[] combineInstances = new CombineInstance[points.Length];
        Mesh Omesh = new Mesh();
        for (int i = 0; i < points.Length; i++)
        {
            combineInstances[i].mesh = transformmesh(Instance,points[i],new Vector3(50,500,50));
        }
        Omesh.indexFormat = IndexFormat.UInt32;
        Omesh.CombineMeshes(combineInstances,true,false);
        Omesh.RecalculateNormals();
        Omesh.RecalculateBounds();
        return Omesh;
    }
    public Mesh transformmesh(Mesh mesh,Vector3 translation,Vector3 scale)
    {
        Mesh newmesh = new Mesh();
        newmesh.vertices = mesh.vertices;
        newmesh.triangles = mesh.triangles;
        Vector3[] newverts = new Vector3[mesh.vertices.Length];
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            newverts[i] = (Vector3.Scale(mesh.vertices[i],scale)) + translation;
        }
        newmesh.SetVertices(newverts);
        return newmesh;
    }
}
public class cell
{
    public Vector2 cellpos;
    public Vector3 tagpos;
    public Mesh mesh;
}