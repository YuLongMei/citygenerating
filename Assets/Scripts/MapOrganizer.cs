using CityGen.Struct;
using CityGen.Util;
using System.Linq;
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
            roads.Insert(road.Bound, road);
            insertJunction(road.start, road);
            insertJunction(road.end, road);
        }

        internal void deleteRoad(Road road)
        {
            roads.Remove(road.Bound, road);
            updateJunction(road.start, road);
            updateJunction(road.end, road);
        }

        internal void insertJunction(Junction junction, Road road)
        {
            var foundJunction = junctions.Intersects(junction.Bound);
            if (foundJunction.Any())
            {
                foreach (var j in foundJunction)
                {
                    j.add(road);
                }
            }
            else
            {
                junction.add(road);
                junctions.Insert(junction.Bound, junction);
            }
        }

        internal void updateJunction(Junction junction, Road road)
        {
            var foundJunction = junctions.Intersects(junction.Bound);
            if (foundJunction.Any())
            {
                foreach (var j in foundJunction)
                {
                    j.remove(road);
                    if (j.RoadsCount == 0)
                    {
                        junctions.Remove(j.Bound, j);
                    }
                }
            }
        }
    }
}

