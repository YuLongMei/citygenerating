using UnityEngine;

namespace CityGen.ParaMaps
{
    public class ExternalParaMap : ParameterMap
    {
        public ExternalParaMap(int width, int height, Texture2D map) : base(width, height, map) { }

        public override float getValue(float x, float y)
        {
            Vector2 coord = transformCoordinate(x, y);
            return map.GetPixel((int)coord.x, (int)coord.y).r;
        }

        public override Texture2D Map
        {
            get
            {
                return map;
            }
        }
    }
}
