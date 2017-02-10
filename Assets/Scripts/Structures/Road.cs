using UnityEngine;
using System.Collections.Generic;
using CityGen.Util;

namespace CityGen.Struct
{
    public class Road
    {
        internal Vector3 start;
        internal Vector3 end;

        internal float width;

        private float length;
        private Vector3 direction;
        
        public Road()
        {
            initialize(Vector3.zero, Vector3.zero, 0f);
        }
        public Road(Vector3 start, Vector3 end, float width)
        {
            initialize(start, end, width);
        }

        public Vector3 Direction
        {
            get { return direction; }
        }
        public float Length
        {
            get { return length; }
        }

        public void initialize(Vector3 start, Vector3 end, float width)
        {
            this.start = start;
            this.end = end;
            this.width = width;

            direction = end - start;
            length = direction.magnitude;
        }

        public void setStart(Vector3 start)
        {
            initialize(start, end, width);
        }
        public void setEnd(Vector3 end)
        {
            initialize(start, end, width);
        }
        public Road Clone()
        {
            return new Road(start, end, width);
        }

        public Road translate(Vector3 vector)
        {
            start += vector;
            end += vector;
            return this;
        }

        public Road stretch(float stretchLength)
        {
            initialize(start, end + direction.normalized * stretchLength, width);
            return this;
        }

        public List<Road> split(Vector3 splitPoint)
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
                if (this.start == other.start || this.start == other.end ||
                    this.end == other.start || this.end == other.end)
                {
                    intersection = null;
                    return false;
                }
            }
            return Math.doIntersect(this.start, this.end, other.start, other.end, out intersection);
        }

        public Rect Bound
        {
            get
            {
                return new Rect(
                    Mathf.Min(start.x, end.x),
                    Mathf.Min(start.z, end.z),
                    Mathf.Abs(start.x - end.x),
                    Mathf.Abs(start.z - end.z));
            }
        }

        public override bool Equals(object obj)
        {
            return start == (obj as Road).start
                && end == (obj as Road).end
                && width == (obj as Road).width;
        }
    }
}

