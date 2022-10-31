using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D TexturefromeColormap(Color[] colormap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colormap);
        texture.Apply();
        return texture;
    }

    public static Texture2D texturefromheightmap(float[,] heights)
    {
        int width = heights.GetLength(0);
        int height = heights.GetLength(1);


        Color[] colormap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colormap[y * width + x] = Color.Lerp(Color.black, Color.white, heights[x, y]);
            }
        }
        return TexturefromeColormap(colormap, width, height);
    }
}
