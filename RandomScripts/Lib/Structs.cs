using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using System;

[System.Serializable]
public struct MapData
{
    public readonly MapDataParameter[] Parameters;
    
    public MapData(params MapDataParameter[] parameters)
    {
        this.Parameters = parameters;
    }

    public MapDataParameter GetParameterByType(MapDataType type)
    {
        if (this.Parameters.Length <= 0)
            return default;

        return this.Parameters.FirstOrDefault(t => t.DataType == type);
    }

    public MapDataParameter[] GetParametersByType(MapDataType type)
    {
        if (this.Parameters.Length <= 0)
            return default;

        return this.Parameters.Where(t => t.DataType == type).ToArray();
    }
}

[System.Serializable]
public struct MapDataParameter
{
    public readonly float[,] NoiseData;
    public readonly Color[] ColorData;    
    public readonly MapDataType DataType;
    public readonly TileData[] Data;

    public MapDataParameter(float[,] noiseData, Color[] colorData, MapDataType dataType, TileData[] data = null)
    {
        NoiseData = noiseData;
        ColorData = colorData;        
        DataType = dataType;
        Data = data;
    }
}

[System.Serializable]
public struct RenderPlane
{
    public string name;
    public Renderer renderer;
    public Texture2D texture;
    public GameObject plane;
    public RenderPlaneType type;    
    public bool isActive;
}

[System.Serializable]
public struct MapThreadInfo<T>
{
    public readonly Action<T> callback;
    public readonly T parameter;

    public MapThreadInfo(Action<T> callback, T parameter)
    {
        this.callback = callback;
        this.parameter = parameter;
    }
}

[System.Serializable]
public struct NoiseParameters
{
    public int index;            
    public string name;
    public bool isMaster;    
    public bool useMaster;           
    public NoiseMapType outputType;    
    [Range(0.0f, 100.0f)]
    public float scale;
    public FastNoise.FastNoiseLite.NoiseType type;
    [Range(1, 10)]
    public int octaves;
    public float frequency;
    [Range(1.0f, 100.0f)]
    public float lacunarity;
    [Range(0.0f, 1.0f)]
    public float persistance;
    public float jitter;
    public FastNoise.FastNoiseLite.CellularDistanceFunction cellularDistanceFunction;
    public FastNoise.FastNoiseLite.CellularReturnType CellularReturnType;
    public float pingPongStrength;
    public FastNoise.FastNoiseLite.DomainWarpType domainWarpType;
    public float domainWarpStrength;
    public FastNoise.FastNoiseLite.FractalType fractalType;
    public float fractalWeightStrength;
    public FastNoise.FastNoiseLite.RotationType3D rotationType3D;    
  
    public static NoiseParameters GetParametersByOutputType(NoiseParameters[] parameters, NoiseMapType type)
    {
        if (parameters != null || parameters.Length > 0)
        {
            if (parameters.Any(p => p.outputType == type))
            {
                NoiseParameters np = parameters.FirstOrDefault(p => p.outputType == type);
                return np;
            }
        }

        return default;
    }

    public static NoiseParameters GetFirstMaster(NoiseParameters[] parameters)
    {
        if (parameters != null || parameters.Length > 0)
        {
            if (parameters.Any(p => p.isMaster))
            {
                NoiseParameters np = parameters.FirstOrDefault(p => p.isMaster);
                return np;
            }
        }

        return default;
    }

    public static NoiseParameters FindByIndex(NoiseParameters[] parameters, int index, bool master = false)
    {
        if (parameters != null || parameters.Length > 0)
        {
            if (parameters.Any(p => p.index == index && p.isMaster == master))
            {
                NoiseParameters np = parameters.SingleOrDefault(p => p.index == index);
                return np;
            }
        }

        return default;
    }
}

[System.Serializable]
public struct TerrainFeature 
{
    public string name;
    public bool enabled;
    public FeatureType type;

    [Range(0.0f, 1.0f)]
    public float minimumHeightThreshold;
    [Range(0.0f, 1.0f)]
    public float maximumHeightThreshold;
}

[System.Serializable]
public struct TerrainFeatureParameters
{    
    public float terraceWidth;
    public float peakStrength;
}

