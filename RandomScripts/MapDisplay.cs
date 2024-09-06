using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{            
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public void DrawTexturesFromRenderPlanes(RenderPlane[] renderPlanes)
    {
        for (int i = 0; i < renderPlanes.Length; i++)
        {
            if (renderPlanes[i].isActive)
            {
                renderPlanes[i].renderer.sharedMaterial.mainTexture = renderPlanes[i].texture;
                renderPlanes[i].renderer.transform.localScale = new Vector3(renderPlanes[i].texture.width, 1, renderPlanes[i].texture.height);
            }
        }
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}
