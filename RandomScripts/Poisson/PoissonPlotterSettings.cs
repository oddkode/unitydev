using UnityEngine;

[System.Serializable]
public class PoissonPlotterSettings
{
    [Header("General")]
    [Space()]
    public string name;
    public int seed;    
    public GameObject target;    
    public GameObject spawnItemPrefab;
    [Range(0.1f, 100.0f)]
    public float sampleRadius = 10.0f;
    [Range(0, 100000)]
    public int maxSamples;
    
    [Header("Flags")]
    [Space()]    
    public bool active;
    public bool realTime;
    public bool overrideParent;
    [Space()]
    public bool orientItemToLandscape;
    public bool limitInstantiationBySlope;
    public bool preventOverlap;
    public bool useRandomOverlapRadius;
    public bool jitterRotation;
    public bool encourageClumping;
    [Space()]
    public bool showSampleBounds;
    public bool showSamplePoints;
    public bool showSampleRays;
    public bool showOverlapRadius;
    public bool showClumpingRadius;

    [Header("Placement")]
    [Space()]
    public Vector3 jitterRotationRange;        
    public float maxSlopeAngle;
    public float overlapRadius;
    public float randomOverlapMinimumRadius;
    public float randomOverlapMaximumRadius;
    public float clumpingRadius;    
    public float clumpingStrength;

    [Header("Display")]
    [Space()]
    public Vector2 manualSampleRegionSize;   
    [ColorUsage(true)]
    public Color sampleGizmoColor;
    [ColorUsage(true)]
    public Color sampleBoundsColor;
    [ColorUsage(true)]
    public Color sampleRayColor;
    [ColorUsage(true)]
    public Color overlapGizmoColor;
    [ColorUsage(true)]
    public Color clumpingGizmoColor;
    [Range(0.1f, 25f)]
    public float sampleGizmoSize;    
}
