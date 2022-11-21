using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

public static class MapNoise
{
    public static Vector3[,] GenerateNoiseMap(int mapSize, Vector2 Offset,NoiseData HeightNoiseData, NoiseData HorizontalNoisedata)
    {
        AnimationCurve Heightcurve = new (HeightNoiseData.heightCurve.keys);
        AnimationCurve FlatDispcurve = new (HorizontalNoisedata.heightCurve.keys);

        Vector3[,] noiseMap = new Vector3[mapSize, mapSize];
        float halfsize = mapSize / 2f;

        int horSeed = HorizontalNoisedata.seed;
        float horFreq = HorizontalNoisedata.frequency;
        float horPers = HorizontalNoisedata.persistance;
        float horX = HorizontalNoisedata.frequencyScale;
        int horOct = HorizontalNoisedata.octaves;
        float horAmp = HorizontalNoisedata.amplitude;

        int highSeed = HeightNoiseData.seed;
        float highFreq = HeightNoiseData.frequency;
        float highPers = HeightNoiseData.persistance;
        float highX = HeightNoiseData.frequencyScale;
        int highOct = HeightNoiseData.octaves;
        float highAmp = HeightNoiseData.amplitude;

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {

                float sampleX = x + Offset.x - halfsize;
                float sampleY = y - Offset.y - halfsize;


                Profiler.BeginSample("Calculating Noise");
                noiseMap[x, y].y = OctavedNoise(highSeed, highFreq, highX, highPers, highOct, new Vector2(sampleX, sampleY));

                noiseMap[x, y].x = FlatDispcurve.Evaluate(noiseMap[x,y].y) * (OctavedNoise(horSeed + 50, horFreq, horX, horPers, horOct, new Vector2(sampleX, sampleY))-0.5f)
                    * horAmp * horFreq;

                noiseMap[x, y].z = FlatDispcurve.Evaluate(noiseMap[x, y].y) * (OctavedNoise(horSeed, horFreq, horX, horPers, horOct, new Vector2(sampleX, sampleY))-0.5f)
                    * horAmp * horFreq;

                noiseMap[x,y].y = Heightcurve.Evaluate(noiseMap[x,y].y) * highAmp * highFreq;
                Profiler.EndSample();
            }
        }

        return noiseMap;
    }
    [System.Serializable]
    public class NoiseData
    {

        public int seed;
        public float frequency;
        public float frequencyScale = 1;
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
    public static float OctavedNoise(int seed, float inFrequency, float Xscale , float persistance, int octaves, Vector2 Coord)
    {
        Profiler.BeginSample("VariableDeclaration");
        float amplitude = 1;

        float maxPossibleHeight = 0;

        Vector2[] octaveOffsets = new Vector2[octaves];

        Profiler.EndSample();
        Profiler.BeginSample("SettingOffsets");
        int offsetY = seed * 20;
        int offsetX = seed;
        for (int i = 0; i < octaves; i++)
        {
            offsetX = (seed << i + 1 & 8562861 | seed ^ offsetY) ^ (seed << 24);
            offsetY = (seed << i + 1 | 18728387 & seed ^ offsetX) ^ (seed << 14);


            octaveOffsets[i] = new Vector2(offsetX >> 10, offsetY >> 10);
            

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }
        Profiler.EndSample();

        Profiler.BeginSample("PerlinCalculationLoop");
        amplitude = 1;
        float frequency = 1;
        float noiseheight = 0;

        
        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (Coord.x + octaveOffsets[i].x) / inFrequency * frequency * Xscale;
            float sampleY = (Coord.y + octaveOffsets[i].y) / inFrequency * frequency;



            noiseheight += Mathf.PerlinNoise(sampleX, sampleY) * amplitude;

            amplitude *= persistance;
            frequency *= 2;
        }
        
        float height = noiseheight/maxPossibleHeight;
        Profiler.EndSample();
        return height;

    }
}

