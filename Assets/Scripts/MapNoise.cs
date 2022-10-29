using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MapNoise
{
    public enum NormalizeMode { local, Global};

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 Offset, NormalizeMode normalizeMode)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random PRNG = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;

        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = PRNG.Next(-100000, 100000) + Offset.x;
            float offsetY = PRNG.Next(-100000, 100000) - Offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
            maxPossibleHeight += amplitude * persistance;
        }

        if (scale <= 0.0)
        {
            scale = 0.0001f;
        }
        float maxlocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfwidth = mapWidth / 2f;
        float halfheight = mapHeight / 2f;

        for(int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseheight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x-halfwidth + octaveOffsets[i].x) / scale * frequency ;
                    float sampleY = (y- halfheight + octaveOffsets[i].y) / scale * frequency;

                    float perlinvalue = Mathf.PerlinNoise(sampleX, sampleY)*2 -1;
                    noiseheight += perlinvalue * amplitude;
                   

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }
                if (noiseheight > maxlocalNoiseHeight)
                {
                    maxlocalNoiseHeight = noiseheight;
                }
                else if (noiseheight< minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseheight;
                }

                noiseMap[x, y] = noiseheight;

            }
        }
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (normalizeMode == NormalizeMode.local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxlocalNoiseHeight, noiseMap[x, y]);
                }
                else
                {
                    float normalizedheight = (noiseMap[x,y]+1)/(2f * maxPossibleHeight/2.1f);
                    noiseMap[x,y] = Mathf.Clamp(normalizedheight,0,int.MaxValue);
                }
            }
        }

                return noiseMap;
    }
}
