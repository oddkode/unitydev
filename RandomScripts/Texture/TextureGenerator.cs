using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        
        return texture;
    }

    public static Texture2D[] TexturesFromColorMaps(int width, int height, params Color[][] colors)
    {
        Texture2D[] textures = new Texture2D[colors.Length];

        for(int i = 0; i < colors.Length; i++)
        {
            Texture2D texture = new Texture2D(width, height);

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(colors[i]);
            texture.Apply();
            
            textures[i] = texture;            
        }        

        return textures;
    }

    public static Texture2D TextureFromHeightMap(float[,] heightMap, float alpha = 1.0f)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        Color[] colorMap = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colorMap[y * width + x] = Color.Lerp(new Color(0f, 0f, 0f, alpha), new Color(1f, 1f, 1f, alpha), heightMap[x, y]);
            }
        }

        return TextureFromColorMap(colorMap, width, height);
    }

    public static Color[] ColorFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colorMap = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return colorMap;
    }

}
