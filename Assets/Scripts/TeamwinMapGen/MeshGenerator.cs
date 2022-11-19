using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(Vector3[,] heightmap,int levelOfDetail)
    {
        Profiler.BeginSample("GenerateMesh");
        int width = heightmap.GetLength(0);
        int height = heightmap.GetLength(1);



        float topleftX = (width - 1) / -2f;
        float topleftz = (height - 1) / 2f;

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int verticiesperline = ((width-1)/meshSimplificationIncrement) + 1;

        MeshData meshdata = new MeshData(verticiesperline, verticiesperline);
        int vertexindex = 0;

        for(int y=0;y<height;y+= meshSimplificationIncrement)
        {
            for (int x = 0; x < width; x+= meshSimplificationIncrement)
            {
                float vertexheight = heightmap[x, y].y;
                float xOffset = topleftX + x + (heightmap[x, y].x);
                float zOffset = topleftz - y + (heightmap[x, y].z);

                meshdata.vertices[vertexindex] = new Vector3(xOffset, vertexheight , zOffset);

              //  meshdata.vertices[vertexindex] = new Vector3(topleftX + x, heightmap[x, y] * heightmult, topleftz - y);

                meshdata.uvs[vertexindex] = new Vector2(x / (float)width, y / (float)height);


                if(x < width-1 && y < height - 1)
                {
                    meshdata.AddTriangle(vertexindex, vertexindex + verticiesperline + 1, vertexindex + verticiesperline);
                    meshdata.AddTriangle(vertexindex + verticiesperline + 1, vertexindex, vertexindex + 1);
                }
                vertexindex++;
            }
        }
        Profiler.EndSample();
        return meshdata;

    }
}
public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;


    int triangleindex;
    public MeshData(int meshwidth, int meshheight)
    {
        uvs = new Vector2[meshwidth * meshheight];
        vertices = new Vector3[meshwidth * meshheight];
        triangles = new int[(meshwidth - 1) * (meshheight - 1) * 6];
    }

    public void AddTriangle(int a,int b,int c)
    {
        triangles[triangleindex] = a;
        triangles[triangleindex+1] = b;
        triangles[triangleindex+2] = c;
        triangleindex += 3;
    }

    public Mesh createmesh()
    {
        Mesh mesh = new Mesh();

        if (vertices.Length >= 65535) mesh.indexFormat = IndexFormat.UInt32;

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        return mesh;
    }

}
