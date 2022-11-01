using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MapNoise
{


    public static Vector3[,] GenerateNoiseMap(int mapSize, int seed, float scale, int octaves, float persistance, Vector2 Offset)
    {
        Vector3[,] noiseMap = new Vector3[mapSize, mapSize];

        System.Random PRNG = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;

        float amplitude = 1;
        float frequency;

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = PRNG.Next(-100000, 100000) + Offset.x;
            float offsetY = PRNG.Next(-100000, 100000) - Offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if (scale <= 0.0)
        {
            scale = 0.0001f;
        }

        float halfsize = mapSize / 2f;

        for(int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseheight = 0;

                noiseMap[x, y].x = 0;
                noiseMap[x, y].z = 0;


                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfsize + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfsize + octaveOffsets[i].y) / scale * frequency;

                    float perlinvalue = Mathf.PerlinNoise(sampleX, sampleY)*2 -1;
                    noiseheight += perlinvalue * amplitude;
                    noiseMap[x,y].x += (Mathf.PerlinNoise(sampleX+5000, sampleY+12223) * 2 - 1) * amplitude;
                    noiseMap[x, y].z += (Mathf.PerlinNoise(sampleX + 500, sampleY + 4123) * 2 - 1) * amplitude;
                    amplitude *= persistance;
                    frequency *= 2;
                }
                

                noiseMap[x, y].y = noiseheight;




            }
        }
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                noiseMap[x,y].y = (noiseMap[x, y].y + maxPossibleHeight) / (maxPossibleHeight * 2);

            }
        }

                return noiseMap;
    }
}
