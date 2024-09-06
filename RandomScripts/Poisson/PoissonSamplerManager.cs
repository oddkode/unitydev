using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PoissonSamplerManager : MonoBehaviour
{
    public List<Vector2> points => samplePoints;    
    
    [HideInInspector]
    public PoissonSettings settings;
    
    [HideInInspector]
    private List<Vector2> samplePoints;

    [HideInInspector]
    private UniformCirclePlane circlePlane;
    
    [HideInInspector]
    private GameObject renderPlane;

    [HideInInspector]
    private Renderer rectRenderer;
    
    [HideInInspector]
    private Renderer circleRenderer;    
    
    [ExecuteAlways]
    private void Awake()
    {
        Initalize();
    }

    [ExecuteAlways]
    private void OnDrawGizmos()
    {
        RenderPoints(settings.coreSettings.regionType, settings.coreSettings.rectangularRegionBounds, settings.displaySettings.forgroundColor, settings.displaySettings.pointDisplayRadius);
    }

    [ExecuteAlways]
    private void OnValidate()
    {
        UpdateSettings();
    }

    [ExecuteAlways]
    public void UpdateSettings()
    {
        Initalize();               

        Generate(settings.coreSettings.regionType, settings.postFilterSettings.postFilterType);

        UpdateBackgroundMaterial(
            settings.displaySettings.backgroundType, 
            settings.postFilterSettings.postFilterType, 
            settings.displaySettings.textureAlpha
        );
        
        UpdateStatus(
            settings.coreSettings.regionType, 
            settings.displaySettings.backgroundType
        );        
    }

    private void RenderPoints(SamplerRegionType regionType, Vector2 rectangularRegionSize, Color foregroundColor, float pointRadius)
    {
        if (regionType == SamplerRegionType.Rectangular)
        {
            Gizmos.DrawWireCube(rectangularRegionSize / 2, rectangularRegionSize);
        }

        if (samplePoints != null)
        {
            foreach (Vector2 point in samplePoints)
            {
                Gizmos.color = foregroundColor;
                Gizmos.DrawSphere(point, pointRadius);
            }
        }
    }

    private void Initalize()
    {        
        settings = gameObject.GetComponent<PoissonSettings>();                   

        circlePlane = settings.objectReferenceSettings.circleRenderPlaneReference;        
        renderPlane = settings.objectReferenceSettings.rectRenderPlaneReference;
        
        rectRenderer = renderPlane.GetComponent<Renderer>();                
        circleRenderer = circlePlane.gameObject.GetComponent<Renderer>();
        
    }

    [ExecuteAlways]
    private void Generate(SamplerRegionType regionType, PostFilterType postFilterType)
    {
        Vector2? spawnStart;

        if (settings.coreSettings.randomizeSpawnStart)
        {
            spawnStart = null;
        }

        else
        {
            spawnStart = settings.coreSettings.spawnStart;
        }

        if (regionType == SamplerRegionType.Rectangular)
        {
            renderPlane.transform.position = settings.coreSettings.rectangularRegionBounds / 2;
            MaskFilter filter;

            switch (postFilterType)
            {                               
                case PostFilterType.ImportedMask:                

                    filter = new MaskFilter(
                        (int)settings.coreSettings.rectangularRegionBounds.x,
                        (int)settings.coreSettings.rectangularRegionBounds.y,
                        settings.materialAndTextureSettings.importedMaskFilterBackgroundMaterial.GetTexture("_MainTex"),
                        settings.postFilterSettings.inverseFilterLogic
                    );                    

                    samplePoints = PoissonSampler.GeneratePoints(
                        settings.coreSettings.sampleRadius,
                        settings.coreSettings.rectangularRegionBounds,
                        settings.coreSettings.seed,
                        settings.coreSettings.maxSampleRejections,
                        spawnStart,
                        filter
                    );
                    break;

                case PostFilterType.GeneratedMask:                                       

                    switch(settings.postFilterSettings.postFilterMaskType)
                    {
                        case PostFilterMaskType.LinearCircle:  
                            
                            filter = new MaskFilter(
                                new PostFilterCircleMask() { falloff = settings.postFilterSettings.postFilterCircularMaskFalloff },
                                (int)settings.coreSettings.rectangularRegionBounds.x,
                                (int)settings.coreSettings.rectangularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.SolidCircle:
                    
                            filter = new MaskFilter(
                                new PostFilterSolidCircleMask() { radius = settings.postFilterSettings.solidCircleMaskRadius },
                                (int)settings.coreSettings.rectangularRegionBounds.x,
                                (int)settings.coreSettings.rectangularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.LinearRectangle:      
                            
                            filter = new MaskFilter(
                                new PostFilterSquareMask()
                                {
                                    falloffStrength = settings.postFilterSettings.postFilterRectangularMaskFalloffStrength,
                                    falloffFade = settings.postFilterSettings.postFilterRectangularMaskFalloffFade
                                },
                                (int)settings.coreSettings.rectangularRegionBounds.x,
                                (int)settings.coreSettings.rectangularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.SolidRectangle:                    

                            filter = new MaskFilter(
                                new PostFilterSolidRectangleMask() { rect = settings.postFilterSettings.solidRectangleMaskRect },
                                (int)settings.coreSettings.rectangularRegionBounds.x,
                                (int)settings.coreSettings.rectangularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.SolidTriangle:

                            filter = new MaskFilter(
                                new PostFilterSolidTriangleMask(),
                                (int)settings.coreSettings.rectangularRegionBounds.x,
                                (int)settings.coreSettings.rectangularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;                        

                        case PostFilterMaskType.SolidDiamond:

                            filter = new MaskFilter(
                                new PostFilterSolidDiamondMask(),
                                (int)settings.coreSettings.rectangularRegionBounds.x,
                                (int)settings.coreSettings.rectangularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;                        

                        case PostFilterMaskType.HorizontalLinear:
                    
                            filter = new MaskFilter(
                                new PostFilterHorizontalLinearMask() { falloff = settings.postFilterSettings.linearGradientMaskFalloff },
                                (int)settings.coreSettings.rectangularRegionBounds.x,
                                (int)settings.coreSettings.rectangularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.VerticalLinear:
                            
                            filter = new MaskFilter(
                                new PostFilterVerticalLinearMask() { falloff = settings.postFilterSettings.linearGradientMaskFalloff },
                                (int)settings.coreSettings.rectangularRegionBounds.x,
                                (int)settings.coreSettings.rectangularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;


                        case PostFilterMaskType.Checkerboard:
                            
                            filter = new MaskFilter(
                                new PostFilterCheckerboardMask() { blockSize = settings.postFilterSettings.checkerboardBlockSize },
                                (int)settings.coreSettings.rectangularRegionBounds.x,
                                (int)settings.coreSettings.rectangularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;


                        case PostFilterMaskType.CircleGrid:
                            
                            filter = new MaskFilter(
                                new PostFilterCircleGridMask() { circleDiameter = settings.postFilterSettings.circleGridCellRadius, spacing = settings.postFilterSettings.circleGridSpacing },
                                (int)settings.coreSettings.rectangularRegionBounds.x,
                                (int)settings.coreSettings.rectangularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.CircleTriangleGrid:

                            filter = new MaskFilter(
                                new PostFilterCircleTriangleGridMask() { circleDiameter = settings.postFilterSettings.circleGridCellRadius, spacing = settings.postFilterSettings.circleGridSpacing },
                                (int)settings.coreSettings.rectangularRegionBounds.x,
                                (int)settings.coreSettings.rectangularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.Noise:

                            filter = new MaskFilter(
                                new PostFilterNoiseMask() { settings = settings.postFilterSettings },
                                (int)settings.coreSettings.rectangularRegionBounds.x,
                                (int)settings.coreSettings.rectangularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        default:
                            filter = default;
                            break;
                    }                    

                    samplePoints = PoissonSampler.GeneratePoints(
                        settings.coreSettings.sampleRadius,
                        settings.coreSettings.rectangularRegionBounds,
                        settings.coreSettings.seed,
                        settings.coreSettings.maxSampleRejections,
                        spawnStart,
                        filter

                    );
                    break;

                default:                                    
                    samplePoints = PoissonSampler.GeneratePoints(
                        settings.coreSettings.sampleRadius,
                        settings.coreSettings.rectangularRegionBounds,
                        settings.coreSettings.seed,
                        settings.coreSettings.maxSampleRejections,
                        spawnStart
                    );
                    break;                
            }           
        }

        else
        {
            float diameter = Mathf.CeilToInt(settings.coreSettings.circularRegionRadius * 2);
            Vector2 circularRegionBounds = new Vector2(diameter, diameter);

            circlePlane.transform.position = circularRegionBounds / 2;
            circlePlane.radius = settings.coreSettings.circularRegionRadius;

            MaskFilter filter;

            switch(postFilterType)
            {                               
                case PostFilterType.ImportedMask:
                    
                    filter = new MaskFilter(
                        (int)circularRegionBounds.x,
                        (int)circularRegionBounds.y,
                        settings.materialAndTextureSettings.importedMaskFilterBackgroundMaterial.GetTexture("_MainTex"),
                        settings.postFilterSettings.inverseFilterLogic
                    );                    

                    samplePoints = PoissonSampler.GeneratePoints(
                        settings.coreSettings.sampleRadius, 
                        circularRegionBounds, 
                        settings.coreSettings.seed, 
                        settings.coreSettings.maxSampleRejections,
                        spawnStart,
                        new CircleFilter(
                            circularRegionBounds / 2, 
                            settings.coreSettings.circularRegionRadius * settings.coreSettings.circularRegionRadius
                        ), 
                        filter
                    );
                    break;

                case PostFilterType.GeneratedMask:

                    switch(settings.postFilterSettings.postFilterMaskType)
                    {
                        case PostFilterMaskType.LinearCircle:
                
                            filter = new MaskFilter(
                                new PostFilterCircleMask() { falloff = settings.postFilterSettings.postFilterCircularMaskFalloff }, 
                                (int)circularRegionBounds.x, 
                                (int)circularRegionBounds.y, 
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic
                            );
                    
                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.SolidCircle:
                    
                            filter = new MaskFilter(
                                new PostFilterSolidCircleMask() { radius = settings.postFilterSettings.solidCircleMaskRadius }, 
                                (int)circularRegionBounds.x, 
                                (int)circularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.LinearRectangle:
                    
                            filter = new MaskFilter(
                                new PostFilterSquareMask() { 
                                    falloffStrength = settings.postFilterSettings.postFilterRectangularMaskFalloffStrength, 
                                    falloffFade = settings.postFilterSettings.postFilterRectangularMaskFalloffFade 
                                },
                                (int)settings.postFilterSettings.postFilterRectangularMaskRegionSize.x,
                                (int)settings.postFilterSettings.postFilterRectangularMaskRegionSize.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.SolidRectangle:
                    
                            filter = new MaskFilter(
                                new PostFilterSolidRectangleMask() { rect = settings.postFilterSettings.solidRectangleMaskRect }, 
                                (int)circularRegionBounds.x, 
                                (int)circularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.SolidTriangle:

                            filter = new MaskFilter(
                                new PostFilterSolidTriangleMask(),
                                (int)circularRegionBounds.x,
                                (int)circularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.SolidDiamond:

                            filter = new MaskFilter(
                                new PostFilterSolidDiamondMask(),
                                (int)circularRegionBounds.x,
                                (int)circularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.HorizontalLinear:
                    
                            filter = new MaskFilter(
                                new PostFilterHorizontalLinearMask() { falloff = settings.postFilterSettings.linearGradientMaskFalloff }, 
                                (int)circularRegionBounds.x, 
                                (int)circularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.VerticalLinear:
                    
                            filter = new MaskFilter(
                                new PostFilterVerticalLinearMask() { falloff = settings.postFilterSettings.linearGradientMaskFalloff }, 
                                (int)circularRegionBounds.x, 
                                (int)circularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.Checkerboard:
                    
                            filter = new MaskFilter(
                                new PostFilterCheckerboardMask() { blockSize = settings.postFilterSettings.checkerboardBlockSize }, 
                                (int)circularRegionBounds.x, 
                                (int)circularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.CircleGrid:                

                            filter = new MaskFilter(
                                new PostFilterCircleGridMask() { circleDiameter = settings.postFilterSettings.circleGridCellRadius, spacing = settings.postFilterSettings.circleGridSpacing },
                                (int)circularRegionBounds.x, 
                                (int)circularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.CircleTriangleGrid:

                            filter = new MaskFilter(
                                new PostFilterCircleTriangleGridMask() { circleDiameter = settings.postFilterSettings.circleGridCellRadius, spacing = settings.postFilterSettings.circleGridSpacing },
                                (int)circularRegionBounds.x,
                                (int)circularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        case PostFilterMaskType.Noise:

                            filter = new MaskFilter(
                                new PostFilterNoiseMask() { settings = settings.postFilterSettings },
                                (int)circularRegionBounds.x,
                                (int)circularRegionBounds.y,
                                settings.postFilterSettings.inverseTextureColors,
                                settings.postFilterSettings.inverseFilterLogic,
                                settings.postFilterSettings.filterRotation
                            );

                            settings.materialAndTextureSettings.filterMaskTexture = filter.texture;
                            break;

                        default:                
                            filter = default;
                            break;
                    }

                    samplePoints = PoissonSampler.GeneratePoints(
                        settings.coreSettings.sampleRadius, 
                        circularRegionBounds, 
                        settings.coreSettings.seed, 
                        settings.coreSettings.maxSampleRejections,
                        spawnStart,
                        new CircleFilter(
                            circularRegionBounds / 2, 
                            settings.coreSettings.circularRegionRadius * settings.coreSettings.circularRegionRadius
                        ), 
                        filter
                    );
                    break;

                default:
                    
                    samplePoints = PoissonSampler.GeneratePoints(
                        settings.coreSettings.sampleRadius, 
                        circularRegionBounds, 
                        settings.coreSettings.seed, 
                        settings.coreSettings.maxSampleRejections,
                        spawnStart,
                        new CircleFilter(
                            circularRegionBounds / 2, 
                            settings.coreSettings.circularRegionRadius * settings.coreSettings.circularRegionRadius
                        )
                    );
                    break;
            }
        }
    }

    private void UpdateBackgroundMaterial(SamplerBackground backgroundType, PostFilterType postFilterType, float textureAlpha)
    {
        circlePlane.Generate();

        if (backgroundType == SamplerBackground.Solid)
        {
            Material solidBackground = Instantiate(settings.materialAndTextureSettings.solidBackgroundMaterial);

            solidBackground.SetColor("_Color", settings.displaySettings.backgroundColor);

            rectRenderer.sharedMaterial = solidBackground;
            circleRenderer.sharedMaterial = solidBackground;            
        }        
       
        else if (backgroundType == SamplerBackground.Mask)
        {
            if (postFilterType == PostFilterType.GeneratedMask)
            {
                Material maskBackground = Instantiate(settings.materialAndTextureSettings.maskFilterBackgroundMaterial);

                maskBackground.SetColor("_Color", new Color(1.0f, 1.0f, 1.0f, textureAlpha));
                maskBackground.mainTexture = settings.materialAndTextureSettings.filterMaskTexture;

                rectRenderer.sharedMaterial = maskBackground;                
                circleRenderer.sharedMaterial = maskBackground;                
            }

            else if (postFilterType == PostFilterType.ImportedMask)
            {
                Material importedMaskBackground = Instantiate(settings.materialAndTextureSettings.importedMaskFilterBackgroundMaterial);

                importedMaskBackground.SetColor("_Color", new Color(1.0f, 1.0f, 1.0f, textureAlpha));                

                rectRenderer.sharedMaterial = importedMaskBackground;
                circleRenderer.sharedMaterial = importedMaskBackground;               
            }
        }

        rectRenderer.transform.localScale = new Vector3(20, 1, 20);
    }

    private void UpdateStatus(SamplerRegionType regionType, SamplerBackground backgroundType)
    {        
        if (backgroundType != SamplerBackground.None)
        {
            if (settings.coreSettings.regionType == SamplerRegionType.Rectangular)
            {
                circlePlane.gameObject.SetActive(false);
                renderPlane.SetActive(true);
            }

            else if (settings.coreSettings.regionType == SamplerRegionType.Circular)
            {
                renderPlane.SetActive(false);
                circlePlane.gameObject.SetActive(true);
            }

            else
            {
                renderPlane.SetActive(false);
                circlePlane.gameObject.SetActive(false);
            }
        }

        else
        {
            renderPlane.SetActive(false);
            circlePlane.gameObject.SetActive(false);
        }
    }
}
