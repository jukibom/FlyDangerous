using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MapNoise
{
    public static Vector3[,] GenerateNoiseMap(int mapSize, Vector2 Offset,NoiseData HeightNoiseData, NoiseData HorizontalNoisedata)
    {
        AnimationCurve Heightcurve = new (HeightNoiseData.heightCurve.keys);
        AnimationCurve FlatDispcurve = new (HorizontalNoisedata.heightCurve.keys);


        int seed = HeightNoiseData.seed;
        float scale = HeightNoiseData.frequency;
        int octaves = HeightNoiseData.octaves;
        float persistance = HeightNoiseData.persistance;


        Vector3[,] noiseMap = new Vector3[mapSize, mapSize];

        System.Random VertPRNG = new System.Random(seed);
        System.Random HoriPRNG = new System.Random(HorizontalNoisedata.seed);
        Vector2[] VertoctaveOffsets = new Vector2[octaves];
        Vector2[] HorioctaveOffsets = new Vector2[HorizontalNoisedata.octaves];

        float maxPossibleHeight = 0;
        float maxPossibleWidth = 0;

        float amplitude = 1;
        float frequency;

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = VertPRNG.Next(-100000, 100000) + Offset.x;
            float offsetY = VertPRNG.Next(-100000, 100000) - Offset.y;
            VertoctaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }
        amplitude = 1;
        for (int i = 0; i < HorizontalNoisedata.octaves; i++)
        {
            float offsetX = HoriPRNG.Next(-100000, 100000) + Offset.x;
            float offsetY = HoriPRNG.Next(-100000, 100000) - Offset.y;
            HorioctaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleWidth += amplitude;
            amplitude *= HorizontalNoisedata.persistance;
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
                    float sampleX = (x - halfsize + VertoctaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfsize + VertoctaveOffsets[i].y) / scale * frequency;

                    float perlinvalue = Mathf.PerlinNoise(sampleX, sampleY)*2 -1;
                    noiseheight += perlinvalue * amplitude;
                    amplitude *= persistance;
                    frequency *= 2;
                }
                

                noiseMap[x, y].y = noiseheight;

                amplitude = 1;
                frequency = 1;

                for (int i = 0; i < HorizontalNoisedata.octaves; i++)
                {
                    float sampleX = (x - halfsize + HorioctaveOffsets[i].x) / HorizontalNoisedata.frequency * frequency;
                    float sampleY = (y - halfsize + HorioctaveOffsets[i].y) / HorizontalNoisedata.frequency * frequency;

                    noiseMap[x, y].x += (Mathf.PerlinNoise(sampleX-112, sampleY +523)*2 -1) *  amplitude;
                    noiseMap[x, y].z += (Mathf.PerlinNoise(sampleX, sampleY) * 2 -1) *  amplitude;
                    amplitude *= HorizontalNoisedata.persistance;
                    frequency *= 2;
                }


            }
        }
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                noiseMap[x, y].y = (noiseMap[x, y].y + maxPossibleHeight) / (maxPossibleHeight * 2);

                noiseMap[x, y].x = (noiseMap[x, y].x + maxPossibleWidth) / (maxPossibleWidth * 2);
                noiseMap[x, y].z = (noiseMap[x, y].z + maxPossibleWidth) / (maxPossibleWidth * 2);
                noiseMap[x, y].x = FlatDispcurve.Evaluate(noiseMap[x, y].y) * (noiseMap[x, y].x-0.5f) * HorizontalNoisedata.amplitude * HorizontalNoisedata.frequency;
                noiseMap[x, y].z = FlatDispcurve.Evaluate(noiseMap[x, y].y) * (noiseMap[x,y].z-0.5f) * HorizontalNoisedata.amplitude * HorizontalNoisedata.frequency;

                noiseMap[x, y].y = Heightcurve.Evaluate(noiseMap[x, y].y) * HeightNoiseData.amplitude * scale;
            }
        }

        return noiseMap;
    }
    [System.Serializable]
    public class NoiseData
    {

        public int seed;
        public float frequency;
        public float amplitude;
        [Range(0f, 1f)]
        public float persistance;
        public int octaves;
        public AnimationCurve heightCurve;
        public NoiseData(int seed,float frequency,float amplitude,float persistance,int octaves,AnimationCurve heightCurve)
        {
            this.seed = seed;
            this.frequency = frequency;
            this.amplitude = amplitude;
            this.persistance = persistance;
            this.octaves = octaves;
            this.heightCurve = heightCurve;
        }
    }


}
