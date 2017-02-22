using UnityEngine;

namespace CityGen.ParaMaps
{
    public abstract class ParameterMap
    {
        protected Texture2D map;
        protected int width;
        protected int height;

        public ParameterMap(int width, int height, Texture2D map)
        {
            this.width = width;
            this.height = height;
            this.map = map;
        }

        public int Width
        {
            get { return width; }
        }
        public int Height
        {
            get { return height; }
        }

        protected Vector2 transformCoordinate(float x, float y)
        {
            // texture will rotate 180 degree first 
            x = width * .5f - x;
            y = height * .5f - y;
            return new Vector2(Mathf.Round(x), Mathf.Round(y));
        }

        public abstract float getValue(float x, float y);

        public abstract Texture2D Map { get; }
    }
}
