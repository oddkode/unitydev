using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Square : MonoBehaviour
{
    private LineRenderer lineRenderer;        

    public void Draw(Rect bounds, Color? color = null, float lineThickness = 0.3f)
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;

        if (color.HasValue)
        {
            lineRenderer.sharedMaterial.SetColor("_Color", color.Value);
        }
        
        lineRenderer.startWidth = lineThickness;
        lineRenderer.endWidth = lineThickness;
        lineRenderer.positionCount = 5;
        lineRenderer.loop = false;

        Vector3[] vertices = new Vector3[5];

        vertices[0] = new Vector2(bounds.x, bounds.y);
        vertices[1] = new Vector2(bounds.width, bounds.y);
        vertices[2] = new Vector2(bounds.width, bounds.height);
        vertices[3] = new Vector2(bounds.x, bounds.height);
        vertices[4] = new Vector2(bounds.x, bounds.y);

        lineRenderer.SetPositions(vertices);                
    }
}