[System.Serializable]
public struct HeightTypeValues
{
    [HideInInspector]
    public bool HasData;

    public float DeepWaterValue;
    public float DeepWaterTempOffset;
    public float DeepWaterMoistureOffset;
    public Color DeepWaterColor;

    public float ShallowWaterValue;
    public float ShallowWaterTempOffset;
    public float ShallowWaterMoistureOffset;
    public Color ShallowWaterColor;

    public float ShoreValue;
    public float ShoreTempOffset;
    public float ShoreMoistureOffset;
    public Color ShoreColor;

    public float SandValue;
    public float SandTempOffset;
    public float SandMoistureOffset;
    public Color SandColor;
    
    public float GrassValue;
    public float GrassTempOffset;
    public float GrassMoistureOffset;
    public Color GrassColor;
    
    public float ForestValue;
    public float ForestTempOffset;
    public float ForestMoistureOffset;
    public Color ForestColor;
    
    public float RockValue;
    public float RockTempOffset;
    public float RockMoistureOffset;
    public Color RockColor;

    public float SnowValue;
    public float SnowTempOffset;
    public float SnowMoistureOffset;
    public Color SnowColor;

    public static HeightTypeValues GetDefault()
    {
        return new HeightTypeValues()
        {
            HasData = true,

            DeepWaterValue = 0.1f,
            DeepWaterColor = new Color(0, 0, 0.5f, 1),
            DeepWaterTempOffset = 0f,
            DeepWaterMoistureOffset = 8f,
            
            ShallowWaterValue = 0.2f,
            ShallowWaterTempOffset = 0f,
            ShallowWaterMoistureOffset = 3f,
            ShallowWaterColor = new Color(25 / 255f, 25 / 255f, 150 / 255f, 1),

            ShoreValue = 0.3f,
            ShoreTempOffset = 0f,
            ShoreMoistureOffset = 1f,
            ShoreColor = new Color(240 / 255f, 240 / 255f, 64 / 255f, 1),

            SandValue = 0.5f,
            SandTempOffset = 0f,
            SandMoistureOffset = 0.25f,
            SandColor = new Color(240 / 255f, 240 / 255f, 64 / 255f, 1),

            GrassValue = 0.7f,
            GrassTempOffset = 0.1f,
            GrassMoistureOffset = 0f,
            GrassColor = new Color(50 / 255f, 220 / 255f, 20 / 255f, 1),

            ForestValue = 0.8f,
            ForestTempOffset = 0.2f,
            ForestMoistureOffset = 0f,
            ForestColor = new Color(16 / 255f, 160 / 255f, 0, 1),

            RockValue = 0.9f,
            RockTempOffset = 0.3f,
            RockMoistureOffset = 0f,
            RockColor = new Color(0.5f, 0.5f, 0.5f, 1),

            SnowValue = 1.0f,
            SnowTempOffset = 0.4f,
            SnowMoistureOffset = 0f,
            SnowColor = new Color(1, 1, 1, 1)
        };
    }
}

[System.Serializable]
public struct BiomeTypeColors
{
    [HideInInspector]
    public bool HasData;

    public Color Ice;
    public Color Desert;
    public Color Savanna;
    public Color TropicalRainforest;
    public Color Tundra;
    public Color TemperateRainforest;
    public Color Grassland;
    public Color SeasonalForest;
    public Color BorealForest;
    public Color Woodland;

    public static BiomeTypeColors GetDefault()
    {
        return new BiomeTypeColors()
        {
            HasData = true,

            Ice = Color.white,
            Desert = new Color(238 / 255f, 218 / 255f, 130 / 255f, 1),
            Savanna = new Color(177 / 255f, 209 / 255f, 110 / 255f, 1),
            TropicalRainforest = new Color(66 / 255f, 123 / 255f, 25 / 255f, 1),
            Tundra = new Color(96 / 255f, 131 / 255f, 112 / 255f, 1),
            TemperateRainforest = new Color(29 / 255f, 73 / 255f, 40 / 255f, 1),
            Grassland = new Color(164 / 255f, 225 / 255f, 99 / 255f, 1),
            SeasonalForest = new Color(73 / 255f, 100 / 255f, 35 / 255f, 1),
            BorealForest = new Color(95 / 255f, 115 / 255f, 62 / 255f, 1),
            Woodland = new Color(139 / 255f, 175 / 255f, 90 / 255f, 1),
        };
    }
}

