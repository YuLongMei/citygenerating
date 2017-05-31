using CityGen.Util;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityGen.Struct
{
    public class Block
    {
        private List<Road> edges = new List<Road>();
        private Polygon tightenedPolygon = null;
        private List<Polygon> lots = new List<Polygon>();

        internal void addEdges(Road road)
        {
            edges.Add(road);
        }

        internal bool isClosure()
        {
            return edges.Count > 2 ?
                edges[0].start.Equals(edges[edges.Count - 1].end) : false;
        }

        public IEnumerable<Vector3> Boundary
        {
            get
            {
                if (tightenedPolygon == null ||
                    tightenedPolygon.vertices.Count < 4)
                {
                    if (tightenUp() == false)
                    {
                        return null;
                    }
                }
                return tightenedPolygon.vertices;
            }
        }

        public IEnumerable<Vector3> BoundingBox
        {
            get
            {
                if (tightenedPolygon != null)
                {
                    return tightenedPolygon.BoundingBox;
                }
                return null;
            }
        }

        public IEnumerable<Polygon> Lots
        {
            get
            {
                if (lots.Count <= 0)
                {
                    subdivide();
                }
                return lots;
            }
        }

        protected bool tightenUp()
        {
            if (isClosure())
            {
                List<Vector3> vertices = new List<Vector3>();

                for (int index = 0; index < edges.Count; ++index)
                {
                    int next = (index + 1) % edges.Count;
                    // The angle between two roads.
                    var theta = 
                        Math.angle360(edges[index].Direction, edges[index].getRelativeDirection(edges[next]))
                        * Mathf.Deg2Rad;

                    var h1 = edges[index].width * .5f;
                    var h2 = edges[next].width * .5f;

                    var theta2 =
                        Mathf.Atan(Mathf.Sin(theta) / (h1 / h2 + Mathf.Cos(theta)));
                    var distance = h2 / Mathf.Sin(theta2);

                    var rotation = Quaternion.AngleAxis(-theta2 * Mathf.Rad2Deg, Vector3.up);
                    var newVertex = edges[index].end.position + rotation * edges[next].Direction.normalized * distance;

                    vertices.Add(newVertex);
                }

                for (int index = 0; index < vertices.Count; ++index)
                {
                    int last = (index - 1 + vertices.Count) % vertices.Count;
                    int next = (index + 1) % vertices.Count;

                    var a = vertices[index] - vertices[last];
                    var b = vertices[next] - vertices[index];

                    if (Vector3.Angle(a, b) > 180 - Config.SMALLEST_DEGREE_BETWEEN_TWO_ROADS)
                    {
                        // Index needs to be counted down.
                        vertices.RemoveAt(index--);
                    }
                }

                vertices.Add(vertices[0]);

                tightenedPolygon = new Polygon(vertices);
            }
            return tightenedPolygon != null || tightenedPolygon.vertices.Count > 3;
        }

        internal bool subdivide()
        {
            var _boundary = Boundary;

            lots.AddRange(subdivide(tightenedPolygon));
            
            return lots.Count > 0;
        }

        protected List<Polygon> subdivide(Polygon polygon)
        {
            var lots = new List<Polygon>();
            var parts = polygon.split();

            if (parts.Count <= 0 ||
                parts.Any(subdivisionRules))
            {
                lots.Add(polygon);
                return lots;
            }

            foreach (var part in parts)
            {
                lots.AddRange(subdivide(part));
            }

            return lots;
        }

        protected bool subdivisionRules(Polygon polygon)
        {
            if (polygon.Area <= 0f)
            {
                return true;
            }

            var pd = polygon.getCentrePopulationDensity();
            pd = (pd - Config.MIN_STREET_APPEAR_POPULATION_DENSITY_VALUE) / 
                (Config.MAX_POPULATION_DENSITY_VALUE - Config.MIN_STREET_APPEAR_POPULATION_DENSITY_VALUE);
            pd = Mathf.Max(0f, pd);
            var minArea = Config.LOT_AREA_BASIS +
                Mathf.Pow(pd * Config.LOT_AREA_MULTIPLE, Config.LOT_AREA_EXPONENT) +
                Random.value * Config.LOT_AREA_CORRECTION;
            
            return polygon.Area < minArea;
        }
    }
}