using UnityEngine;

[System.Serializable]
public class PoissonSettings : MonoBehaviour
{       
    public PoissonCoreSettings coreSettings;

    [Space()]    
    public PoissonMaterialAndTextureSettings materialAndTextureSettings;

    [Space()]    
    public PoissonObjectReferenceSettings objectReferenceSettings;

    [Space()]    
    public PoissonVisualizerDisplaySettings displaySettings;

    [Space()]    
    public PoissonPostFilterSettings postFilterSettings;
}

[System.Serializable]
public class PoissonCoreSettings
{
    public int seed = 1337;
    [Range(0.0f, 50.0f)]
    public float sampleRadius = 1.0f;
    [Range(1, 100)]
    public int maxSampleRejections = 30;
    public Vector2 spawnStart;
    public bool randomizeSpawnStart;
    public SamplerRegionType regionType = SamplerRegionType.Rectangular;
    [Range(1, 500)]
    public float circularRegionRadius = 100.0f;
    public Vector2 rectangularRegionBounds = new Vector2(200.0f, 200.0f);    
}

[System.Serializable]
public class PoissonMaterialAndTextureSettings
{
    public Material densityCloudBackgroundMaterial;
    public Material solidBackgroundMaterial;
    public Material textureFilterBackgroundMaterial;
    public Material maskFilterBackgroundMaterial;
    public Material importedMaskFilterBackgroundMaterial;

    [HideInInspector]
    public Texture2D filterTexture;
    [HideInInspector]
    public Texture2D filterMaskTexture;
    [HideInInspector]
    public Texture2D importedMaskTexture;    
}

[System.Serializable]
public class PoissonObjectReferenceSettings
{
    public UniformCirclePlane circleRenderPlaneReference;
    public GameObject rectRenderPlaneReference;
}

[System.Serializable]
public class PoissonVisualizerDisplaySettings
{    
    public SamplerBackground backgroundType;
    [ColorUsage(true)]
    public Color backgroundColor = Color.black;
    [ColorUsage(true)]
    public Color forgroundColor;
    [Range(0.0f, 1.0f)]
    public float textureAlpha;
    [Range(0.1f, 5f)]
    public float pointDisplayRadius = 1;
    public bool realTime = false;
}

[System.Serializable]
public class PoissonPostFilterSettings
{
    [Space()]
    [Header("Filter and Mask Types")]
    public PostFilterType postFilterType;
    public PostFilterMaskType postFilterMaskType;

    [Space()]
    [Header("Checkerboard and Grid")]
    
    public int checkerboardBlockSize = 4;
    public int circleGridCellRadius = 4;
    public int circleGridSpacing = 1;

    [Space()]
    [Header("Circular Falloff Mask")]
    [Range(0.0f, 100.0f)]
    public float postFilterCircularMaskFalloff = 2.0f;

    [Space()]
    [Header("Rectangular Falloff Mask")]
    
    [Range(0.1f, 10.0f)]
    public float postFilterRectangularMaskFalloffStrength = 3.0f;
    [Range(0.1f, 10.0f)]
    public float postFilterRectangularMaskFalloffFade = 2.2f;
    public Vector2 postFilterRectangularMaskRegionSize = Vector2.one;

    [Space()]
    [Header("Gradient Masks")]
    
    [Range(0.1f, 2.0f)]
    public float linearGradientMaskFalloff = 1f;
    
    [Space()]
    [Header("Solid Masks")]

    [Range(1, 100)]
    public int solidCircleMaskRadius = 1;
    public Rect solidRectangleMaskRect = new Rect(0f, 0f, 10, 10);

    [Space()]
    [Header("Noise Mask")]
    public int densityCloudSeed;
    public Vector2 densityCloudOffset;
    [Range(1, 10)]
    public int densityCloudOctaves;
    [Range(1.0f, 100.0f)]
    public float densityCloudScale;
    [Range(0.1f, 9.9f)]
    public float densityCloudPersistance;
    [Range(0.1f, 9.9f)]
    public float densityCloudLacunarity;        

    [Space()]
    [Header("Filter Logic")]
    public bool inverseTextureColors;
    public bool inverseFilterLogic;
    public bool overrideMaskRotation;
    public float rotationOverrideAngle;

    [HideInInspector]
    public float? filterRotation
    {
        get
        {
            if (!overrideMaskRotation)
                return null;
            else
                return rotationOverrideAngle;
        }
    }   
}