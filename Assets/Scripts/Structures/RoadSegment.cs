using System;
using System.Collections.Generic;
using UnityEngine;

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

        public Vector3 getStart()
        {
            return roads[0].start;
        }

        public Vector3 getEnd()
        {
            return roads[roads.Count - 1].end;
        }

        public Road getLastRoad()
        {
            return roads[roads.Count - 1];
        }

        public void updateLastRoad(Road road)
        {
            totalLength -= roads[roads.Count - 1].Length;
            roads[roads.Count - 1].initialize(road);
            totalLength += road.Length;
        }

        public void grow(Road road)
        {
            roads.Add(road);
            totalLength += road.Length;
        }
    }
}