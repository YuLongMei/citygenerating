using CityGen.Util;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityGen.Struct
{
    public class BoundingBox
    {
        internal List<Vector3> corners = new List<Vector3>();
        protected float area = float.MaxValue;
        private Vector3 centre = Vector3.zero;
        private float u, v;
        private Vector3 shortEdgeDir = Vector3.zero;
        private Vector3 longEdgeDir = Vector3.zero;

        public float U { get { return u; } }
        public float V { get { return v; } }

        public float Area { get { return area; } }

        public Vector3 Centre { get { return centre; } }

        public Vector3 ShortEdgeDir { get { return shortEdgeDir; } }
        public Vector3 LongEdgeDir { get { return longEdgeDir; } }

        public BoundingBox() { }

        internal bool fit(Polygon polygon)
        {
            var tightenedVertices = polygon.vertices;

            // OBB
            for (int index = 0; index < tightenedVertices.Count - 1; ++index)
            {
                var projectionLineStart = tightenedVertices[index];
                var projectionLineEnd = tightenedVertices[index + 1];

                // Don't cling to the edge to avoid concave polygons.
                var transNor = Vector3.Cross(Vector3.up, projectionLineEnd - projectionLineStart).normalized;
                projectionLineStart += transNor * Config.EPSILON_TRANSLATION_FOR_OBB;
                projectionLineEnd += transNor * Config.EPSILON_TRANSLATION_FOR_OBB;
                var projectionLine = projectionLineEnd - projectionLineStart;

                var sideCoefficients = tightenedVertices.
                    Select(point => Math.sideOfLineForPoint(projectionLineStart, projectionLineEnd, point));

                // Not all of the points are on the same side.
                if (!sideCoefficients.All(d => d >= 0f) &&
                    !sideCoefficients.All(d => d <= 0f))
                {
                    continue;
                }

                var results = tightenedVertices
                    .Select(point =>
                    new
                    {
                        vertex = point,
                        dotProduct = Vector3.Dot(projectionLine, point - projectionLineStart),
                        distance = Math.distanceToLine(projectionLineStart, projectionLineEnd, point)
                    });

                var orderedResultsByProduct = results.OrderBy(item => item.dotProduct);
                var farthestResult = results.Aggregate((a, b) => a.distance > b.distance ? a : b);

                // Points on bounding box.
                var left = orderedResultsByProduct.First().vertex;
                var right = orderedResultsByProduct.Last().vertex;
                var top = farthestResult.vertex;

                // Corners of bounding box.
                var cornerBL = Math.project(projectionLineStart, projectionLineEnd, left);
                var cornerBR = Math.project(projectionLineStart, projectionLineEnd, right);

                // Current area of bounding box is larger than smallest one.
                var u = Vector3.Distance(cornerBL, cornerBR);
                var v = farthestResult.distance;
                var curArea = u * v;
                if (curArea >= Area)
                {
                    continue;
                }

                var translation = -transNor * farthestResult.distance;
                var cornerTL = cornerBL + translation;
                var cornerTR = cornerBR + translation;

                corners = new List<Vector3> { cornerBL, cornerBR, cornerTR, cornerTL };
                area = curArea;
                centre = (cornerTL + cornerBR) * .5f;
                this.u = u;
                this.v = v;
                if (u > v)
                {
                    longEdgeDir = projectionLine.normalized;
                    shortEdgeDir = transNor;
                }
                else
                {
                    longEdgeDir = transNor;
                    shortEdgeDir = projectionLine.normalized;
                }
            }

            return corners.Count >= 4;
        }

        internal List<Vector3> Splitter
        {
            get
            {
                if (corners.Count < 4)
                {
                    return new List<Vector3>();
                }

                float e1 = Random.Range(Config.SPLITTER_LEFT_LIMIT, Config.SPLITTER_RIGHT_LIMIT);
                float e2 = 1f - e1;
                if (u > v)
                {
                    return new List<Vector3>
                    { (corners[0] * e1 + corners[1] * e2), (corners[3] * e1 + corners[2] * e2) };
                }
                else
                {
                    return new List<Vector3>
                    { (corners[0] * e1 + corners[3] * e2), (corners[1] * e1 + corners[2] * e2) };
                }
            }
        }

        internal float AspectRatio
        {
            get
            {
                return u > v ? u / v : v / u;
            }
        }
    }
}
