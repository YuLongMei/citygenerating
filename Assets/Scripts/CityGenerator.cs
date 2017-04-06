using C5;
using CityGen.ParaMaps;
using CityGen.Struct;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CityGen
{
    public class CityGenerator : MonoBehaviour
    {
        public Vector2 minMapPostion;
        public Vector2 maxMapPostion;

        public string seed = null;

        private MapOrganizer map;
        private ParameterMaps paraMaps;

        // local constraints as delegate
        delegate bool LocalConstraint(
            ref RoadSegment<MetaInformation> seg);
        private LocalConstraint localConstraints = null;

        // global goals as delegate
        delegate bool GlobalGoal(
            RoadSegment<MetaInformation> approvedSeg,
            out List<RoadSegment<MetaInformation>> potentialSegs);
        private GlobalGoal globalGoals = null;

        // Use this for initialization
        void Start()
        {
            map = new MapOrganizer(minMapPostion, maxMapPostion);
            paraMaps = new ParameterMaps(
                (int)(maxMapPostion.x - minMapPostion.x),
                (int)(maxMapPostion.y - minMapPostion.y));

            localConstraints += generalLocalConstraint;
            globalGoals += makeCandidatesByPopulationDensity;

            generate(seed);
        }

        void Update()
        {
            drawDebug();
        }

        protected SimulatedParaMap perlin;
        public void generate(string seed)
        {
            // initalize random state by the hash code of seed
            Random.InitState(seed.GetHashCode());

            // population density for global goals
            // ------------------------------TEST---------------------------------
            perlin = new SimulatedParaMap(
                paraMaps.Width,
                paraMaps.Height,
                null,
                Random.Range(-99999f, 99999f),
                Random.Range(-99999f, 99999f));
            var ground = GameObject.Find("Ground");
            ground.transform.localScale = 
                new Vector3(paraMaps.Width * ground.transform.localScale.x, 1, paraMaps.Height * ground.transform.localScale.x);
            var renderer = ground.GetComponent<Renderer>();
            renderer.material.mainTexture = perlin.Map;
            // ------------------------------TEST---------------------------------

            // priority queue
            var priQueue = new IntervalHeap<RoadSegment<MetaInformation>>();

            // first road segment
            Vector2 centre = (maxMapPostion + minMapPostion) * .5f;
            var centre3 = new Vector3(centre.x, 0, centre.y);
            Road rootRoad = new Road(
                centre3, 
                new Vector3(centre.x + Config.HIGHWAY_DEFAULT_LENGTH, 0, centre.y), 
                Config.HIGHWAY_DEFAULT_LENGTH);
            MetaInformation meta = new HighwayMetaInfo();
            var rootSegment = new RoadSegment<MetaInformation>(0, rootRoad, meta);
            map.insertRoad(rootRoad);
            priQueue.Add(rootSegment);

            rootRoad = new Road(
                centre3,
                new Vector3(centre.x - Config.HIGHWAY_DEFAULT_LENGTH, 0, centre.y),
                Config.HIGHWAY_DEFAULT_LENGTH);
            meta = new HighwayMetaInfo();
            rootSegment = new RoadSegment<MetaInformation>(0, rootRoad, meta);
            map.insertRoad(rootRoad);
            priQueue.Add(rootSegment);

            // loop until priority queue to empty
            while (priQueue.Count > 0 
                && map.Roads.Count < Config.ROAD_COUNT_LIMIT)
            {
                // pop smallest road from priority queue
                RoadSegment<MetaInformation> minSeg = priQueue.DeleteMin();

                // check that it is valid, skip to the next segment if it is not
                if (localConstraints != null)
                {
                    if(!localConstraints(ref minSeg))
                    {
                        continue;
                    }
                }

                // It's valid, so add it to list. It is now part of the final result
                map.insertRoad(minSeg.getLastRoad());

                // produce potential segments leading off this road according to some global goal
                List<RoadSegment<MetaInformation>> pendingSegs = null;
                if (globalGoals != null && minSeg.canGrow)
                {
                    globalGoals(minSeg, out pendingSegs);

                    foreach (RoadSegment<MetaInformation> seg in pendingSegs)
                    {
                        seg.timeDelay += minSeg.timeDelay + 1;
                        priQueue.Add(seg);
                    }
                }
            }
        }

        #region Local Constraint
        /// <summary>
        /// General local constraint
        /// </summary>
        /// <param name="seg"></param>
        /// <returns>if this road can be accepted</returns>
        protected bool generalLocalConstraint(
            ref RoadSegment<MetaInformation> seg)
        {
            // out of bounds
            if (!withinRange(seg.getLastRoad()))
            {
                seg.canGrow = false;
                return false;
            }

            // 1. If a candidate segment crosses another segment 
            // then join the roads together to form a T-Junction.
            bool originalIntersectWithAnotherRoad = 
                makeTJunction(ref seg);

            if (originalIntersectWithAnotherRoad)
            {
                seg.canGrow = false;
                return true;
            }

            // 2. If a candidate segment stops near an existing T-Junction 
            // then extend it to join the junction and form a cross-junction.
            bool originalCloseToACrossing =
                makeCrossJunction(ref seg);

            if (originalCloseToACrossing)
            {
                seg.canGrow = false;
                return true;
            }

            // 3. If a candidate segment stops near to another segment
            // then extend it to join the roads and form a T-Junction.
            var extendedSeg =
                new RoadSegment<MetaInformation>(seg);
            extendedSeg.getLastRoad().stretch(Config.DETECTIVE_RADIUS_FROM_ENDS);
            // out of bounds
            if (!withinRange(extendedSeg.getEnd()))
            {
                seg.canGrow = false;
                return false;
            }

            bool originalCloseToARoad =
                makeTJunction(ref extendedSeg);

            if (originalCloseToARoad)
            {
                seg = extendedSeg;
                seg.canGrow = false;
                return true;
            }

            // 4. Accept this segment without any changes.
            seg.canGrow = true;
            return true;
        }

        protected bool makeTJunction(
            ref RoadSegment<MetaInformation> seg)
        {
            Road originalRoad = seg.getLastRoad();

            // retrieve all generated roads whose bounds are overlapped
            // with this road's bound.
            var pendingRoads =
                map.Roads.Intersects(originalRoad.Bound);

            // priority queue
            var priQueue = new IntervalHeap<IntersectionInfo>();

            // to find the nearest intersection
            foreach (Road road in pendingRoads)
            {
                Vector3? intersection;
                // checking if intersected
                if (road.isIntersectingWith(originalRoad, out intersection))
                {
                    if (intersection.HasValue)
                    {
                        Road proposedRoad = new Road(originalRoad.start, intersection.Value, originalRoad.width);
                        float distance = proposedRoad.Length;
                        var info = new IntersectionInfo(
                            distance, intersection.Value, proposedRoad, road);
                        priQueue.Add(info);
                    }
                }
            }

            // there's no intersection
            if (priQueue.IsEmpty)
            {
                return false;
            }

            // get the nearest intersected road
            var nearestSeg = priQueue.DeleteMin();

            // restructuring the roads
            List<Road> newRoads = nearestSeg.intersectedRoad.split(nearestSeg.intersection);
            newRoads.Add(nearestSeg.proposedRoad);
            foreach (Road road in newRoads)
            {
                map.insertRoad(road);
            }
            map.deleteRoad(nearestSeg.intersectedRoad);

            // return
            seg.updateLastRoad(nearestSeg.proposedRoad);
            return true;
        }

        protected bool makeCrossJunction(
            ref RoadSegment<MetaInformation> seg)
        {
            Road originalRoad = seg.getLastRoad();
            Junction closest = 
                findClosestJunction(originalRoad.end, Config.DETECTIVE_RADIUS_FROM_ENDS);

            if (closest == null)
            {
                return false;
            }

            seg.updateLastRoad(new Road(originalRoad.start, closest.position, originalRoad.width));
            return true;
        }

        private Junction findClosestJunction(Junction centre, float radius)
        {
            // get every junctions in the square which is
            // diameter * diameter
            var diameter = 2 * radius;
            var candidates = map.Junctions.Intersects(
                new Rect(centre.position.x - radius, centre.position.z - radius,
                diameter, diameter));

            Junction closest = null;
            var closestDistanceSqr = float.MaxValue;
            foreach (var candidate in candidates)
            {
                // ignore itself
                if (candidate.Equals(centre))
                {
                    continue;
                }

                // select the closest candidate
                var distanceSqr = (candidate.position - centre.position).sqrMagnitude;
                if (distanceSqr > closestDistanceSqr)
                {
                    continue;
                }

                closestDistanceSqr = distanceSqr;
                closest = candidate;
            }

            // The query is a circle, but we checked a rectangle,
            // reject if it's in the rect but outside the circle
            if (closestDistanceSqr > radius * radius)
                return null;

            return closest;
        }
        #endregion

        #region Global Goals
        protected bool makeCandidatesByPopulationDensity(
            RoadSegment<MetaInformation> approvedSeg,
            out List<RoadSegment<MetaInformation>> potentialSegs)
        {
            // Vaialble declarations
            potentialSegs = new List<RoadSegment<MetaInformation>>();
            bool branchAppeared = false;
            var pendingBranchSegs = new List<RoadSegment<MetaInformation>>();
            float branchDigreeDiff = 0f;

            // If segment grows beyond the fixed length,
            // several branches will appear.
            if (approvedSeg.TotalLength >= Config.HIGHWAY_SEGMENT_MAX_LENGTH)
            {
                branchAppeared = true;
            }

            // growth segment will grow along last segment.
            var pendingGrowthSegs = new List<RoadSegment<MetaInformation>>();
            float growthDigreeDiff = Config.HIGHWAY_GROWTH_MAX_DEGREE - Config.HIGHWAY_GROWTH_MIN_DEGREE;
            // branch segment will make a branch from the end.
            if (branchAppeared)
            {
                branchDigreeDiff = Config.HIGHWAY_BRANCH_MAX_DEGREE - Config.HIGHWAY_BRANCH_MIN_DEGREE;
            }
            
            Road approvedRoad = approvedSeg.getLastRoad();

            for (int i = 0; i < 4; ++i)
            {
                // get a growth digree randomly
                float growthDigree = Random.Range(-growthDigreeDiff, growthDigreeDiff);
                growthDigree += (growthDigree > 0) ? 
                    Config.HIGHWAY_GROWTH_MIN_DEGREE : -Config.HIGHWAY_GROWTH_MIN_DEGREE;

                // figure out the end point of new road
                var rotation = Quaternion.Euler(0, growthDigree, 0);
                var potentialRoadEnd = approvedRoad.end +
                    rotation * approvedRoad.Direction.normalized * Config.HIGHWAY_DEFAULT_LENGTH;

                // create new road
                var potentialRoad = new Road(approvedRoad.end, potentialRoadEnd, Config.HIGHWAY_DEFAULT_WIDTH);
                var metaInfo = new HighwayMetaInfo();
                metaInfo.populationDensity = perlin.getValue(potentialRoadEnd.x, potentialRoadEnd.z);
                var potentialSeg = new RoadSegment<MetaInformation>(0, potentialRoad, metaInfo);
                pendingGrowthSegs.Add(potentialSeg);

                if (branchAppeared)
                {
                    // get a branch digree randomly
                    float branchDigree = Random.Range(-branchDigreeDiff, branchDigreeDiff);
                    branchDigree += (branchDigree > 0) ?
                        Config.HIGHWAY_BRANCH_MIN_DEGREE : -Config.HIGHWAY_BRANCH_MIN_DEGREE;

                    // figure out the end point of new branch road
                    rotation = Quaternion.Euler(0, branchDigree, 0);
                    potentialRoadEnd = approvedRoad.end +
                        rotation * approvedRoad.Direction.normalized * Config.HIGHWAY_DEFAULT_LENGTH;

                    // create new branch road
                    potentialRoad = new Road(approvedRoad.end, potentialRoadEnd, Config.HIGHWAY_DEFAULT_WIDTH);
                    metaInfo = new HighwayMetaInfo();
                    metaInfo.populationDensity = perlin.getValue(potentialRoadEnd.x, potentialRoadEnd.z);
                    potentialSeg = new RoadSegment<MetaInformation>(0, potentialRoad, metaInfo);
                    pendingBranchSegs.Add(potentialSeg);
                }
            }

            // pick out the road where has the most population density
            var maxDensityGrowthRoad =
                pendingGrowthSegs
                .OrderByDescending(x => ((HighwayMetaInfo)x.metaInformation).populationDensity)
                .FirstOrDefault();

            if (branchAppeared)
            {
                var maxDensityBranchRoad =
                pendingBranchSegs
                .OrderByDescending(x => ((HighwayMetaInfo)x.metaInformation).populationDensity)
                .Take(1);

                // as for growth road, add it to result directly
                potentialSegs.Add(maxDensityGrowthRoad);

                // as for branch road, add if it has higher population density
                var growthRoadDensity = ((HighwayMetaInfo)maxDensityGrowthRoad.metaInformation).populationDensity;
                foreach (var road in maxDensityBranchRoad)
                {
                    if (((HighwayMetaInfo)road.metaInformation).populationDensity > growthRoadDensity)
                    {
                        potentialSegs.Add(road);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                // segment grows
                approvedSeg.grow(maxDensityGrowthRoad.getLastRoad());
                approvedSeg.metaInformation = maxDensityGrowthRoad.metaInformation;

                potentialSegs.Add(approvedSeg);
            }

            return potentialSegs.Count > 0;
        }
        #endregion

        public bool withinRange(Junction junction)
        {
            return map.Junctions.Bounds.Contains(
                new Vector2(junction.position.x, junction.position.z));
        }

        public bool withinRange(Road road)
        {
            return withinRange(road.start)
                && withinRange(road.end);
        }

        #region Debug
        void drawDebug()
        {
            var roads = map.Roads.GetEnumerator();
            while (roads.MoveNext())
            {
                var road = roads.Current.Value;
                Debug.DrawLine(road.start, road.end, Color.red);
            }
        }
        #endregion
    }
}
