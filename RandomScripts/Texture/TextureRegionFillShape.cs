using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TextureExtensions;

public static class TextureRegionFillShape
{    
    public static Texture2D Generate(int width, int height, List<Vector2> points, Color? backgroundColor = null, Color? foregroundColor = null, bool fillShape = false, bool closeShape = false, float? rotation = null)
    {
        Texture2D canvas = new Texture2D(width, height);
        Color[] colors = new Color[width * height];
        Color fg, bg;

        if (!backgroundColor.HasValue)
            bg = Color.black;
        else
            bg = backgroundColor.Value;

        if (!foregroundColor.HasValue)
            fg = Color.white;
        else
            fg = foregroundColor.Value;

        for(int i = 0; i < colors.Length; i++) { colors[i] = bg; }        

        if ((width <= 0 || height <= 0) || (points == null || points.Count <= 0))
        {
            canvas.Apply();
            return canvas;
        }            

        canvas.SetPixels(colors);               
        
        for(int i = 0; i < points.Count - 1; i++)
        {                        
            canvas.DrawLine(points[i], points[i + 1], fg);      
        }

        if(closeShape)
        {
            canvas.DrawLine(points[points.Count - 1], points[0], fg);
        }

        // We can't flood fill open shapes
        if(closeShape && fillShape)
        {
            int x = width / 2;
            int y = height / 2;
            
            canvas.FloodFillBorder(x, y, fg, fg);
        }

        canvas.filterMode = FilterMode.Point;        
        
        if (rotation.HasValue)
        {
            canvas.Rotate(rotation.Value);
        }

        canvas.Apply();

        return canvas;
    }
    
    public static Texture2D Triangle(int width, int height, Color? backgroundColor = null, Color? foregroundColor = null, float? rotation = null)
    {
        List<Vector2> points = new List<Vector2>();        
        int centerX = width / 2;                       
        Color fg, bg;

        if (!backgroundColor.HasValue)
            bg = Color.black;
        else
            bg = backgroundColor.Value;

        if (!foregroundColor.HasValue)
            fg = Color.white;
        else
            fg = foregroundColor.Value;

        points.Add(new Vector2(centerX, height));
        points.Add(new Vector2(0, 0));
        points.Add(new Vector2(width, 0));

        return Generate(width, height, points, bg, fg, true, true, rotation);
    }

    public static Texture2D Diamond(int width, int height, Color? backgroundColor = null, Color? foregroundColor = null, float? rotation = null)
    {
        List<Vector2> points = new List<Vector2>();
        
        int centerX = width / 2;
        int centerY = height / 2;

        Color fg, bg;

        if (!backgroundColor.HasValue)
            bg = Color.black;
        else
            bg = backgroundColor.Value;

        if (!foregroundColor.HasValue)
            fg = Color.white;
        else
            fg = foregroundColor.Value;        

        points.Add(new Vector2(centerX, height));
        points.Add(new Vector2(0, centerY));
        points.Add(new Vector2(centerX, 0));
        points.Add(new Vector2(width, centerY));

        return Generate(width, height, points, bg, fg, true, true, rotation);
    }
}
