using UnityEngine;
using System.Collections.Generic;

namespace CityGen.Struct
{
    public class Junction
    {
        internal Vector3 position;
        private Rect bound;

        private HashSet<Road> connectedRoads = new HashSet<Road>();

        // flag
        private bool validBound = false;
        internal bool isBeManaged = false;

        public Junction(Vector3 position)
        {
            this.position = position;
        }

        public Junction(float x, float y, float z)
        {
            position = new Vector3(x, y, z);
        }

        public static implicit operator Junction(Vector3 position)
        {
            return new Junction(position);
        }

        public Junction(Junction junction)
        {
            position = junction.position;

            var enumerator = junction.Roads.GetEnumerator();
            while (enumerator.MoveNext())
            {
                connectedRoads.Add(enumerator.Current);
            }
        }

        public IEnumerable<Road> Roads
        {
            get { return connectedRoads; }
        }

        public int RoadsCount
        {
            get { return connectedRoads.Count; }
        }

        public void add(Road road)
        {
            if (road.Length == 0f)
            {
                return;
            }
            connectedRoads.Add(road);
        }

        public bool remove(Road road)
        {
            return connectedRoads.Remove(road);
        }

        public Rect Bound
        {
            get
            {
                if (!validBound)
                {
                    bound = new Rect(
                        position.x - Config.FLOAT_DELTA,
                        position.z - Config.FLOAT_DELTA,
                        Config.FLOAT_DELTA * 2,
                        Config.FLOAT_DELTA * 2);
                    validBound = true;
                }
                return bound;
            }
        }

        public override bool Equals(object obj)
        {
            return position == ((Junction)obj).position;
        }

        public override int GetHashCode()
        {
            return position.GetHashCode();
        }

        public override string ToString()
        {
            return position.ToString();
        }
    }
}

