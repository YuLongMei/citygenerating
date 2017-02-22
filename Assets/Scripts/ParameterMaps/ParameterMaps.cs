using UnityEngine;
using System.Collections.Generic;

namespace CityGen.ParaMaps
{
    public enum ParameterMapIndex
    {
        PopulationDensity,
    }

    public class ParameterMaps
    {
        private List<Texture2D> maps = new List<Texture2D>();
        private int width;
        private int height;

        public ParameterMaps(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public int Width
        {
            get { return width; }
        }
        public int Height
        {
            get { return height; }
        }

        public Texture2D getMap(ParameterMapIndex index)
        {
            while ((int)index >= maps.Count)
            {
                Texture2D t = new Texture2D(width, height);
                maps.Add(t);
            }
            return maps[(int)index];
        }

        public void setMap(ParameterMapIndex index, Texture2D input)
        {
            while ((int)index >= maps.Count)
            {
                Texture2D t = new Texture2D(width, height);
                maps.Add(t);
            }
            maps[(int)index].SetPixels(input.GetPixels());
            maps[(int)index].Apply();
        }

        /*public Color getValueInMap(ParameterMapIndex index, float x, float y)
        {
            var map = getMap(index);
        }*/
    }
}
