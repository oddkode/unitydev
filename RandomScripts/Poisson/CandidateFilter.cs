using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TextureExtensions;

public interface ICandidateFilter
{
    FilterType type { get; }
    bool Filter(Vector2 candidate);    
}

public interface IPostFilterMask
{
    PostFilterMaskType type { get; }    
    Texture2D Generate(int width, int height, bool inverseColors = false, float? rotation = null);
}

public struct CircleFilter : ICandidateFilter
{
    public readonly FilterType type => FilterType.Area;
    public readonly Vector2 center;
    public readonly float radius;

    public CircleFilter(Vector2 center, float radius)
    {
        this.center = center;
        this.radius = radius;
    }

    public bool Filter(Vector2 candidate)
    {
        float cx = candidate.x - center.x;
        float cy = candidate.y - center.y;

        return (cx * cx + cy * cy) <= radius;
    }
}

public struct RectangleFilter : ICandidateFilter
{
    public readonly FilterType type => FilterType.Area;
    public readonly Rect bounds;
    
    public RectangleFilter(Rect bounds)
    {
        this.bounds = bounds;
    }

    public bool Filter(Vector2 candidate)
    {
        return (candidate.x >= bounds.x && candidate.x < bounds.width && candidate.y >= bounds.y && candidate.y < bounds.height);
    }
}

public struct MaskFilter : ICandidateFilter
{
    public readonly FilterType type => FilterType.Post;
    public readonly IPostFilterMask filter;    
    public readonly Texture2D texture;
    public readonly Texture altTexture;
    public readonly bool inverseColors;
    public readonly bool inverseLogic;
    public readonly float? rotation;
    public readonly int width;
    public readonly int height;    

    public MaskFilter(IPostFilterMask filter, int width, int height, bool inverseColors = false, bool inverseLogic = false, float? rotation = null)
    {
        this.filter = filter;
        this.inverseColors = inverseColors;
        this.inverseLogic = inverseLogic;
        this.rotation = rotation;
        this.width = width;
        this.height = height;
        this.texture = this.filter.Generate(width, height, inverseColors, rotation);
        this.altTexture = null;        
    }

    public MaskFilter(int width, int height, Texture altTexture, bool inverseLogic = false)
    {        
        this.altTexture = altTexture;
        this.width = width;
        this.height = height;
        this.inverseColors = false;
        this.inverseLogic = inverseLogic;
        this.rotation = null;
        this.filter = null;        

        RenderTexture currentRT = RenderTexture.active;

        RenderTexture renderTexture = new RenderTexture(altTexture.width, altTexture.height, 32);
        Graphics.Blit(altTexture, renderTexture);

        RenderTexture.active = renderTexture;

        Texture2D mask = new Texture2D(renderTexture.width, renderTexture.height);

        mask.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        mask.Apply();        

        renderTexture.Release();

        RenderTexture.active = currentRT;

        TextureScale.Bilinear(mask, width, height);

        this.texture = mask;
    }

    public bool Filter(Vector2 candidate)
    {
        if (texture != null)
        {
            int x = (int)candidate.x * (width / texture.width);
            int y = (int)candidate.y * (height / texture.height);
            
            if (x > width)
                x = width;

            if (x < 0)
                x = 0;

            if (y > height)
                y = height;

            if (y < 0)
                y = 0;

            Color pixel;

            if (filter != null && filter.type == PostFilterMaskType.Noise)
            {
                pixel = texture.GetPixel(texture.width - x, texture.height - y);
            }

            else if (filter != null && filter.type == PostFilterMaskType.Checkerboard)
            {
                pixel = texture.GetPixel(x, y);
            }

            else
            {
                pixel = texture.GetPixel(x, texture.height - y);
            }
            
            if (inverseLogic)
            {
                return Random.value > pixel.grayscale;
            }

            else
            {
                return Random.value < pixel.grayscale;
            }            
        }        

        return false;
    }
}

