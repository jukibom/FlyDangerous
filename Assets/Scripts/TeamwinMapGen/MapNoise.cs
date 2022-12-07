using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public static class MapNoise
{
    public static Vector3[,] GenerateNoiseMap(int mapSize, Vector2 Offset, NoiseData[] NoiseArray)
    {

        Vector3[,] noiseMap = new Vector3[mapSize, mapSize];
        float halfsize = mapSize / 2f;

        AnimationCurve[] curvearray = new AnimationCurve[NoiseArray.Length];

        for (int i = 0; i < NoiseArray.Length; i++)
        {
            curvearray[i] = NoiseArray[i].getAnimationCurve();
        }


        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                
                
                float sampleX = x + Offset.x - halfsize;
                float sampleY = y - Offset.y - halfsize;

                for(int i = 0; i < NoiseArray.Length;i++)
                {
                    if (NoiseArray[i].isActive)
                    {
                        Profiler.BeginSample("Defining Variables");
                        var freq = NoiseArray[i].frequency;
                        var seed = NoiseArray[i].seed;
                        var freqscale = NoiseArray[i].frequencyScale;
                        var amp = NoiseArray[i].amplitude;
                        var pers = NoiseArray[i].persistance;
                        var oct = NoiseArray[i].octaves;
                        AnimationCurve curve = curvearray[i];
                        Profiler.EndSample();


                        if (NoiseArray[i].isHorizontal)
                        {

                            float noiseHeightY;
                            float noiseHeightX;

                            if (NoiseArray[i].noiseType == NoiseData.NoiseType.Constant)
                            {
                                noiseHeightX = noiseMap[x, y].x;
                                noiseHeightY = noiseMap[x, y].z;
                            }
                            else if (NoiseArray[i].noiseType == NoiseData.NoiseType.Voronoi_Edge || NoiseArray[i].noiseType == NoiseData.NoiseType.Voronoi_Center)
                            {
                                noiseHeightX = VorField(new Vector2(sampleX - 500, sampleY + 300), freq, seed, pers, NoiseArray[i].noiseType == NoiseData.NoiseType.Voronoi_Edge);
                                noiseHeightY = VorField(new Vector2(sampleX, sampleY), freq, seed, pers, true);
                            }
                            else
                            {
                                noiseHeightX = OctavedNoise(seed, freq, freqscale, pers, oct, new Vector2(sampleX, sampleY));
                                noiseHeightY = OctavedNoise(seed + 50, freq, freqscale, pers, oct, new Vector2(sampleX, sampleY));

                                noiseHeightX -= 0.5f;
                                noiseHeightY -= 0.5f;
                            }
                            Profiler.BeginSample("Curve Evaluation");
                            if (NoiseArray[i].UseVertical)
                            {
                                noiseHeightX *= curve.Evaluate(noiseMap[x, y].y);
                                noiseHeightY *= curve.Evaluate(noiseMap[x, y].y);
                            }
                            else
                            {
                                noiseHeightX = curve.Evaluate(noiseHeightX);
                                noiseHeightY = curve.Evaluate(noiseHeightY);
                            }
                            Profiler.EndSample();

                            noiseHeightX *= amp;
                            noiseHeightY *= amp;

                            if (NoiseArray[i].maskMode == NoiseData.MaskMode.Multiply)
                            {
                                noiseMap[x,y].x *= noiseHeightX;
                                noiseMap[x,y].z *= noiseHeightY;
                            }
                            else if(NoiseArray[i].maskMode == NoiseData.MaskMode.Add)
                            {
                                noiseMap[x, y].x += noiseHeightX;
                                noiseMap[x, y].z += noiseHeightY;
                            }

                        }
                        else
                        {
                            float noiseHeight;

                            if (NoiseArray[i].noiseType == NoiseData.NoiseType.Constant)
                            {
                                noiseHeight = noiseMap[x, y].y;
                            }
                            else if (NoiseArray[i].noiseType == NoiseData.NoiseType.Voronoi_Edge || NoiseArray[i].noiseType == NoiseData.NoiseType.Voronoi_Center)
                            {
                                noiseHeight = VorField(new Vector2(sampleX, sampleY), freq, seed, pers, NoiseArray[i].noiseType == NoiseData.NoiseType.Voronoi_Edge);
                            }
                            else
                            {
                                noiseHeight =  OctavedNoise(seed, freq, freqscale, pers, oct, new Vector2(sampleX, sampleY));
                            }
                            Profiler.BeginSample("Curve Evaluation");
                            if (NoiseArray[i].UseVertical)
                            {
                                noiseHeight -= 0.5f;
                                noiseHeight *= curve.Evaluate(noiseMap[x, y].y);
                            }
                            else
                            {
                                noiseHeight = curve.Evaluate(noiseHeight);
                            }
                            Profiler.EndSample();

                            noiseHeight *= amp;



                            if (NoiseArray[i].maskMode == NoiseData.MaskMode.Multiply)
                            {
                                noiseMap[x, y].y *= noiseHeight;

                            }
                            else if (NoiseArray[i].maskMode == NoiseData.MaskMode.Add)
                            {
                                noiseMap[x, y].y += noiseHeight;

                            }
                        }
                    }
                }
            }
        }

        return noiseMap;
    }
    [System.Serializable]
    public class NoiseData
    {
        public bool isActive;
        public int seed;
        public float frequency;
        public float frequencyScale = 1;
        public float amplitude;
        public bool isHorizontal;
        [Range(0f, 1f)]
        public float persistance;
        public int octaves;
        public AnimationCurve heightCurve;
        public bool UseVertical;
        public enum NoiseType {Perlin, Constant, Voronoi_Edge, Voronoi_Center}
        public NoiseType noiseType;
        public enum MaskMode {Add, Multiply};
        public MaskMode maskMode;
        
        public NoiseData(int seed,float frequency,float amplitude,float persistance,int octaves,AnimationCurve heightCurve)
        {
            this.seed = seed;
            this.frequency = frequency;
            this.amplitude = amplitude;
            this.persistance = persistance;
            this.octaves = octaves;
            this.heightCurve = heightCurve;
        }
        public AnimationCurve getAnimationCurve()
        {
            return new (heightCurve.keys);
        }
    }
    public static float VorField(Vector2 Coord, float frequency, int seed, float jitterstrength, bool IsEdge)
    {
        jitterstrength *= 1f;
        Profiler.BeginSample("Calculating Voronoi");
        Profiler.BeginSample("Initializing points");
        Coord = Coord / frequency;
        Vector2 quantizedCoord = new Vector2( Mathf.Round(Coord.x),Mathf.Round(Coord.y));
        quantizedCoord -= new Vector2(1, 1);
        Vector2[] points = new Vector2[9];
        
        for (int i = 0; i < 3; i++)
        {
            points[i * 3] = quantizedCoord + new Vector2(0,i);
            points[i * 3 + 1] = quantizedCoord + new Vector2(1, i);
            points[i * 3 + 2] = quantizedCoord + new Vector2(2, i);
        }
        Profiler.EndSample();
        Endlessterrain.PRNG rng;
        float[] dists = new float[9];
        for (int i = 0; i < 9; i++)
        {
            Profiler.BeginSample("Setting PRNG");
            rng = new Endlessterrain.PRNG(Mathf.RoundToInt(points[i].magnitude + points[i].x*1.5231f + points[i].y * 12.51f)+seed);
            Profiler.EndSample();
            Profiler.BeginSample("jittering points");
            points[i] += new Vector2((rng.NextFloat() - 0.5f)*jitterstrength, (rng.NextFloat() - 0.5f)*jitterstrength);
            Profiler.EndSample();
            Profiler.BeginSample("Getting distances");
            dists[i] = Vector2.Distance(points[i],Coord);
            Profiler.EndSample();
        }
        Profiler.BeginSample("Sorting points");
        Array.Sort(dists);
        Profiler.EndSample();
        Profiler.EndSample();
        if(IsEdge)
        {
            return dists[1] - dists[0];
        }
        else
        {
            return dists[0];
        }
        
    }
    public static float OctavedNoise(int seed, float inFrequency, float Xscale , float persistance, int octaves, Vector2 Coord)
    {
        Profiler.BeginSample("Calculating Octaved Noise");
        float amplitude = 1;

        float maxPossibleHeight = 0;

        Vector2[] octaveOffsets = new Vector2[octaves];

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

