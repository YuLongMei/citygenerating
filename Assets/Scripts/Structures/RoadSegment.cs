using System;
using System.Collections.Generic;

namespace CityGen.Struct
{
    public class RoadSegment<T> : IComparable<RoadSegment<T>>
        where T : MetaInformation
    {
        internal int timeDelay;
        internal List<Road> roads;
        internal T metaInformation;

        // flags
        internal bool growthBlocked = false;
        internal bool successionBlocked = false;
        internal bool tooShortJudgment = true;
        internal bool discarded = false;

        protected float totalLength = 0f;

        public RoadSegment(int timeDelay, Road road, T meta)
        {
            this.timeDelay = timeDelay;
            roads = new List<Road>();
            grow(road);
            metaInformation = meta;
        }

        public RoadSegment(RoadSegment<T> seg)
        {
            timeDelay = seg.timeDelay;
            roads = new List<Road>(seg.roads);
            totalLength = seg.TotalLength;
            metaInformation = seg.metaInformation;

            growthBlocked = seg.growthBlocked;
            successionBlocked = seg.successionBlocked;
            discarded = seg.discarded;
        }

        public float TotalLength
        {
            get { return totalLength; }
        }

        int IComparable<RoadSegment<T>>.CompareTo(RoadSegment<T> other)
        {
            return timeDelay - other.timeDelay;
        }

        public Junction getStart()
        {
            return roads.Count > 0 ?
                roads[0].start : null;
        }

        public Junction getEnd()
        {
            return roads.Count > 0 ?
                roads[roads.Count - 1].end : null;
        }

        public Road getFirstRoad()
        {
            return roads.Count > 0 ?
                roads[0] : null;
        }

        public Road getLastRoad()
        {
            return roads.Count > 0 ?
                roads[roads.Count - 1] : null;
        }

        public void updateLastRoad(Road road)
        {
            if (roads.Count > 0)
            {
                totalLength -= roads[roads.Count - 1].Length;
                roads[roads.Count - 1] = road;
                totalLength += road.Length;
            }
        }

        public void grow(Road road)
        {
            if (road.Length == 0f)
            {
                return;
            }
            roads.Add(road);
            totalLength += road.Length;
        }

        public void deleteLastRoad()
        {
            if (roads.Count > 0)
            {
                roads.RemoveAt(roads.Count - 1);
            }
        }
    }
}