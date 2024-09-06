using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour
{    
    public enum HeatMapDirection : int
    {
        Longitudinal,
        Latitudinal
    }    

    public enum DrawMode : int
    {
        NoiseMap,        
        ColorMap,
        HeatMap,
        MoistureMap,
        BiomeMap,
        ColorHeatMoisture,
        ColorHeatMoistureBiomes,
        AllExceptMesh,
        Mesh
    }

    [Space()]
    [Header("Detail")]
    [Range(0, 6)]
    public int previewLevelOfDetail;
    [Header("Falloff")]
    public bool useFalloff;
    public float fallOffStrength = 3f;
    public float fallOffFade = 2.2f;
    [Header("Display Options")]
    public DrawMode drawMode;
    public MeshTextureType meshTextureType;
    public NoiseMapType noisePreviewType;
    public Noise.NormalizeMode noiseNormalizationMode;
    public HeatMapType heatMapPreviewType;    
    public bool autoUpdate;    
    
    [Space()]
    [Header("Map Attributes")]
    public string seedVal;
    public const int mapChunkSize = 241;
    public RenderPlane[] renderPlanes;
    public GameObject viewer;    

    [Space()]
    [Header("Heat Map")]
    public HeatMapDirection heatMapDirection;
    public float heatMapCenterPoint;
    public float heatMapOffset;
    public float heatMapDistance;
    public bool useLateralMap;
    public bool useAvgHeatWithHeight;
    public bool useMeanHeat;
    public bool useHeatCurve;
    public HeatTypeValues heatLevels;
    public AnimationCurve heatCurve;

    [Space()]
    [Header("Moisture Map")]
    public bool useMoistureCurve;
    public bool averageMoistureWithHeight;
    public MoistureTypeValues moistureLevels;
    public AnimationCurve moistureCurve;

    [Space()]
    [Header("Noise Parameters")]       
    public float meshHeightMultiplier;
    [SerializeField]
    public float2 offset;
    public float normalizationFactor;
    public bool normalizeAmplitudes;
    public AnimationCurve meshHeightCurve;
    [HideInInspector]
    public int masterNoiseIndex;

    [Space()]
    [Header("Erosion Parameters")]
    public bool enableErosion;
    public bool autoUpdateErosion;    
    public int numberOfIterations = 1;

    [Space()]
    [Header("Features and Feature Parameters")]
    public TerrainFeature[] features;
    public TerrainFeatureParameters parameters;
        
    public NoiseGenerator noiseGenerator;
    public NoiseParameters[] noiseParameters;

    [Space()]
    [Header("Regions, Types and Biomes")]
    public HeightTypeValues heightLevels;    
    public BiomeTypeColors biomeColors;            

    [HideInInspector]
    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    
    [HideInInspector]
    private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    [HideInInspector]
    private float[,] fallOffNoise;    

    public void RequestMapData(float2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    private void MapDataThread(float2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center, enableErosion);
        
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {        
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.GetParameterByType(MapDataType.HeightNoise).NoiseData, meshHeightMultiplier, meshHeightCurve, lod);

        lock(meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    [ExecuteInEditMode]
    private void Awake()
    {
        fallOffNoise = FalloffGenerator.GenerateFalloffMap(mapChunkSize, fallOffStrength, fallOffFade);       
    }

    private void Update()
    {
        if(mapDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if(meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }    

    private MapData GenerateMapData(float2 center, bool runErosion = false)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, SeedCreator.CreateFromString(seedVal), center + offset, NoiseParameters.GetParametersByOutputType(noiseParameters, NoiseMapType.Height), noiseGenerator, noiseNormalizationMode, normalizationFactor, normalizeAmplitudes, features, parameters);
        float[] erodedMap = noiseMap.To1DArray(mapChunkSize);
        
        if(runErosion)
        {
            MapErosion erosion = GetComponent<MapErosion>();
            erosion.Erode(erodedMap, mapChunkSize, numberOfIterations);
            noiseMap = erodedMap.To2DArray(mapChunkSize);
        }

        if (!heightLevels.HasData)
        {
            heightLevels = HeightTypeValues.GetDefault();
        }

        if (!heatLevels.HasData)
        {
            heatLevels = HeatTypeValues.GetDefault();
        }

        if (!moistureLevels.HasData)
        {
            moistureLevels = MoistureTypeValues.GetDefault();
        }

        if (!biomeColors.HasData)
        {
            biomeColors = BiomeTypeColors.GetDefault();
        }

        System.Random prng = new System.Random(SeedCreator.CreateFromString(seedVal));

        int randomMoistureSeedOffset = prng.Next();
        int randomHeatSeedOffset = prng.Next();

        // Heat mapping
        float[,] lateralHeatNoise = Noise.GenerateUniformNoiseMap(mapChunkSize, heatMapCenterPoint, heatMapOffset, heatMapDistance);
        float[,] randomHeatNoise = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, SeedCreator.CreateFromString(seedVal) + randomHeatSeedOffset, center + offset, NoiseParameters.GetParametersByOutputType(noiseParameters, NoiseMapType.Heat), noiseGenerator, noiseNormalizationMode, normalizationFactor, normalizeAmplitudes);
        float[,] heatNoise = new float[mapChunkSize, mapChunkSize];

        // Moisture mapping        
        float[,] moistureNoise = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, SeedCreator.CreateFromString(seedVal) + randomMoistureSeedOffset, center + offset, NoiseParameters.GetParametersByOutputType(noiseParameters, NoiseMapType.Moisture), noiseGenerator, noiseNormalizationMode, normalizationFactor, normalizeAmplitudes);        

        TileData[] tileData = new TileData[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                int dataIndex = y * mapChunkSize + x;
                TileData td = new TileData() { x = x, y = y, index = dataIndex };

                if (useFalloff)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - fallOffNoise[x, y]);
                }

                float currentHeight = noiseMap[x, y];
                td.heightValue = currentHeight;

                if (currentHeight < heightLevels.ShallowWaterValue)
                {
                    td.heightType = HeightType.DeepWater;
                    td.terainGroup = TerrainGroupType.Water;
                }

                else if (currentHeight < heightLevels.ShoreValue)
                {
                    td.heightType = HeightType.ShallowWater;
                    td.terainGroup = TerrainGroupType.Water;
                }

                else if (currentHeight < heightLevels.SandValue)
                {
                    td.heightType = HeightType.Shore;
                    td.terainGroup = TerrainGroupType.Land;
                }

                else if (currentHeight < heightLevels.GrassValue)
                {
                    td.heightType = HeightType.Sand;
                    td.terainGroup = TerrainGroupType.Land;
                }

                else if (currentHeight < heightLevels.ForestValue)
                {
                    td.heightType = HeightType.Grass;
                    td.terainGroup = TerrainGroupType.Land;
                }

                else if (currentHeight < heightLevels.RockValue)
                {
                    td.heightType = HeightType.Forest;
                    td.terainGroup = TerrainGroupType.Land;
                }

                else if (currentHeight < heightLevels.SnowValue)
                {
                    td.heightType = HeightType.Rock;
                    td.terainGroup = TerrainGroupType.Land;
                }

                else
                {
                    td.heightType = HeightType.Snow;
                    td.terainGroup = TerrainGroupType.Land;
                }

                if (useLateralMap)
                {
                    if (useAvgHeatWithHeight)
                    {
                        heatNoise[x, y] = lateralHeatNoise[x, y] * randomHeatNoise[x, y];
                        
                        // Adjust Heat Map based on Height - Higher == colder
                        if (td.heightType == HeightType.Grass)
                        {
                            heatNoise[x, y] -= heightLevels.GrassTempOffset * td.heightValue;
                        }
                        else if (td.heightType == HeightType.Forest)
                        {
                            heatNoise[x, y] -= heightLevels.ForestTempOffset * td.heightValue;
                        }
                        else if (td.heightType == HeightType.Rock)
                        {
                            heatNoise[x, y] -= heightLevels.RockTempOffset * td.heightValue;
                        }
                        else if (td.heightType == HeightType.Snow)
                        {
                            heatNoise[x, y] -= heightLevels.SnowTempOffset * td.heightValue;
                        }                        
                    }

                    else
                    {
                        heatNoise[x, y] = lateralHeatNoise[x, y] * randomHeatNoise[x, y];
                    }
                }

                else
                {
                    if (useAvgHeatWithHeight)
                    {                                                                        
                        // Adjust Heat Map based on Height - Higher == colder
                        if (td.heightType == HeightType.Grass)
                        {
                            heatNoise[x, y] -= heightLevels.GrassTempOffset * td.heightValue;
                        }
                        else if (td.heightType == HeightType.Forest)
                        {
                            heatNoise[x, y] -= heightLevels.ForestTempOffset * td.heightValue;
                        }
                        else if (td.heightType == HeightType.Rock)
                        {
                            heatNoise[x, y] -= heightLevels.RockTempOffset * td.heightValue;
                        }
                        else if (td.heightType == HeightType.Snow)
                        {
                            heatNoise[x, y] -= heightLevels.SnowTempOffset * td.heightValue;
                        }
                    }

                    else
                    {
                        if (useMeanHeat)
                        {
                            heatNoise[x, y] = 1.0f - currentHeight * randomHeatNoise[x, y];
                        }

                        else
                        {
                            heatNoise[x, y] = randomHeatNoise[x, y];
                        }
                    }
                }

                if (useHeatCurve)
                {
                    heatNoise[x, y] += heatCurve.Evaluate(currentHeight) * currentHeight;
                }

                float currentHeat = heatNoise[x, y];

                if (useMoistureCurve)
                {
                    moistureNoise[x, y] += moistureCurve.Evaluate(currentHeight) * currentHeight;
                }

                float currentMoisture = 0f;

                if (averageMoistureWithHeight)
                {
                    // Adjust moisture based on height
                    if (td.heightType == HeightType.DeepWater)
                    {
                        moistureNoise[x, y] += heightLevels.DeepWaterMoistureOffset * td.heightValue;
                    }
                    else if (td.heightType == HeightType.ShallowWater)
                    {
                        moistureNoise[x, y] += heightLevels.ShallowWaterMoistureOffset * td.heightValue;
                    }
                    else if (td.heightType == HeightType.Shore)
                    {
                        moistureNoise[x, y] += heightLevels.ShoreMoistureOffset * td.heightValue;
                    }
                    else if (td.heightType == HeightType.Sand)
                    {
                        moistureNoise[x, y] += heightLevels.SandMoistureOffset * td.heightValue;
                    }
                }

                else
                {
                    currentMoisture = moistureNoise[x, y];
                }

                if (useFalloff)
                {
                    currentHeat = Mathf.Clamp01(currentHeat - fallOffNoise[x, y]);
                    currentMoisture = Mathf.Clamp01(currentMoisture - fallOffNoise[x, y]);
                }

                td.heatValue = currentHeat;
                td.moistureValue = currentMoisture;

                if(currentHeat < heatLevels.ColdestValue)
                {
                    td.heatType = HeatType.Coldest;                    
                }

                else if(currentHeat < heatLevels.ColderValue)
                {
                    td.heatType = HeatType.Colder;
                }

                else if(currentHeat < heatLevels.ColdValue)
                {
                    td.heatType = HeatType.Cold;
                }

                else if(currentHeat < heatLevels.WarmValue)
                {
                    td.heatType = HeatType.Warm;
                }

                else if(currentHeat < heatLevels.WarmerValue)
                {
                    td.heatType = HeatType.Warmer;
                }

                else
                {
                    td.heatType = HeatType.Warmest;
                }

                if(currentMoisture < moistureLevels.DryerValue)
                {
                    td.moistureType = MoistureType.Dryest;
                }

                else if(currentMoisture < moistureLevels.DryValue)
                {
                    td.moistureType = MoistureType.Dryer;
                }

                else if(currentMoisture < moistureLevels.WetValue)
                {
                    td.moistureType = MoistureType.Dry;
                }

                else if(currentMoisture < moistureLevels.WetterValue)
                {
                    td.moistureType = MoistureType.Wet;
                }

                else if(currentMoisture < moistureLevels.WettestValue)
                {
                    td.moistureType = MoistureType.Wetter;
                }

                else
                {
                    td.moistureType = MoistureType.Wettest;
                }

                td.biomeType = TileData.GetBiomeType(td);
                tileData[td.index] = td;
            }
        }

        Color[] colorMap = BuildColorMap(tileData);
        Color[] heatMap = BuildHeatMap(tileData);        
        Color[] moistureMap = BuildMoistureMap(tileData);
        Color[] randomHeatMap = BuildHeatMapFromNoise(randomHeatNoise);
        Color[] lateralHeatMap = BuildHeatMapFromNoise(lateralHeatNoise);
        Color[] heightMap = TextureGenerator.ColorFromHeightMap(noiseMap);        
        Color[] biomeMap = BuildBiomeMap(tileData);        

        MapData data = new MapData(
            new MapDataParameter(noiseMap, null, MapDataType.HeightNoise),            
            new MapDataParameter(heatNoise, null, MapDataType.HeatNoise),
            new MapDataParameter(lateralHeatNoise, null, MapDataType.LateralHeatNoise),
            new MapDataParameter(randomHeatNoise, null, MapDataType.RandomHeatNoise),
            new MapDataParameter(moistureNoise, null, MapDataType.MoistureNoise),
            new MapDataParameter(null, heightMap, MapDataType.HeightMap),            
            new MapDataParameter(null, colorMap, MapDataType.ColorMap),
            new MapDataParameter(null, heatMap, MapDataType.HeatMap),
            new MapDataParameter(null, lateralHeatMap, MapDataType.LateralHeatMap),
            new MapDataParameter(null, randomHeatMap, MapDataType.RandomHeatMap),
            new MapDataParameter(null, moistureMap, MapDataType.MoistureMap),
            new MapDataParameter(null, biomeMap, MapDataType.BiomeMap),
            new MapDataParameter(null, null, MapDataType.TileData, tileData)
        );

        return data;
    }

    public void GenerateMapInEditor(bool runErosion = false)
    {
        MapData data = GenerateMapData(Vector2.zero, true);        

        float[,] noiseMap = data.GetParameterByType(MapDataType.HeightNoise).NoiseData;

        // Heat mapping
        float[,] lateralHeatNoise = data.GetParameterByType(MapDataType.LateralHeatNoise).NoiseData;
        float[,] randomHeatNoise = data.GetParameterByType(MapDataType.RandomHeatNoise).NoiseData;
        float[,] heatNoise = data.GetParameterByType(MapDataType.HeatNoise).NoiseData;        

        // Moisture mapping        
        float[,] moistureNoise = data.GetParameterByType(MapDataType.MoistureNoise).NoiseData;

        Color[] heightMap = data.GetParameterByType(MapDataType.HeightMap).ColorData;
        Texture2D falloffMap = TextureGenerator.TextureFromHeightMap(fallOffNoise);
        Color[] colorMap = data.GetParameterByType(MapDataType.ColorMap).ColorData;
        Color[] heatMap = data.GetParameterByType(MapDataType.HeatMap).ColorData;
        Color[] lateralHeatMap = data.GetParameterByType(MapDataType.LateralHeatMap).ColorData;
        Color[] randomHeatMap = data.GetParameterByType(MapDataType.RandomHeatMap).ColorData;
        Color[] moistureMap = data.GetParameterByType(MapDataType.MoistureMap).ColorData;
        Color[] biomeMap = data.GetParameterByType(MapDataType.BiomeMap).ColorData;        

        MapDisplay display = FindObjectOfType<MapDisplay>();
        

        if (drawMode == DrawMode.NoiseMap)
        {
            Texture2D noiseTexture;

            if(noisePreviewType == NoiseMapType.Falloff)
            {
                noiseTexture = falloffMap;
            }

            else if(noisePreviewType == NoiseMapType.Heat)
            {
                if(heatMapPreviewType == HeatMapType.Random)
                {
                    noiseTexture = TextureGenerator.TextureFromHeightMap(randomHeatNoise);
                }

                else if(heatMapPreviewType == HeatMapType.Lateral)
                {
                    noiseTexture = TextureGenerator.TextureFromHeightMap(lateralHeatNoise);
                }

                else
                {
                    noiseTexture = TextureGenerator.TextureFromHeightMap(heatNoise);
                }
            }            

            else if (noisePreviewType == NoiseMapType.Moisture)
            {
                noiseTexture = TextureGenerator.TextureFromHeightMap(moistureNoise);
            }

            else
            {
                noiseTexture = TextureGenerator.TextureFromHeightMap(noiseMap);
            }            

            if(renderPlanes != null && renderPlanes.Length > 0)
            {
                for(int i = 0; i < renderPlanes.Length; i++)
                {
                    if(renderPlanes[i].type == RenderPlaneType.Noise)
                    {
                        renderPlanes[i].texture = noiseTexture;

                        if (!renderPlanes[i].plane.activeInHierarchy)
                        {
                            renderPlanes[i].isActive = true;
                            renderPlanes[i].plane.SetActive(true);                            
                        }
                    }

                    else
                    {
                        renderPlanes[i].isActive = false;
                        renderPlanes[i].plane.SetActive(false);
                    }
                }
            }

            display.DrawTexturesFromRenderPlanes(renderPlanes);

            if (viewer.activeInHierarchy)
            {
                viewer.SetActive(false);
            }            
        }

        else if(drawMode == DrawMode.ColorMap)
        {            
            if (renderPlanes != null && renderPlanes.Length > 0)
            {
                for (int i = 0; i < renderPlanes.Length; i++)
                {
                    if (renderPlanes[i].type == RenderPlaneType.Color)
                    {
                        renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize);
                        
                        if (!renderPlanes[i].plane.activeInHierarchy)
                        {
                            renderPlanes[i].isActive = true;
                            renderPlanes[i].plane.SetActive(true);
                        }
                    }

                    else
                    {
                        renderPlanes[i].isActive = false;
                        renderPlanes[i].plane.SetActive(false);
                    }
                }
            }

            display.DrawTexturesFromRenderPlanes(renderPlanes);

            if (viewer.activeInHierarchy)
            {
                viewer.SetActive(false);
            }
        }

        else if (drawMode == DrawMode.HeatMap)
        {
            Texture2D heatMapTexture;

            if (heatMapPreviewType == HeatMapType.Random)
            {
                heatMapTexture = TextureGenerator.TextureFromColorMap(randomHeatMap, mapChunkSize, mapChunkSize);
            }

            else if (heatMapPreviewType == HeatMapType.Lateral)
            {
                heatMapTexture = TextureGenerator.TextureFromColorMap(lateralHeatMap, mapChunkSize, mapChunkSize);
            }

            else
            {
                heatMapTexture = TextureGenerator.TextureFromColorMap(heatMap, mapChunkSize, mapChunkSize);
            }            

            if (renderPlanes != null && renderPlanes.Length > 0)
            {
                for (int i = 0; i < renderPlanes.Length; i++)
                {
                    if (renderPlanes[i].type == RenderPlaneType.Heat)
                    {
                        renderPlanes[i].texture = heatMapTexture;
                        
                        if (!renderPlanes[i].plane.activeInHierarchy)
                        {
                            renderPlanes[i].isActive = true;
                            renderPlanes[i].plane.SetActive(true);
                        }
                    }

                    else
                    {
                        renderPlanes[i].isActive = false;
                        renderPlanes[i].plane.SetActive(false);
                    }
                }
            }

            display.DrawTexturesFromRenderPlanes(renderPlanes);

            if (viewer.activeInHierarchy)
            {
                viewer.SetActive(false);
            }
        }

        else if (drawMode == DrawMode.MoistureMap)
        {            
            if (renderPlanes != null && renderPlanes.Length > 0)
            {
                for (int i = 0; i < renderPlanes.Length; i++)
                {
                    if (renderPlanes[i].type == RenderPlaneType.Moisture)
                    {
                        renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(moistureMap, mapChunkSize, mapChunkSize);

                        if (!renderPlanes[i].plane.activeInHierarchy)
                        {
                            renderPlanes[i].isActive = true;
                            renderPlanes[i].plane.SetActive(true);
                        }
                    }

                    else
                    {
                        renderPlanes[i].isActive = false;
                        renderPlanes[i].plane.SetActive(false);
                    }
                }
            }

            display.DrawTexturesFromRenderPlanes(renderPlanes);

            if (viewer.activeInHierarchy)
            {
                viewer.SetActive(false);
            }
        }

        else if (drawMode == DrawMode.BiomeMap)
        {                                   
            if (renderPlanes != null && renderPlanes.Length > 0)
            {
                for (int i = 0; i < renderPlanes.Length; i++)
                {
                    if (renderPlanes[i].type == RenderPlaneType.Biomes)
                    {                       
                        renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(biomeMap, mapChunkSize, mapChunkSize);
                       
                        if (!renderPlanes[i].plane.activeInHierarchy)
                        {
                            renderPlanes[i].isActive = true;
                            renderPlanes[i].plane.SetActive(true);
                        }
                    }

                    else
                    {
                        renderPlanes[i].isActive = false;
                        renderPlanes[i].plane.SetActive(false);
                    }
                }
            }

            display.DrawTexturesFromRenderPlanes(renderPlanes);

            if (viewer.activeInHierarchy)
            {
                viewer.SetActive(false);
            }
        }

        else if(drawMode == DrawMode.ColorHeatMoisture)
        {            
            if (renderPlanes != null && renderPlanes.Length > 0)
            {
                for (int i = 0; i < renderPlanes.Length; i++)
                {                                        
                    if (renderPlanes[i].type == RenderPlaneType.Color || renderPlanes[i].type == RenderPlaneType.Heat || renderPlanes[i].type == RenderPlaneType.Moisture)
                    {
                        if(renderPlanes[i].type == RenderPlaneType.Color) 
                        {
                            renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize);
                        }

                        else if (renderPlanes[i].type == RenderPlaneType.Heat)
                        {
                            if (heatMapPreviewType == HeatMapType.Random)
                            {
                                renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(randomHeatMap, mapChunkSize, mapChunkSize);
                            }

                            else if (heatMapPreviewType == HeatMapType.Lateral)
                            {
                                renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(lateralHeatMap, mapChunkSize, mapChunkSize);
                            }

                            else
                            {
                                renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(heatMap, mapChunkSize, mapChunkSize);
                            }                            
                        }

                        else if (renderPlanes[i].type == RenderPlaneType.Moisture)
                        {
                            renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(moistureMap, mapChunkSize, mapChunkSize);
                        }

                        if (!renderPlanes[i].plane.activeInHierarchy)
                        {
                            renderPlanes[i].isActive = true;
                            renderPlanes[i].plane.SetActive(true);
                        }
                    }

                    else
                    {
                        renderPlanes[i].isActive = false;
                        renderPlanes[i].plane.SetActive(false);
                    }
                }

                display.DrawTexturesFromRenderPlanes(renderPlanes);
            }

            if (viewer.activeInHierarchy)
            {
                viewer.SetActive(false);
            }
        }

        else if (drawMode == DrawMode.ColorHeatMoistureBiomes)
        {
            if (renderPlanes != null && renderPlanes.Length > 0)
            {
                for (int i = 0; i < renderPlanes.Length; i++)
                {                    
                    if (renderPlanes[i].type != RenderPlaneType.Noise || renderPlanes[i].type != RenderPlaneType.Mesh)
                    {
                        if (renderPlanes[i].type == RenderPlaneType.Color)
                        {
                            renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize);
                        }

                        else if (renderPlanes[i].type == RenderPlaneType.Heat)
                        {
                            if (heatMapPreviewType == HeatMapType.Random)
                            {
                                renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(randomHeatMap, mapChunkSize, mapChunkSize);
                            }

                            else if (heatMapPreviewType == HeatMapType.Lateral)
                            {
                                renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(lateralHeatMap, mapChunkSize, mapChunkSize);
                            }

                            else
                            {
                                renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(heatMap, mapChunkSize, mapChunkSize);
                            }
                        }

                        else if (renderPlanes[i].type == RenderPlaneType.Moisture)
                        {
                            renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(moistureMap, mapChunkSize, mapChunkSize);
                        }

                        else if (renderPlanes[i].type == RenderPlaneType.Biomes)
                        {                           
                            renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(biomeMap, mapChunkSize, mapChunkSize);                           
                        }

                        if (!renderPlanes[i].plane.activeInHierarchy)
                        {
                            renderPlanes[i].isActive = true;
                            renderPlanes[i].plane.SetActive(true);
                        }
                    }

                    else
                    {
                        renderPlanes[i].isActive = false;
                        renderPlanes[i].plane.SetActive(false);
                    }
                }

                display.DrawTexturesFromRenderPlanes(renderPlanes);
            }

            if (viewer.activeInHierarchy)
            {
                viewer.SetActive(false);
            }
        }

        else if (drawMode == DrawMode.AllExceptMesh)
        {
            if (renderPlanes != null && renderPlanes.Length > 0)
            {
                for (int i = 0; i < renderPlanes.Length; i++)
                {
                    if (renderPlanes[i].type != RenderPlaneType.Mesh)
                    {
                        if(renderPlanes[i].type == RenderPlaneType.Noise)
                        {
                            Color[] noiseMapColors;

                            if (noisePreviewType == NoiseMapType.Heat)
                            {
                                noiseMapColors = TextureGenerator.ColorFromHeightMap(randomHeatNoise);
                            }

                            else if (noisePreviewType == NoiseMapType.Moisture)
                            {
                                noiseMapColors = TextureGenerator.ColorFromHeightMap(moistureNoise);
                            }                            

                            else
                            {
                                noiseMapColors = TextureGenerator.ColorFromHeightMap(noiseMap);
                            }

                            if (noisePreviewType != NoiseMapType.Falloff)
                            {
                                renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(noiseMapColors, mapChunkSize, mapChunkSize);
                            }

                            else
                            {
                                renderPlanes[i].texture = falloffMap;
                            }
                        }

                        else if (renderPlanes[i].type == RenderPlaneType.Color)
                        {
                            renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize);
                        }

                        else if (renderPlanes[i].type == RenderPlaneType.Heat)
                        {
                            if (heatMapPreviewType == HeatMapType.Random)
                            {
                                renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(randomHeatMap, mapChunkSize, mapChunkSize);
                            }

                            else if (heatMapPreviewType == HeatMapType.Lateral)
                            {
                                renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(lateralHeatMap, mapChunkSize, mapChunkSize);
                            }

                            else
                            {
                                renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(heatMap, mapChunkSize, mapChunkSize);
                            }
                        }

                        else if (renderPlanes[i].type == RenderPlaneType.Moisture)
                        {
                            renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(moistureMap, mapChunkSize, mapChunkSize);
                        }

                        else if (renderPlanes[i].type == RenderPlaneType.Biomes)
                        {                            
                            renderPlanes[i].texture = TextureGenerator.TextureFromColorMap(biomeMap, mapChunkSize, mapChunkSize);                            
                        }

                        if (!renderPlanes[i].plane.activeInHierarchy)
                        {
                            renderPlanes[i].isActive = true;
                            renderPlanes[i].plane.SetActive(true);
                        }
                    }

                    else
                    {
                        renderPlanes[i].isActive = false;
                        renderPlanes[i].plane.SetActive(false);
                    }
                }

                display.DrawTexturesFromRenderPlanes(renderPlanes);
            }

            if (viewer.activeInHierarchy)
            {
                viewer.SetActive(false);
            }
        }

        else if (drawMode == DrawMode.Mesh)
        {
            Texture2D meshTexture;

            switch(meshTextureType)
            {
                case MeshTextureType.Noise:
                    meshTexture = TextureGenerator.TextureFromHeightMap(noiseMap);
                    break;
                case MeshTextureType.Color:
                    meshTexture = TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize);
                    break;
                case MeshTextureType.Heat:
                    if(heatMapPreviewType == HeatMapType.Random)
                    {
                        meshTexture = TextureGenerator.TextureFromColorMap(randomHeatMap, mapChunkSize, mapChunkSize);
                    }
                    else if(heatMapPreviewType == HeatMapType.Lateral)
                    {
                        meshTexture = TextureGenerator.TextureFromColorMap(lateralHeatMap, mapChunkSize, mapChunkSize);
                    }

                    else
                    {
                        meshTexture = TextureGenerator.TextureFromColorMap(heatMap, mapChunkSize, mapChunkSize);
                    }                    
                    break;
                case MeshTextureType.Moisture:
                    meshTexture = TextureGenerator.TextureFromColorMap(moistureMap, mapChunkSize, mapChunkSize);
                    break;
                case MeshTextureType.Biomes:
                    meshTexture = TextureGenerator.TextureFromColorMap(biomeMap, mapChunkSize, mapChunkSize);
                    break;
                default:
                    meshTexture = TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize);
                    break;
            }
            
            if (renderPlanes != null && renderPlanes.Length > 0)
            {
                for (int i = 0; i < renderPlanes.Length; i++)
                {
                    if (renderPlanes[i].type == RenderPlaneType.Mesh)
                    {
                        if (!renderPlanes[i].plane.activeInHierarchy)
                        {
                            renderPlanes[i].isActive = true;
                            renderPlanes[i].plane.SetActive(true);                            
                        }
                    }

                    else
                    {
                        renderPlanes[i].isActive = false;
                        renderPlanes[i].plane.SetActive(false);
                    }
                }
            }

            if (!viewer.activeInHierarchy)
            {
                viewer.SetActive(true);
            }
                   
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, previewLevelOfDetail), meshTexture);
          
        }        
    }

    private Color[] BuildColorMap(TileData[] tileData)
    {
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];

        for (int i = 0; i < tileData.Length; i++)
        {            
            switch (tileData[i].heightType)
            {
                case HeightType.DeepWater:
                    colorMap[i] = heightLevels.DeepWaterColor;                    
                    break;
                case HeightType.ShallowWater:
                    colorMap[i] = heightLevels.ShallowWaterColor;
                    break;
                case HeightType.Shore:
                    colorMap[i] = heightLevels.ShoreColor;
                    break;
                case HeightType.Sand:
                    colorMap[i] = heightLevels.SandColor;
                    break;
                case HeightType.Grass:
                    colorMap[i] = heightLevels.GrassColor;
                    break;
                case HeightType.Forest:
                    colorMap[i] = heightLevels.ForestColor;
                    break;
                case HeightType.Rock:
                    colorMap[i] = heightLevels.RockColor;
                    break;
                case HeightType.Snow:
                    colorMap[i] = heightLevels.SnowColor;
                    break;
                default:
                    colorMap[i] = Color.black;
                    break;
            }
        }

        return colorMap;
    }

    private Color[] BuildMoistureMap(TileData[] tileData)
    {
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];

        for (int i = 0; i < tileData.Length; i++)
        {

            switch (tileData[i].moistureType)
            {
                case MoistureType.Dryest:
                    colorMap[i] = moistureLevels.DryestColor;
                    break;
                case MoistureType.Dryer:
                    colorMap[i] = moistureLevels.DryerColor;
                    break;
                case MoistureType.Dry:
                    colorMap[i] = moistureLevels.DryColor;
                    break;
                case MoistureType.Wet:
                    colorMap[i] = moistureLevels.WetColor;
                    break;
                case MoistureType.Wetter:
                    colorMap[i] = moistureLevels.WetterColor;
                    break;
                case MoistureType.Wettest:
                    colorMap[i] = moistureLevels.WettestColor;
                    break;
                default:
                    break;
            }
        }

        return colorMap;
    }

    private Color[] BuildHeatMapFromNoise(float[,] heatNoise)
    {
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        float[] convertedNoise = heatNoise.To1DArray(mapChunkSize);

        for (int i = 0; i < convertedNoise.Length; i++)
        {
            int x = i / mapChunkSize;
            int y = i % mapChunkSize;

            int index;

            if (heatMapDirection == HeatMapDirection.Latitudinal)
            {
                index = i;
            }

            else
            {
                index = x * mapChunkSize + y;
            }

            if (convertedNoise[index] < heatLevels.ColdestValue)
            {
                colorMap[index] = heatLevels.ColdestColor;
            }

            else if (convertedNoise[index] < heatLevels.ColderValue)
            {
                colorMap[index] = heatLevels.ColderColor;
            }

            else if (convertedNoise[index] < heatLevels.ColdValue)
            {
                colorMap[index] = heatLevels.ColdColor;
            }

            else if (convertedNoise[index] < heatLevels.WarmValue)
            {
                colorMap[index] = heatLevels.WarmColor;
            }

            else if (convertedNoise[index] < heatLevels.WarmerValue)
            {
                colorMap[index] = heatLevels.WarmerColor;
            }

            else
            {
                colorMap[index] = heatLevels.WarmestColor;
            }
        }

        return colorMap;
    }

    private Color[] BuildHeatMap(TileData[] tileData)
    {
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];       

        for (int i = 0; i < tileData.Length; i++)
        {
            int x = i / mapChunkSize;
            int y = i % mapChunkSize;

            int index;

            if (heatMapDirection == HeatMapDirection.Latitudinal)
            {
                index = i;
            }

            else
            {
                index = x * mapChunkSize + y;
            }

            switch (tileData[index].heatType)
            {
                case HeatType.Coldest:
                    colorMap[index] = heatLevels.ColdestColor;
                    break;
                case HeatType.Colder:
                    colorMap[index] = heatLevels.ColderColor;
                    break;
                case HeatType.Cold:
                    colorMap[index] = heatLevels.ColdColor;
                    break;
                case HeatType.Warm:
                    colorMap[index] = heatLevels.WarmColor;
                    break;
                case HeatType.Warmer:
                    colorMap[index] = heatLevels.WarmerColor;
                    break;
                case HeatType.Warmest:
                    colorMap[index] = heatLevels.WarmestColor;
                    break;
                default:
                    break;
            }
        }

        return colorMap;
    }

    private Color[] BuildBiomeMap(TileData[] tileData)
    {                
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        
        for(int i = 0; i < tileData.Length; i++)
        {
            if (tileData[i].terainGroup != TerrainGroupType.Water)
            {

                switch (tileData[i].biomeType)
                {
                    case BiomeType.Ice:
                        colorMap[i] = biomeColors.Ice;
                        break;
                    case BiomeType.BorealForest:
                        colorMap[i] = biomeColors.BorealForest;
                        break;
                    case BiomeType.Desert:
                        colorMap[i] = biomeColors.Desert;
                        break;
                    case BiomeType.Grassland:
                        colorMap[i] = biomeColors.Grassland;
                        break;
                    case BiomeType.SeasonalForest:
                        colorMap[i] = biomeColors.SeasonalForest;
                        break;
                    case BiomeType.Tundra:
                        colorMap[i] = biomeColors.Tundra;
                        break;
                    case BiomeType.Savanna:
                        colorMap[i] = biomeColors.Savanna;
                        break;
                    case BiomeType.TemperateRainforest:
                        colorMap[i] = biomeColors.TemperateRainforest;
                        break;
                    case BiomeType.TropicalRainforest:
                        colorMap[i] = biomeColors.TropicalRainforest;
                        break;
                    case BiomeType.Woodland:
                        colorMap[i] = biomeColors.Woodland;
                        break;
                    default:
                        break;
                }
            }

            else
            {                
                if (tileData[i].heightType == HeightType.DeepWater)
                {
                    colorMap[i] = heightLevels.DeepWaterColor;
                    continue;
                }

                else if (tileData[i].heightType == HeightType.ShallowWater)
                {
                    colorMap[i] = heightLevels.ShallowWaterColor;
                    continue;
                }

                else
                {
                    colorMap[i] = Color.black;
                    continue;
                }
            }

        }

        return colorMap;
    }

    private TileData[] GetTilesByBiomeType(TileData[] data, BiomeType biome)
    {
        return data.Where(td => td.biomeType == biome).ToArray();
    }

    public void UpdateNoiseParameters()
    {
        if (noiseParameters.Length > 0)
        {
            if (noiseParameters.Any(p => p.isMaster))
            {
                bool foundMaster = false;

                for (int p = 0; p < noiseParameters.Length; p++)
                {
                    if (foundMaster)
                    {
                        noiseParameters[p].isMaster = false;

                        if (noiseParameters[p].useMaster)
                        {
                            NoiseParameters mp = noiseParameters[masterNoiseIndex];
                            NoiseParameters op = noiseParameters[p];

                            mp.index = op.index;
                            mp.name = op.name;
                            mp.outputType = op.outputType;
                            mp.type = op.type;
                            mp.useMaster = true;
                            mp.isMaster = false;

                            noiseParameters[p] = mp;
                        }                      
                    }

                    if (noiseParameters[p].isMaster)
                    {
                        foundMaster = true;
                        masterNoiseIndex = p;
                        continue;
                    }
                }
            }
        }
    }

    private void OnValidate()
    {
        fallOffNoise = FalloffGenerator.GenerateFalloffMap(mapChunkSize, fallOffStrength, fallOffFade);
        UpdateNoiseParameters();
    }

}
