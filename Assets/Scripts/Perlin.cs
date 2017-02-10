using UnityEngine;
using System.Collections;

namespace CityGen
{
    public class Perlin
    {
        public Texture2D texture;

        internal float xOffset;
        internal float yOffset;
        internal float freq = 1f;

        private Color[] pixels;

        public Perlin(int width, int height)
        {
            texture = new Texture2D(width, height);
            pixels = new Color[width * height];
        }

        public Perlin(int width, int height, float xOffset, float yOffset, float freq = 1f)
        {
            texture = new Texture2D(width, height);
            pixels = new Color[width * height];

            makeNoises(xOffset, yOffset, freq);
        }

        public void makeNoises(float xOffset, float yOffset, float freq = 1f)
        {
            Debug.Log(xOffset);
            Debug.Log(yOffset);
            this.xOffset = xOffset;
            this.yOffset = yOffset;
            this.freq = freq;

            for (float y = 0f; y < texture.height; ++y)
            {
                for (float x = 0f; x < texture.width; ++x)
                {
                    float xCoord = xOffset + x / texture.width * freq;
                    float yCoord = yOffset + y / texture.height * freq;
                    float sample = Mathf.PerlinNoise(xCoord, yCoord);
                    pixels[(int)(y * texture.width + x)] = new Color(sample, sample, sample);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
        }

        public float valueAt(int x, int y)
        {
            return pixels[y * texture.width + x].r;
        }
    }
}

