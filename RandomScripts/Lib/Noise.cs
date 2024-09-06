using System.Collections;
using UnityEngine;
using Unity.Mathematics;
using MNoise = Unity.Mathematics.noise;
using FNoise = FastNoise;

public static class Noise
{
    public enum NoiseType : int
    {
        Perlin,
        Simplex,
        Cellular
    }

    public enum NormalizeMode : int
    {
        Local,
        Global
    }

    public static float[,] GenerateUniformNoiseMap(int chunkSize, float centerPoint, float maxDistance, float offset)
    {
        // create an empty noise map with the mapDepth and mapWidth coordinates
        float[,] noiseMap = new float[chunkSize, chunkSize];

        for (int zIndex = 0; zIndex < chunkSize; zIndex++)
        {
            // calculate the sampleZ by summing the index and the offset
            float sampleZ = zIndex + offset;
            // calculate the noise proportional to the distance of the sample to the center of the level
            float noise = Mathf.Abs(sampleZ - centerPoint) / maxDistance;
            // apply the noise for all points with this Z coordinate
            for (int xIndex = 0; xIndex < chunkSize; xIndex++)
            {
                noiseMap[chunkSize - zIndex - 1, xIndex] = noise;
            }
        }

        return noiseMap;

    }

    public static float[,] GenerateNoiseMap(int width, int height, int seed, float2 offset, NoiseParameters parameters, NoiseGenerator generator = NoiseGenerator.Unity, NormalizeMode normalizeMode = NormalizeMode.Local, float normalizationFactor = 2.25f, bool normalizeAmplitudes = false, TerrainFeature[] features = null, TerrainFeatureParameters? featureParameters = null)
    {        
        float[,] noiseMap = new float[width, height];
        FNoise.FastNoiseLite fNoiseParent = new FNoise.FastNoiseLite();

        System.Random prng = new System.Random(seed);
        float2[] octaveOffsets = new float2[parameters.octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1f;
        float frequency = 1f;

        for (int i = 0; i < parameters.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;

            octaveOffsets[i] = new float2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= parameters.persistance;
        }

        if (parameters.scale <= 0)
        {
            parameters.scale = 0.0001f;
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

                if (generator == NoiseGenerator.Unity)
                {
                    amplitude = 1f;
                    frequency = 1f;

                    for (int i = 0; i < parameters.octaves; i++)
                    {
                        float sampleX = (x - halfWidth + octaveOffsets[i].x) / parameters.scale * frequency;
                        float sampleY = (y - halfHeight + octaveOffsets[i].y) / parameters.scale * frequency;
                        float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);

                        if (features != null && features.Length > 0)
                        {
                            if (featureParameters.HasValue) {

                                TerrainFeatureParameters fp = featureParameters.Value;

                                for (int f = 0; f < features.Length; f++)
                                {
                                    if (features[f].enabled && (noiseValue >= features[f].minimumHeightThreshold && noiseValue <= features[f].maximumHeightThreshold))
                                    {
                                        switch (features[f].type)
                                        {
                                            case FeatureType.Terraces:
                                                noiseValue = AddTerraceMin(noiseValue, fp.terraceWidth);
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                }

                            }
                        }

                        // TODO: Start back here - you were trying to think of how to implement terrain features based on height thresholds - various features use various math functions to sculpt and shape it

                        noiseValue = noiseValue * 2 - 1;
                        noiseHeight += noiseValue * amplitude;

                        amplitude *= parameters.persistance;
                        frequency *= parameters.lacunarity;
                    }
                }

                else if (generator == NoiseGenerator.FastNoise)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[0].x) / parameters.scale * parameters.frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[0].y) / parameters.scale * parameters.frequency;

                    fNoiseParent.SetCellularReturnType(parameters.CellularReturnType);
                    fNoiseParent.SetFractalGain(parameters.persistance);
                    fNoiseParent.SetFractalLacunarity(parameters.lacunarity);
                    fNoiseParent.SetFractalOctaves(parameters.octaves);
                    fNoiseParent.SetCellularDistanceFunction(parameters.cellularDistanceFunction);
                    fNoiseParent.SetCellularJitter(parameters.jitter);
                    fNoiseParent.SetDomainWarpAmp(parameters.domainWarpStrength);
                    fNoiseParent.SetDomainWarpType(parameters.domainWarpType);
                    fNoiseParent.SetRotationType3D(parameters.rotationType3D);
                    fNoiseParent.SetFractalType(parameters.fractalType);
                    fNoiseParent.SetFractalPingPongStrength(parameters.pingPongStrength);
                    fNoiseParent.SetFractalWeightedStrength(parameters.fractalWeightStrength);
                    fNoiseParent.SetFrequency(parameters.frequency);
                    fNoiseParent.SetNoiseType(parameters.type);

                    fNoiseParent.SetSeed(seed);

                    float noiseValue = fNoiseParent.GetNoise(sampleX, sampleY, normalizeAmplitudes);
                    maxPossibleHeight = fNoiseParent.GetMaxHeightFromLastSample();

                    if (features != null && features.Length > 0)
                    {
                        if (featureParameters.HasValue)
                        {
                            TerrainFeatureParameters fp = featureParameters.Value;

                            for (int f = 0; f < features.Length; f++)
                            {
                                if (features[f].enabled && (noiseValue >= features[f].minimumHeightThreshold && noiseValue <= features[f].maximumHeightThreshold))
                                {
                                    switch (features[f].type)
                                    {
                                        case FeatureType.Terraces:
                                            noiseValue = AddTerraceRound(noiseValue, fp.terraceWidth);
                                            break;
                                        case FeatureType.RidgedNoise:
                                            noiseValue = AddPeaks(noiseValue, fp.peakStrength);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }

                        }
                    }

                    noiseValue = noiseValue * 2 - 1;

                    noiseHeight += noiseValue;
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

        // Normalization so we fall within -1 and 1
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }

                else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / normalizationFactor);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }
        
        return noiseMap;
    }

    private static float AddPeaks(float v, float strength)
    {
        return Mathf.Pow(v, strength);
    }

    private static float AddTerraceSin(float h, float e)
    {
        return Mathf.Pow((float)(Mathf.Round(h) + 0.5 * (2 * (h - Mathf.Round(h)))), e);
    }

    private static float AddTerraceMin(float h, float w)
    {        
        var k = Mathf.Floor(h / w);
        var f = (h - k * w) / w;
        var s = Mathf.Min(2 * f, 1.0f);

        return (k + s) * w;
    }

    private static float AddTerraceRound(float h, float w)
    {
        return Mathf.Round(h * w) / w;
    }

    //if(features.Length > 0)
    //            {
    //                for(int i = 0; i<features.Length; i++)
    //                {
    //                    if (!features[i].enabled)
    //                        continue;

    //                    if (noiseMap[x, y] < features[i].minimumHeightThreshold || noiseMap[x, y] > features[i].maximumHeightThreshold)
    //                        continue;                        

    //                    switch (features[i].type)
    //                    {
    //                        case FeatureType.Terraces:
    //                            noiseMap[x, y] = AddTerrace(noiseMap[x, y], terraceSteps);
    //                            break;
    //                        case FeatureType.RidgedNoise:
    //                            noiseMap[x, y] = AddPeaks(noiseMap[x, y], peakStrength);
    //                            break;
    //                        default:
    //                            break;
    //                    }
    //                }
    //            }
}
