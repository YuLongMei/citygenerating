using CityGen.Struct;
using CityGen.Util;
using System.Collections.Generic;
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
        private HashSet<Road> reversedRoads;
        private List<Block> blocks;

        public MapOrganizer(Vector2 minMapPostion, Vector2 maxMapPostion)
        {
            this.minMapPostion = minMapPostion;
            this.maxMapPostion = maxMapPostion;

            // initalize quadtree
            Rect r = new Rect(minMapPostion, maxMapPostion - minMapPostion);
            junctions = new Quadtree<Junction>(r, 31, 8);
            roads = new Quadtree<Road>(r, 63, 8);
            reversedRoads = new HashSet<Road>();
            blocks = new List<Block>();
        }

        public Quadtree<Road> Roads
        {
            get { return roads; }
        }

        public Quadtree<Junction> Junctions
        {
            get { return junctions; }
        }

        public HashSet<Road> twowayRoads
        {
            get
            {
                var twowayRoadsObjects = new HashSet<Road>(reversedRoads);
                twowayRoadsObjects.UnionWith(RoadsEnumerable);
                return twowayRoadsObjects;
            }
        }

        public List<Block> Blocks
        {
            get { return blocks; }
        }

        internal IEnumerable<Road> RoadsEnumerable
        {
            get { return roads.Intersects(roads.Bounds); }
        }

        internal IEnumerable<Junction> JunctionsEnumerable
        {
            get { return junctions.Intersects(junctions.Bounds); }
        }

        internal void insertRoad(Road road)
        {
            if (road == null) return;
            if (road.isBeManaged) return;

            // Do not insert this road if there's a reversed one.
            var reversedRoad = road.reverse();
            if (reversedRoads.Contains(reversedRoad) ||
                reversedRoads.Contains(road))
            {
                return;
            }

            roads.Insert(road.Bound, road);
            road.isBeManaged = true;
            reversedRoads.Add(reversedRoad);

            insertJunction(road.start, road);
            insertJunction(road.end, road);
        }

        internal void deleteRoad(Road road, bool deleteToFork = true)
        {
            if (road == null) return;
            if (!road.isBeManaged) return;
            
            if (roads.Remove(road.Bound, road))
            {
                road.isBeManaged = false;
                reversedRoads.Remove(road.reverse());
            }

            updateJunction(road.start, road);
            updateJunction(road.end, road);

            // Delete all roads until meeting a fork.
            if (deleteToFork)
            {
                if (road.start.RoadsCount == 1)
                {
                    deleteRoad(road.start.Roads.First());
                }
                if (road.end.RoadsCount == 1)
                {
                    deleteRoad(road.end.Roads.First());
                }
            }
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

        internal void addBlock(Block block)
        {
            blocks.Add(block);
        }
    }
}

