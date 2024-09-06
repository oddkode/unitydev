using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum TerrainGroupType : int
{
    Water,
    Land
}

[System.Serializable]
public enum HeightType : int
{
    DeepWater,
    ShallowWater,
    Shore,
    Sand,
    Grass,
    Forest,
    Rock,
    Snow
}

[System.Serializable]
public enum MeshTextureType : int
{
    Noise,
    Color,
    Heat,
    Moisture,
    Biomes
}

[System.Serializable]
public enum RenderPlaneType : int
{
    Noise,
    Color,
    Heat,
    Moisture,
    Biomes,
    Mesh
}

[System.Serializable]
public enum NoiseMapType : int
{
    Height,
    Heat,
    Moisture,
    Falloff
}

[System.Serializable]
public enum HeatMapType : int
{
    Random,
    Lateral,
    Combined
}

[System.Serializable]
public enum NoiseGenerator : int
{
    Unity,
    FastNoise
}

[System.Serializable]
public enum MapDataType : int
{
    HeightNoise,    
    HeatNoise,
    LateralHeatNoise,
    RandomHeatNoise,
    MoistureNoise,
    HeightMap,
    ErodedHeightMap,
    ColorMap,
    HeatMap,
    LateralHeatMap,
    RandomHeatMap,
    MoistureMap,
    BiomeMap,        
    TileData
}

[System.Serializable]
public enum BiomeType : int
{
    Desert,
    Savanna,
    TropicalRainforest,
    Grassland,
    Woodland,
    SeasonalForest,
    TemperateRainforest,
    BorealForest,
    Tundra,
    Ice
}

[System.Serializable]
public enum HeatType : int
{
    Coldest,
    Colder,
    Cold,
    Warm,
    Warmer,
    Warmest
}

[System.Serializable]
public enum MoistureType : int
{
    Wettest,
    Wetter,
    Wet,
    Dry,
    Dryer,
    Dryest
}

[System.Serializable]
public enum FeatureType : int
{
    None,
    RidgedNoise,
    Terraces,
    Caverns,
    Valleys,
    Canyons,
    Rivers,
    Erosion
}

public enum SamplerBackground : int
{
    None,
    Solid,        
    Mask
}

[System.Serializable]
public enum SamplerRegionType : int
{
    Rectangular,
    Circular
}

[System.Serializable]
public enum PostFilterType : int
{
    None,        
    GeneratedMask,
    ImportedMask
}

[System.Serializable]
public enum PostFilterMaskType : int
{
    LinearRectangle,
    SolidRectangle,
    LinearCircle,
    SolidCircle,
    SolidTriangle,    
    SolidDiamond,  
    HorizontalLinear,
    VerticalLinear,
    Checkerboard,
    CircleGrid,
    CircleTriangleGrid,    
    Noise
}

[System.Serializable]
public enum FilterType : int
{
    Area,
    Post
}