[System.Serializable]
public struct PostFilterCircleMask : IPostFilterMask
{
    public PostFilterMaskType type => PostFilterMaskType.LinearCircle;
    public float falloff;

    public Texture2D Generate(int width, int height, bool inverseColors = false, float? rotation = null)
    {
        Texture2D mask = new Texture2D(width, height);
        Vector2 maskCenter = new Vector2(width * 0.5f, height * 0.5f);

        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                float distanceFromCenter = Vector2.Distance(maskCenter, new Vector2(x, y));
                float maskPixel = (0.5f - (distanceFromCenter / width)) * falloff;
                Color color = new Color(maskPixel, maskPixel, maskPixel, 1.0f);

                if (inverseColors)
                {
                    color = InvertColor(color);
                }                

                mask.SetPixel(x, y, color);
            }
        }

        mask.Apply();

        return mask;
    }

    private Color InvertColor(Color color)
    {
        return new Color(1.0f - color.r, 1.0f - color.g, 1.0f - color.b);
    }
}

[System.Serializable]
public struct PostFilterSolidCircleMask : IPostFilterMask
{
    public PostFilterMaskType type => PostFilterMaskType.SolidCircle;
    public int radius;    

    public Texture2D Generate(int width, int height, bool inverseColors = false, float? rotation = null)
    {
        Texture2D mask = new Texture2D(width, height);
        Color circleColor;
        Color backgroundColor;
        Color[] background = new Color[width * height];

        int mX = (int)(width * 0.5f);
        int mY = (int)(height * 0.5f);

        if(inverseColors)
        {
            circleColor = Color.black;
            backgroundColor = Color.white;
        }

        else
        {
            circleColor = Color.white;
            backgroundColor = Color.black;
        }

        for(int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                background[y + x * width] = backgroundColor;
            }
        }

        mask.SetPixels(background);
        mask.DrawFilledCircle(mX, mY, radius, circleColor);
        mask.filterMode = FilterMode.Point;

        mask.Apply();

        return mask;
    }
}

[System.Serializable]
public struct PostFilterSolidRectangleMask : IPostFilterMask
{
    public PostFilterMaskType type => PostFilterMaskType.SolidRectangle;
    public Rect rect;

    public Texture2D Generate(int width, int height, bool inverseColors = false, float? rotation = null)
    {
        Texture2D mask = new Texture2D(width, height);
        Color rectColor;
        Color backgroundColor;
        Color[] background = new Color[width * height];        

        if (inverseColors)
        {
            rectColor = Color.black;
            backgroundColor = Color.white;
        }

        else
        {
            rectColor = Color.white;
            backgroundColor = Color.black;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                background[y + x * width] = backgroundColor;
            }
        }

        mask.SetPixels(background);
        mask.DrawFilledRectangle(rect, rectColor);
        mask.filterMode = FilterMode.Point;
        mask.wrapMode = TextureWrapMode.Clamp;
        
        if(rotation.HasValue)
        {
            mask.Rotate(rotation.Value);
        }

        mask.Apply();

        return mask;
    }
}

[System.Serializable]
public struct PostFilterNoiseMask : IPostFilterMask
{
    public PostFilterMaskType type => PostFilterMaskType.Noise;    
    public PoissonPostFilterSettings settings;

    public Texture2D Generate(int width, int height, bool inverseColors = false, float? rotation = null)
    {
        Texture2D mask = GenerateNoiseMap(width, height);
        
        mask.filterMode = FilterMode.Point;
        mask.wrapMode = TextureWrapMode.Clamp;

        if (rotation.HasValue)
        {
            mask.Rotate(rotation.Value);
        }

        mask.Apply();

        return mask;
    }

