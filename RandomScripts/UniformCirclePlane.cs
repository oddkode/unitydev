using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class UniformCirclePlane : MonoBehaviour
{
    public float radius = 10f;
    public int segments = 16;
    public bool realTime = false;   

    public void Generate()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Renderer renderer = GetComponent<Renderer>();

        Mesh mesh = new Mesh();
        
        meshFilter.mesh = mesh;
        meshFilter.sharedMesh = mesh;
        
        List<Vector3> verticesList = new List<Vector3> { };
        
        float x;
        float y;
        
        for (int i = 0; i < segments; i++)
        {
            x = radius * Mathf.Sin((2 * Mathf.PI * i) / segments);
            y = radius * Mathf.Cos((2 * Mathf.PI * i) / segments);
            
            verticesList.Add(new Vector3(x, y, 0f));
        }
        
        Vector3[] vertices = verticesList.ToArray();

        List<int> trianglesList = new List<int> { };
        
        for (int i = 0; i < (segments - 2); i++)
        {
            trianglesList.Add(0);
            trianglesList.Add(i + 1);
            trianglesList.Add(i + 2);
        }
        
        int[] triangles = trianglesList.ToArray();


        List<Vector3> normalsList = new List<Vector3> { };
        
        for (int i = 0; i < vertices.Length; i++)
        {
            normalsList.Add(-Vector3.forward);
        }
        
        Vector3[] normals = normalsList.ToArray();                                               
        
        mesh.vertices = vertices;        
        mesh.triangles = triangles;
        mesh.normals = normals;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();                
    }
}