using UnityEngine;
using System.Collections.Generic;

namespace CityGen.Struct
{
    public class Junction
    {
        internal Vector3 position;

        private List<Road> connectedRoads = new List<Road>();

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
            connectedRoads.Add(road);
        }

        public bool remove(Road road)
        {
            return connectedRoads.Remove(road);
        }

        public Rect Bound
        {
            get { return new 
                    Rect(position.x - Config.FLOAT_DELTA,
                    position.z - Config.FLOAT_DELTA,
                    Config.FLOAT_DELTA * 2,
                    Config.FLOAT_DELTA * 2); }
        }

        public override bool Equals(object obj)
        {
            return position == ((Junction)obj).position;
        }
    }
}

