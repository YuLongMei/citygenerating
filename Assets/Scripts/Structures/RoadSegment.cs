using System;

namespace CityGen.Struct
{
    public class RoadSegment<T> : IComparable<RoadSegment<T>>
        where T : MetaInformation
    {
        internal int timeDelay;
        internal Road road;
        internal T metaInformation;

        public RoadSegment(int timeDelay, Road road, T meta)
        {
            this.timeDelay = timeDelay;
            this.road = road;
            metaInformation = meta;
        }

        public RoadSegment(RoadSegment<T> seg)
        {
            this.timeDelay = seg.timeDelay;
            this.road = seg.road.Clone();
            this.metaInformation = seg.metaInformation;
        }

        int IComparable<RoadSegment<T>>.CompareTo(RoadSegment<T> other)
        {
            return timeDelay - other.timeDelay;
        }
    }
}
