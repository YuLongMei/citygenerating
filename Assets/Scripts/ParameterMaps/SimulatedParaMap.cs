using UnityEngine;

namespace CityGen.ParaMaps
{
    public class SimulatedParaMap : ParameterMap
    {
        private float xOffset;
        private float yOffset;

        public SimulatedParaMap(int width, int height, Texture2D map, float xOffset, float yOffset)
            : base(width, height, map)
        {
            this.xOffset = xOffset;
            this.yOffset = yOffset;
        }

        public override Texture2D Map
        {
            get
            {
                if (map == null)
                {
                    map = new Texture2D(width, height);
                    var pixels = new Color[width * height];

                    for (float y = 0f; y < height; ++y)
                    {
                        for (float x = 0f; x < width; ++x)
                        {
                            float xCoord = xOffset + x / Config.PARAMAP_GRANULARITY;
                            float yCoord = yOffset + y / Config.PARAMAP_GRANULARITY;
                            //float sample = (float)SimplexNoise.noise(xCoord, yCoord);
                            float sample = (Mathf.PerlinNoise(xCoord, yCoord) + 1f) * .5f;
                            pixels[(int)(y * width + x)] = new Color(sample, sample, sample);
                        }
                    }
                    map.SetPixels(pixels);
                    map.Apply();
                }

                return map;
            }
        }

        public override float getValue(float x, float y)
        {
            Vector2 coord = transformCoordinate(x, y);
            float xCoord = xOffset + coord.x / Config.PARAMAP_GRANULARITY;
            float yCoord = yOffset + coord.y / Config.PARAMAP_GRANULARITY;
            return (Mathf.PerlinNoise(xCoord, yCoord) + 1f) * .5f;
        }
    }
}