[System.Serializable]
public struct HeatTypeValues
{
    [HideInInspector]
    public bool HasData;

    public float ColdestValue;
    public Color ColdestColor;
    
    public float ColderValue;
    public Color ColderColor;
    
    public float ColdValue;
    public Color ColdColor;

    public float WarmValue;
    public Color WarmColor;

    public float WarmerValue;
    public Color WarmerColor;

    public Color WarmestColor;

    public static HeatTypeValues GetDefault()
    {
        return new HeatTypeValues()
        {
            HasData = true,

            ColdestValue = 0.005f,
            ColdestColor = new Color(0, 1, 1, 1),

            ColderValue = 0.18f,
            ColderColor = new Color(170 / 255f, 1, 1, 1),
            
            ColdValue = 0.4f,
            ColdColor = new Color(0, 229/255f, 133/255f, 1),

            WarmValue = 0.6f,
            WarmColor = new Color(1, 1, 100/255f, 1),

            WarmerValue = 0.8f,
            WarmerColor = new Color(1, 100/255f, 0, 1),

            WarmestColor = new Color(241/255f, 12/255f, 0, 1)
        };
    }
}

[System.Serializable]
public struct MoistureTypeValues
{
    [HideInInspector]
    public bool HasData;

    public Color DryestColor;

    public float DryerValue;
    public Color DryerColor;

    public float DryValue;
    public Color DryColor;

    public float WetValue;
    public Color WetColor;

    public float WetterValue;
    public Color WetterColor;

    public float WettestValue;
    public Color WettestColor;

    public static MoistureTypeValues GetDefault()
    {
        return new MoistureTypeValues()
        {
            HasData = true,

            DryestColor = new Color(255 / 255f, 139 / 255f, 17 / 255f, 1),

            DryerValue = 0.27f,
            DryerColor = new Color(245 / 255f, 245 / 255f, 23 / 255f, 1),

            DryValue = 0.4f,
            DryColor = new Color(80 / 255f, 255 / 255f, 0 / 255f, 1),

            WetValue = 0.6f,
            WetColor = new Color(85 / 255f, 255 / 255f, 255 / 255f, 1),

            WetterValue = 0.8f,
            WetterColor = new Color(20 / 255f, 70 / 255f, 255 / 255f, 1),

            WettestValue = 0.9f,
            WettestColor = new Color(0 / 255f, 0 / 255f, 100 / 255f, 1)
        };
    }
}

[System.Serializable]
public struct TileData
{    
    public int x;
    public int y;
    public int index;
    public float heightValue;
    public HeightType heightType;
    public TerrainGroupType terainGroup;
    public float moistureValue;
    public MoistureType moistureType;
    public float heatValue;
    public HeatType heatType;
    public BiomeType biomeType;    

    public static BiomeType[,] BiomeTable()
    {
        BiomeType[,] table = new BiomeType[6, 6] {   
            //COLDEST        //COLDER          //COLD                  //HOT                          //HOTTER                       //HOTTEST
            { BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Desert,              BiomeType.Desert,              BiomeType.Desert },              //DRYEST
            { BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Desert,              BiomeType.Desert,              BiomeType.Desert },              //DRYER
            { BiomeType.Ice, BiomeType.Tundra, BiomeType.Woodland,     BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Savanna },             //DRY
            { BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Savanna },             //WET
            { BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.SeasonalForest,      BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest },  //WETTER
            { BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.TemperateRainforest, BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest }   //WETTEST
        };

        return table;
    }

    public static BiomeType GetBiomeType(TileData tile)
    {
        return BiomeTable()[(int)tile.moistureType, (int)tile.heatType];
    }
}

[System.Serializable]
public struct HeightAndGradient
{
    public float height;
    public float gradientX;
    public float gradientY;
}

[System.Serializable]
public struct SamplePoint
{
    public Vector3 position;
    public Vector3 normal;

    public SamplePoint(Vector3 position, Vector3 normal)
    {
        this.position = position;
        this.normal = normal;        
    }
}