using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CityGen.Util;

namespace CityGen.Struct
{
    public class Block
    {
        private List<Road> edges = new List<Road>();
        private List<Vector3> tightenedVertices = new List<Vector3>();
        private float area = -1f;

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
                if (tightenedVertices.Count < 4)
                {
                    tightenUp();
                }
                return tightenedVertices;
            }
        }

        public float Area
        {
            get
            {
                if (area <= 0f && tightenedVertices.Count > 3)
                {
                    area = Math.polygonAreaByShoelace(tightenedVertices);
                }
                return area;
            }
        }

        protected bool tightenUp()
        {
            if (isClosure())
            {
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

                    tightenedVertices.Add(newVertex);
                }

                for (int index = 0; index < tightenedVertices.Count; ++index)
                {
                    int last = (index - 1 + tightenedVertices.Count) % tightenedVertices.Count;
                    int next = (index + 1) % tightenedVertices.Count;

                    var a = tightenedVertices[index] - tightenedVertices[last];
                    var b = tightenedVertices[next] - tightenedVertices[index];

                    if (Vector3.Angle(a, b) > 180 - Config.SMALLEST_DEGREE_BETWEEN_TWO_ROADS)
                    {
                        // Index needs to be counted down.
                        tightenedVertices.RemoveAt(index--);
                    }
                }

                tightenedVertices.Add(tightenedVertices[0]);
            }
            return tightenedVertices.Count > 3;
        }
    }
}