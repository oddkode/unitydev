using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public static class MeshGenerator
{ 
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for(int y = 0; y < height; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < width; x += meshSimplificationIncrement) 
            {
                meshData.vertices[vertexIndex] = new float3(topLeftX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);
                meshData.uvs[vertexIndex] = new float2(x / (float)width, y / (float)height);

                if(x < width - 1 && y < height - 1)
                {
                    meshData.AddTriangle(new int3(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine));
                    meshData.AddTriangle(new int3(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1));                    
                }

                vertexIndex++;
            }
        }

        return meshData;

    }
}

public class MeshData
{
    //TODO: Cast like this to eventually flip to NativeArray<float3>, etc. as the new API will accept these and allows for true multithreading via a Job System and burst compiling
    public float3[] vertices;
    public float2[] uvs;
    public int[] triangles;    

    private int triangleIndex;

    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new float3[meshWidth * meshHeight];
        uvs = new float2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }

    public void AddTriangle(int3 triangle)
    {
        triangles[triangleIndex] = triangle.x;
        triangles[triangleIndex + 1] = triangle.y;
        triangles[triangleIndex + 2] = triangle.z;

        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        // TOOD: Obviously a cost to looping through all of the stored data and converting it to V2s and V3s but this is just temporary
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToVector3();
        mesh.triangles = triangles;
        mesh.uv = uvs.ToVector2();
        mesh.RecalculateNormals();



        return mesh;
    }
}