    private Texture2D GenerateNoiseMap(int width, int height)
    {
        System.Random prng = new System.Random(settings.densityCloudSeed);

        Vector2[] octaveOffsets = new Vector2[settings.densityCloudOctaves];       

        float[,] noiseMap = new float[width, height];

        float amplitude = 1f;
        float frequency = 1f;

        for (int i = 0; i < settings.densityCloudOctaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + settings.densityCloudOffset.x;
            float offsetY = prng.Next(-100000, 100000) - settings.densityCloudOffset.y;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            amplitude *= settings.densityCloudPersistance;
        }

        if (settings.densityCloudScale <= 0)
        {
            settings.densityCloudScale = 0.0001f;
        }

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfHeight = height / 2f;
        float halfWidth = width / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float noiseHeight = 0f;
                amplitude = 1f;
                frequency = 1f;

                for (int i = 0; i < settings.densityCloudOctaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.densityCloudScale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.densityCloudScale * frequency;
                    float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);

                    noiseValue = noiseValue * 2 - 1;
                    noiseHeight += noiseValue * amplitude;

                    amplitude *= settings.densityCloudPersistance;
                    frequency *= settings.densityCloudLacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }

                else if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }

        return TextureGenerator.TextureFromHeightMap(noiseMap);
    }
}

[System.Serializable]
public struct PostFilterSolidTriangleMask : IPostFilterMask
{
    public PostFilterMaskType type => PostFilterMaskType.SolidTriangle;    

    public Texture2D Generate(int width, int height, bool inverseColors = false, float? rotation = null)
    {        
        Color fg;
        Color bg;
        
        if (inverseColors)
        {
            fg = Color.black;
            bg = Color.white;
        }

        else
        {
            fg = Color.white;
            bg = Color.black;
        }

        return TextureRegionFillShape.Triangle(width, height, bg, fg, rotation);
    }
}

[System.Serializable]
public struct PostFilterSolidDiamondMask : IPostFilterMask
{
    public PostFilterMaskType type => PostFilterMaskType.SolidDiamond;

    public Texture2D Generate(int width, int height, bool inverseColors = false, float? rotation = null)
    {
        Color fg;
        Color bg;

        if (inverseColors)
        {
            fg = Color.black;
            bg = Color.white;
        }

        else
        {
            fg = Color.white;
            bg = Color.black;
        }

        return TextureRegionFillShape.Diamond(width, height, bg, fg, rotation);
    }
}

[System.Serializable]
public struct PostFilterCheckerboardMask : IPostFilterMask
{
    public PostFilterMaskType type => PostFilterMaskType.Checkerboard;
    public int blockSize;    

    public Texture2D Generate(int width, int height, bool inverseColors = false, float? rotation = null)
    {     
        if (blockSize <= 2) 
        {
            blockSize = 2;
        }

        Texture2D mask = new Texture2D(width, height);
        Color[] colors = new Color[width * height];              

        for (int x = 0; x < width; x++)
        {            
            for (int y = 0; y < height; y++)
            {
                if(((x / blockSize) + (y / blockSize)) % 2 == 0)
                {
                    if(inverseColors)
                    {
                        colors[x + y * width] = Color.white;
                    }

                    else
                    {
                        colors[x + y * width] = Color.black;
                    }                    
                }

                else
                {
                    if (inverseColors)
                    {
                        colors[x + y * width] = Color.black;
                    }

                    else
                    {
                        colors[x + y * width] = Color.white;
                    }
                }                                    
            }            
        }

        mask.filterMode = FilterMode.Point;
        mask.wrapMode = TextureWrapMode.Repeat;
        mask.SetPixels(colors);                

        if (rotation.HasValue)
        {
            mask.Rotate(rotation.Value);
        }

        mask.Apply();

        return mask;
    }
}

[System.Serializable]
public struct PostFilterCircleGridMask : IPostFilterMask
{
    public PostFilterMaskType type => PostFilterMaskType.CircleGrid;
    public int circleDiameter;
    public int spacing;

