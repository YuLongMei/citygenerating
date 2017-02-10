using System;
using UnityEngine;

namespace CityGen.Struct
{
    public class IntersectionInfo : IComparable<IntersectionInfo>
    {
        internal float distance;
        internal Vector3 intersection;
        internal Road proposedRoad;
        internal Road intersectedRoad;

        public IntersectionInfo(float distance, Vector3 intersection, Road proposedRoad, Road intersectedRoad)
        {
            this.distance = distance;
            this.intersection = intersection;
            this.proposedRoad = proposedRoad;
            this.intersectedRoad = intersectedRoad;
        }

        int IComparable<IntersectionInfo>.CompareTo(IntersectionInfo other)
        {
            float diff = distance - other.distance;
            if (Mathf.Approximately(diff, 0f))
            {
                return 0;
            }
            else
            {
                return diff > 0f ? 1 : -1;
            }
        }
    }
}

