using UnityEngine;
using System.Collections.Generic;

namespace CityGen.Util
{
    public static class Math
    {
        /// <summary>
        /// determine whether or not two lines intersect, 
        /// and return the intersection
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <param name="intersection"></param>
        /// <returns></returns>
        internal static bool doIntersect(Vector3 p1, Vector3 p2, Vector3 q1, Vector3 q2, 
            out Vector3? intersection)
        {
            Vector3 r = p2 - p1;
            Vector3 s = q2 - q1;

            Vector3 denominator = Vector3.Cross(r, s);
            Vector3 uNumerator = Vector3.Cross(q1 - p1, r);

            if (uNumerator == Vector3.zero && denominator == Vector3.zero)
            {
                // the two lines are collinear.
                intersection = null;

                var t0 = Vector3.Dot((q1 - p1), r) / Vector3.Dot(r, r);
                var t1 = t0 + Vector3.Dot(s, r) / Vector3.Dot(r, r);

                // If the interval between t0 and t1 intersects 
                // the interval[0, 1] then the line segments are
                // collinear and overlapping; 
                // otherwise they are collinear and disjoint.
                return (t0 >= 0f && t0 <= 1f) ||
                    (t1 >= 0f && t1 <= 1f);
            }

            if (denominator == Vector3.zero)
            {
                // the two lines are parallel and non-intersecting.
                intersection = null;
                return false;
            }

            var u = uNumerator.y / denominator.y;
            var t = Vector3.Cross(q1 - p1, s).y / denominator.y;

            if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
            {
                // two line segments meet at the point p + t r = q + u s.
                intersection = p1 + t * r;
                return true;
            }
            else
            {
                // the two line segments are not parallel but do not intersect.
                intersection = null;
                return false;
            }
        }

        internal static float angle360(Vector3 from, Vector3 to)
        {
            Vector3 crossProduct = Vector3.Cross(from, to);
            float angle = Vector3.Angle(from, to);
            return crossProduct.y > 0 ? angle : 360f - angle;
        }

        internal static float polygonAreaByShoelace(List<Vector3> vertices)
        {
            float area = 0f;

            for (int index = 0; index < vertices.Count - 1; ++index)
            {
                var cur = vertices[index];
                var next = vertices[index + 1];

                area += cur.x * next.z - next.x * cur.z;
            }

            return .5f * area;
        }
    }
}

