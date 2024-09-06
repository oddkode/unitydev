using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NewMath = Unity.Mathematics;

public static class Extensions
{    
    public static Transform Clear(this Transform transform, bool immediate = false)
    {
        foreach (Transform child in transform)
        {
            if (!immediate)
                GameObject.Destroy(child.gameObject);
            else
                GameObject.DestroyImmediate(child.gameObject);
        }
        return transform;
    }
    
    public static Vector3[] ToVector3(this NewMath.float3[] arr)
    {
        Vector3[] v3arr = new Vector3[arr.Length];

        for(int i = 0; i < arr.Length; i++)
        {
            v3arr[i] = new Vector3(arr[i].x, arr[i].y, arr[i].z);
        }

        return v3arr;
    }

    public static Vector2[] ToVector2(this NewMath.float2[] arr)
    {
        Vector2[] v2arr = new Vector2[arr.Length];

        for (int i = 0; i < arr.Length; i++)
        {
            v2arr[i] = new Vector3(arr[i].x, arr[i].y);
        }

        return v2arr;
    }

    public static T PopRandom<T>(this List<T> list)
    {
        int randomIndex = Random.Range(0, list.Count - 1);
        T item = list[randomIndex];

        list.RemoveAt(randomIndex);

        return item;
    }

    public static float[,] To2DArray(this float[] oneDeeArray, int arrSize)
    {        
        float[,] newArr = new float[arrSize, arrSize];

        for (int i = 0; i < oneDeeArray.Length; i++)
        {
            int x = i / arrSize;
            int y = i % arrSize;

            newArr[x, y] = oneDeeArray[i];
        }

        return newArr;
    }

    public static float[] To1DArray(this float[,] twoDeeArray, int arrSize)
    {
        float[] newArr = new float[arrSize * arrSize];

        for (int y = 0; y < arrSize; y++)
        {
            for (int x = 0; x < arrSize; x++)
            {
                newArr[y * arrSize + x] = twoDeeArray[x, y];
            }
        }

        return newArr;
    }

    public static void InstantiateGridLists(this List<int>[,] grid)
    {
        if (grid != null && grid.Length > 0)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    grid[x, y] = new List<int>();
                }
            }
        }
    }

    public static Vector3? GetDimensions(this GameObject go)
    {
        Vector3? targetDimensions = null;

        if (go.GetComponent<Collider>() != null)
        {
            Collider collider = go.GetComponent<Collider>();
            targetDimensions = collider.bounds.size;
        }

        else if (go.GetComponent<Renderer>() != null)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            targetDimensions = renderer.bounds.size;
        }

        else
        {
            Collider collider = go.AddComponent<Collider>();

            targetDimensions = collider.bounds.size;

            GameObject.DestroyImmediate(collider);
        }

        return targetDimensions;
    }   
}
