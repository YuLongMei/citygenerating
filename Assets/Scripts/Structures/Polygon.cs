using CityGen.Util;
using System.Collections.Generic;
using UnityEngine;

namespace CityGen.Struct
{
    public class Polygon
    {
        internal List<Vector3> vertices = new List<Vector3>();
        internal BoundingBox boundingBox = new BoundingBox();
        protected float area;
        protected Vector2 centre = Vector2.zero;

        protected delegate float PopulationDensityGetter(Polygon polygon);
        protected PopulationDensityGetter getPopulationDensityValue;

        public float Area { get { return area; } }

        public Vector2 Centre { get { return centre; } }

        public Polygon (List<Vector3> vertices)
        {
            this.vertices.AddRange(vertices);
            getPopulationDensityValue += CityGenerator.getPopulationDensityValue;

            if (isClosure())
            {
                area = Math.polygonAreaByShoelace(vertices);

                // Calculate the centre.
                float sum_x = 0f, sum_y = 0f;
                float coefficient = 1f / (6f * area);
                for (int index = 0; index < vertices.Count - 1; ++index)
                {
                    var cur = vertices[index];
                    var next = vertices[index + 1];

                    var latter = cur.x * next.z - next.x * cur.z;
                    sum_x += (cur.x + next.x) * latter;
                    sum_y += (cur.z + next.z) * latter;
                }
                centre.x = coefficient * sum_x;
                centre.y = coefficient * sum_y;
            }
        }

        internal bool isClosure()
        {
            return vertices.Count > 3 ?
                vertices[0].Equals(vertices[vertices.Count - 1]) : false;
        }

        public IEnumerable<Vector3> BoundingBox
        {
            get
            {
                if (!isClosure()) return null;

                if (boundingBox.corners.Count < 4)
                {
                    if (boundingBox.fit(this) == false)
                    {
                        return null;
                    }
                }
                return boundingBox.corners;
            }
        }

        public List<Polygon> split()
        {
            var polygons = new List<Polygon>();
            var _bb = BoundingBox;
            var splitter = boundingBox.Splitter;

            if (isClosure() && splitter.Count == 2)
            {
                var expandedVertices = new List<Vector3>(vertices);
                var intersectionIndexes = new List<int>();

                Vector3? intersection;
                for (int index = 0; index < expandedVertices.Count - 1; ++index)
                {
                    var start = expandedVertices[index];
                    var end = expandedVertices[index + 1];

                    // Splitter goes through this edge.
                    if (Math.doIntersect(splitter[0], splitter[1], start, end, out intersection)
                        && intersection.HasValue)
                    {
                        // If intersection is already contained, 
                        // it means splitter goes through a vertex.
                        int intersectionIndex = expandedVertices.IndexOf(intersection.Value);
                        if (intersectionIndex == -1)
                        {
                            int insertionIndex = ++index;
                            expandedVertices.Insert(insertionIndex, intersection.Value);
                            intersectionIndexes.Add(insertionIndex);
                        }
                        else
                        {
                            if (!intersectionIndexes.Contains(intersectionIndex))
                            {
                                intersectionIndexes.Add(intersectionIndex);
                            }
                        }
                    }
                }

                // Complex situation.
                if (intersectionIndexes.Count != 2)
                {
                    return polygons;
                }

                intersectionIndexes.Sort();
                List<Vector3> leftVertices = new List<Vector3>();
                List<Vector3> rightVertices = new List<Vector3>();

                leftVertices.AddRange(expandedVertices.GetRange(0, intersectionIndexes[0] + 1));
                leftVertices.AddRange(
                    expandedVertices.GetRange(intersectionIndexes[1], expandedVertices.Count - intersectionIndexes[1]));

                rightVertices.AddRange(
                    expandedVertices.GetRange(intersectionIndexes[0], intersectionIndexes[1] - intersectionIndexes[0] + 1));
                rightVertices.Add(expandedVertices[intersectionIndexes[0]]);

                polygons.Add(new Polygon(leftVertices));
                polygons.Add(new Polygon(rightVertices));
            }

            return polygons;
        }

        public float getCentrePopulationDensity()
        {
            return getPopulationDensityValue(this);
        }
    }
}
