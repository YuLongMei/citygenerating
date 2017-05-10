using UnityEngine;
using System.Collections.Generic;
using CityGen.Util;

namespace CityGen.Struct
{
    public class Road
    {
        internal Junction start;
        internal Junction end;

        internal float width;

        private float length;
        private Vector3 direction;

        private Rect bound;

        // flag
        private bool validBound = false;
        internal bool isBeManaged = false;

        public Road()
        {
            initialize(Vector3.zero, Vector3.zero, 0f);
        }
        public Road(Junction start, Junction end, float width)
        {
            initialize(start, end, width);
        }
        public Road(Road road)
        {
            initialize(road);
        }

        public Vector3 Direction
        {
            get { return direction; }
        }
        public float Length
        {
            get { return length; }
        }

        public void initialize(Junction start, Junction end, float width)
        {
            this.start = start;
            this.end = end;
            this.width = width;

            direction = end.position - start.position;
            length = direction.magnitude;

            validBound = false;
        }

        public void initialize(Road road)
        {
            initialize(road.start, road.end, road.width);
        }

        public Road translate(Vector3 translationDir)
        {
            return new Road(start.position + translationDir, end.position + translationDir, width);
        }

        public Road stretch(float stretchLength)
        {
            return new Road(start, end.position + direction.normalized * stretchLength, width);
        }

        public Road reverse()
        {
            return new Road(end, start, width);
        }

        /// <summary>
        /// Get the appropriate direction relative to
        /// the given road.
        /// </summary>
        /// <param name="road"></param>
        /// <returns></returns>
        public Vector3 getRelativeDirection(Road road)
        {
            return (road.end == end || road.start == start) ? road.Direction : -road.Direction;
        }

        /// <summary>
        /// Return the angle between 2 roads.
        /// These 2 roads must connect to each other.
        /// </summary>
        /// <param name="road">another road</param>
        /// <returns>the angle</returns>
        public float getAngleWith(Road road)
        {
            return Vector3.Angle(direction, getRelativeDirection(road));
        }

        public List<Road> split(Junction splitPoint)
        {
            var roads = new List<Road>();
            Road road1 = new Road(start, splitPoint, width);
            Road road2 = new Road(splitPoint, end, width);
            roads.Add(road1);
            roads.Add(road2);
            return roads;
        }

        public bool isIntersectingWith(Road other, out Vector3? intersection, bool omitVertices = true)
        {
            if (omitVertices)
            {
                if (start == other.start || start == other.end ||
                    end == other.start || end == other.end)
                {
                    intersection = null;
                    return false;
                }
            }
            return Math.doIntersect(start.position, end.position, other.start.position, other.end.position, out intersection);
        }

        public Rect Bound
        {
            get
            {
                if (!validBound)
                {
                    var width = Mathf.Abs(start.position.x - end.position.x);
                    var height = Mathf.Abs(start.position.z - end.position.z);
                    bound = new Rect(
                        Mathf.Min(start.position.x, end.position.x),
                        Mathf.Min(start.position.z, end.position.z),
                        Mathf.Approximately(width, 0f) ? Config.FLOAT_DELTA : width,
                        Mathf.Approximately(height, 0f) ? Config.FLOAT_DELTA : height);
                    validBound = true;
                }
                return bound;
            }
        }

        public override bool Equals(object obj)
        {
            return start == (obj as Road).start
                && end == (obj as Road).end
                && width == (obj as Road).width;
        }

        public override int GetHashCode()
        {
            return start.GetHashCode() ^ end.GetHashCode() ^ width.GetHashCode();
        }

        public override string ToString()
        {
            return start.ToString() + " " + end.ToString() + " " + width;
        }
    }
}

