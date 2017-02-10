using UnityEngine;

namespace CityGen.Util
{
    /// <summary>
    /// A 2D  Partitioning Tree
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public class Quadtree<TItem>
        : GeometricTree<TItem, Vector2, Rect>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="threshold"></param>
        /// <param name="maxDepth"></param>
        public Quadtree(Rect bounds, int threshold, int maxDepth)
            : base(bounds, threshold, maxDepth)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="container"></param>
        /// <param name="contained"></param>
        /// <returns></returns>
        protected override bool Contains(Rect container, ref Rect contained)
        {
            return container.Contains(contained.min)
                && container.Contains(contained.max);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected override bool Intersects(Rect a, ref Rect b)
        {
            return a.Overlaps(b);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bound"></param>
        /// <returns></returns>
        protected override Rect[] Split(Rect bound)
        {
            var bounds = new Rect[4];
            var min = bound.min;
            var size = bound.size / 2f;

            var i = 0;
            for (var x = 0; x < 2; x++)
            {
                for (var y = 0; y < 2; y++)
                {
                    var positionOffset = new Vector2(x * size.x, y * size.y);
                    bounds[i++] = new Rect(min + positionOffset, size);
                }
            }

            return bounds;
        }
    }
}