    public Texture2D Generate(int width, int height, bool inverseColors = false, float? rotation = null)
    {        
        if (circleDiameter <= 2)
        {
            circleDiameter = 2;
        }
        
        int xCell = Mathf.FloorToInt((circleDiameter / 2) + spacing);
        int yCell = Mathf.FloorToInt((circleDiameter / 2) + spacing);

        Texture2D mask = new Texture2D(width, height);
        Color[] colors = new Color[width * height];
        
        Color cellColor = Color.white;
        Color backgroundColor = Color.black;

        if(inverseColors)
        {
            cellColor = Color.black;
            backgroundColor = Color.white;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colors[x + y * width] = backgroundColor;
            }
        }

        mask.SetPixels(colors);

        do
        {
            do
            {
                mask.DrawFilledCircle(xCell, yCell, circleDiameter / 2, cellColor);
                xCell = xCell + (circleDiameter + spacing);

            } while (xCell + (circleDiameter / 2) + spacing <= width);

            xCell = (circleDiameter / 2) + spacing;
            yCell = yCell + circleDiameter + spacing;

        } while (yCell + (circleDiameter / 2) + spacing <= height);

        mask.filterMode = FilterMode.Point;           
        
        if (rotation.HasValue)
        {
            mask.Rotate(rotation.Value);
        }

        mask.Apply();

        return mask;
    }   
}

[System.Serializable]
public struct PostFilterCircleTriangleGridMask : IPostFilterMask
{
    public PostFilterMaskType type => PostFilterMaskType.CircleTriangleGrid;
    public int circleDiameter;
    public int spacing;

    public Texture2D Generate(int width, int height, bool inverseColors = false, float? rotation = null)
    {
        if (circleDiameter <= 2)
        {
            circleDiameter = 2;
        }

        int xCell = Mathf.FloorToInt((circleDiameter / 2) + spacing);
        int yCell = Mathf.FloorToInt((circleDiameter / 2) + spacing);
        int triangle = 0;

        Texture2D mask = new Texture2D(width, height);
        Color[] colors = new Color[width * height];

        Color cellColor = Color.white;
        Color backgroundColor = Color.black;

        if (inverseColors)
        {
            cellColor = Color.black;
            backgroundColor = Color.white;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colors[x + y * width] = backgroundColor;
            }
        }

        mask.SetPixels(colors);

        do
        {
            do
            {
                mask.DrawFilledCircle(xCell, yCell, circleDiameter / 2, cellColor);
                xCell = xCell + (circleDiameter + spacing);

            } while (xCell + (circleDiameter / 2) + spacing <= width);

            if (triangle == 0)
            {
                xCell = Mathf.FloorToInt((circleDiameter + 1.5f * spacing));
                triangle = 1;
            }

            else
            {
                xCell = (circleDiameter / 2) + spacing;
                triangle = 0;
            }
            
            yCell = yCell + Mathf.FloorToInt(Mathf.Pow(Mathf.Pow((circleDiameter + spacing), 2) * 0.75f, 0.5f));

        } while (yCell + (circleDiameter / 2) <= (height - spacing));

        mask.filterMode = FilterMode.Point;

        if (rotation.HasValue)
        {
            mask.Rotate(rotation.Value);
        }

        mask.Apply();

        return mask;
    }
}

[System.Serializable]
public struct PostFilterHorizontalLinearMask : IPostFilterMask
{
    public PostFilterMaskType type => PostFilterMaskType.HorizontalLinear;
    public float falloff;

