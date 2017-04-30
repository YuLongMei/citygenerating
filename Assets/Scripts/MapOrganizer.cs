using CityGen.Struct;
using CityGen.Util;
using UnityEngine;

namespace CityGen
{
    public class MapOrganizer
    {
        internal Vector2 minMapPostion;
        internal Vector2 maxMapPostion;

        private Quadtree<Road> roads;
        private Quadtree<Junction> junctions;

        public MapOrganizer(Vector2 minMapPostion, Vector2 maxMapPostion)
        {
            this.minMapPostion = minMapPostion;
            this.maxMapPostion = maxMapPostion;

            // initalize quadtree
            Rect r = new Rect(minMapPostion, maxMapPostion - minMapPostion);
            junctions = new Quadtree<Junction>(r, 63, 8);
            roads = new Quadtree<Road>(r, 31, 8);
        }

        public Quadtree<Road> Roads
        {
            get { return roads; }
        }

        public Quadtree<Junction> Junctions
        {
            get { return junctions; }
        }

        internal void insertRoad(Road road)
        {
            if (road == null) return;
            if (road.isBeManaged) return;

            roads.Insert(road.Bound, road);
            road.isBeManaged = true;
            insertJunction(road.start, road);
            insertJunction(road.end, road);
        }

        internal void deleteRoad(Road road)
        {
            if (road == null) return;
            if (!road.isBeManaged) return;

            if (roads.Remove(road.Bound, road))
            {
                road.isBeManaged = false;
            }
            updateJunction(road.start, road);
            updateJunction(road.end, road);
        }

        internal void insertJunction(Junction junction, Road road)
        {
            if (junction == null || road == null) return;
            junction.add(road);

            if (!junction.isBeManaged)
            {
                junctions.Insert(junction.Bound, junction);
                junction.isBeManaged = true;
            }
        }

        internal void updateJunction(Junction junction, Road road)
        {
            if (junction == null || road == null) return;
            junction.remove(road);

            if (junction.isBeManaged)
            {
                if (junction.RoadsCount == 0 &&
                    junctions.Remove(junction.Bound, junction))
                {
                    junction.isBeManaged = false;
                }
            }
        }
    }
}