    public Texture2D Generate(int width, int height, bool inverseColors = false, float? rotation = null)
    {
        Texture2D mask = new Texture2D(width, height);
        Color[] gradientColors = new Color[2];
        Color[] colors = new Color[width * height];

        if(inverseColors)
        {
            gradientColors[0] = Color.white;            
            gradientColors[1] = Color.black;
        }

        else
        {
            gradientColors[0] = Color.black;            
            gradientColors[1] = Color.white;
        }
                
        GradientColorKey[] colorKeys = new GradientColorKey[gradientColors.Length];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[gradientColors.Length];

        float steps = gradientColors.Length - 1f;

        for (int i = 0; i < gradientColors.Length; i++)
        {
            float step = i / steps;

            colorKeys[i].color = gradientColors[i];
            colorKeys[i].time = step;
            
            alphaKeys[i].alpha = gradientColors[i].a;
            alphaKeys[i].time = step;
        }

        
        Gradient gradient = new Gradient();
        gradient.SetKeys(colorKeys, alphaKeys);        
        
        for(int y = 0; y < height; y++)
        {            
            for (int x = 0; x < width; x++)
            {
                float time = Mathf.InverseLerp(0, height, y) * falloff;
                colors[y + x * width] = gradient.Evaluate(time);
            }
        }

        mask.filterMode = FilterMode.Bilinear;
        mask.SetPixels(colors);
        
        if (rotation.HasValue)
        {
            mask.Rotate(rotation.Value);
        }

        mask.Apply();

        return mask;
    }
}

[System.Serializable]
public struct PostFilterVerticalLinearMask : IPostFilterMask
{
    public PostFilterMaskType type => PostFilterMaskType.HorizontalLinear;
    public float falloff;

    public Texture2D Generate(int width, int height, bool inverseColors = false, float? rotation = null)
    {
        Texture2D mask = new Texture2D(width, height);
        Color[] gradientColors = new Color[2];
        Color[] colors = new Color[width * height];

        if (inverseColors)
        {
            gradientColors[0] = Color.white;            
            gradientColors[1] = Color.black;
        }

        else
        {
            gradientColors[0] = Color.black;            
            gradientColors[1] = Color.white;
        }

        GradientColorKey[] colorKeys = new GradientColorKey[gradientColors.Length];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[gradientColors.Length];

        float steps = gradientColors.Length - 1f;

        for (int i = 0; i < gradientColors.Length; i++)
        {
            float step = i / steps;

            colorKeys[i].color = gradientColors[i];
            colorKeys[i].time = step;

            alphaKeys[i].alpha = gradientColors[i].a;
            alphaKeys[i].time = step;
        }


        Gradient gradient = new Gradient();
        gradient.SetKeys(colorKeys, alphaKeys);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float time = Mathf.InverseLerp(0, width, x) * falloff;
                colors[y + x * width] = gradient.Evaluate(time);
            }
        }

        mask.filterMode = FilterMode.Bilinear;
        mask.SetPixels(colors);
        
        if (rotation.HasValue)
        {
            mask.Rotate(rotation.Value);
        }

        mask.Apply();

        return mask;
    }
}

[System.Serializable]
public struct PostFilterSquareMask : IPostFilterMask
{
    public PostFilterMaskType type => PostFilterMaskType.LinearRectangle;
    public float falloffStrength;
    public float falloffFade;    

    public Texture2D Generate(int width, int height, bool inverseColors = false, float? rotation = null)
    {
        Texture2D mask = new Texture2D(width, height);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float x = i / (float)width * 2 - 1;
                float y = j / (float)height * 2 - 1;

                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                float maskPixel = Evaluate(value, falloffStrength, falloffFade);

                Color pixelColor = new Color(maskPixel, maskPixel, maskPixel, 1.0f);

                if (inverseColors)
                {
                    pixelColor = InvertColor(pixelColor);
                }                
                
                mask.SetPixel(i, j, pixelColor);
            }
        }
       
        if (rotation.HasValue)
        {
            mask.Rotate(rotation.Value);
        }

        mask.Apply();

        return mask;
    }

    private Color InvertColor(Color color)
    {
        return new Color(1.0f - color.r, 1.0f - color.g, 1.0f - color.b);
    }

    private float Evaluate(float value, float a, float b)
    {
        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}