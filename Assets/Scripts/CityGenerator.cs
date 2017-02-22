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
            RoadSegment<MetaInformation> originalSeg, 
            out RoadSegment<MetaInformation> modifiedSeg,
            out bool canGrow);
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
            Road rootRoad = new Road(
                new Vector3(centre.x, 0, centre.y), 
                new Vector3(centre.x + Config.HIGHWAY_DEFAULT_LENGTH, 0, centre.y), 
                Config.HIGHWAY_DEFAULT_LENGTH);
            MetaInformation meta = new HighwayMetaInfo();
            var rootSegment = new RoadSegment<MetaInformation>(0, rootRoad, meta);
            map.insertRoad(rootRoad);
            
            priQueue.Add(rootSegment);

            // loop until priority queue to empty
            while (priQueue.Count > 0 
                && map.Roads.Count < Config.ROAD_COUNT_LIMIT)
            {
                // pop smallest road from priority queue
                RoadSegment<MetaInformation> minSeg = priQueue.DeleteMin();

                // check that it is valid, skip to the next segment if it is not
                RoadSegment<MetaInformation> modifiedSeg = null;
                bool canGrow = false;
                if (localConstraints != null)
                {
                    if(!localConstraints(minSeg, out modifiedSeg, out canGrow))
                    {
                        continue;
                    }
                }

                // It's valid, so add it to list. It is now part of the final result
                map.insertRoad(modifiedSeg.road);

                // produce potential segments leading off this road according to some global goal
                List<RoadSegment<MetaInformation>> pendingSegs = null;
                if (globalGoals != null && canGrow)
                {
                    globalGoals(modifiedSeg, out pendingSegs);

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
        /// <param name="originalSeg"></param>
        /// <param name="modifiedSeg"></param>
        /// <returns>if this road can be accepted</returns>
        protected bool generalLocalConstraint(
            RoadSegment<MetaInformation> originalSeg,
            out RoadSegment<MetaInformation> modifiedSeg,
            out bool canGrow)
        {
            // out of bounds
            if (!withinRange(originalSeg.road))
            {
                modifiedSeg = null;
                canGrow = false;
                return false;
            }

            // 1. If a candidate segment crosses another segment 
            // then join the roads together to form a T-Junction.
            bool originalIntersectWithAnotherRoad = 
                makeTJunction(originalSeg, out modifiedSeg);

            if (originalIntersectWithAnotherRoad)
            {
                canGrow = false;
                return true;
            }

            // 2. If a candidate segment stops near an existing T-Junction 
            // then extend it to join the junction and form a cross-junction.
            bool originalCloseToACrossing =
                makeCrossJunction(originalSeg, out modifiedSeg);

            if (originalCloseToACrossing)
            {
                canGrow = false;
                return true;
            }

            // 3. If a candidate segment stops near to another segment
            // then extend it to join the roads and form a T-Junction.
            var extendedSeg =
                new RoadSegment<MetaInformation>(originalSeg);
            extendedSeg.road.stretch(Config.DETECTIVE_RADIUS_FROM_ENDS);
            // out of bounds
            if (!withinRange(extendedSeg.road.end))
            {
                modifiedSeg = null;
                canGrow = false;
                return false;
            }

            bool originalCloseToARoad =
                makeTJunction(extendedSeg, out modifiedSeg);

            if (originalCloseToACrossing)
            {
                canGrow = false;
                return true;
            }

            // 4. Accept this segment without any changes.
            modifiedSeg = originalSeg;
            canGrow = true;
            return true;
        }

        protected bool makeTJunction(
            RoadSegment<MetaInformation> originalSeg,
            out RoadSegment<MetaInformation> modifiedSeg)
        {
            Road originalRoad = originalSeg.road;

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
                        var seg = new IntersectionInfo(
                            distance, intersection.Value, proposedRoad, road);
                        priQueue.Add(seg);
                    }
                }
            }

            // there's no intersection
            if (priQueue.IsEmpty)
            {
                modifiedSeg = originalSeg;
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
            modifiedSeg = new RoadSegment<MetaInformation>(
                originalSeg.timeDelay,
                nearestSeg.proposedRoad,
                originalSeg.metaInformation);
            return true;
        }

        protected bool makeCrossJunction(
            RoadSegment<MetaInformation> originalSeg,
            out RoadSegment<MetaInformation> modifiedSeg)
        {
            Road originalRoad = originalSeg.road;
            Junction closest = 
                findClosestJunction(originalRoad.end, Config.DETECTIVE_RADIUS_FROM_ENDS);

            if (closest == null)
            {
                modifiedSeg = originalSeg;
                return false;
            }

            modifiedSeg = new RoadSegment<MetaInformation>(
                originalSeg.timeDelay,
                new Road(originalRoad.start, closest.position, originalRoad.width),
                originalSeg.metaInformation);
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
            potentialSegs = new List<RoadSegment<MetaInformation>>();
            var pendingSegs = new List<RoadSegment<MetaInformation>>();
            float digreeDiff = Config.HIGHWAY_GROWTH_MAX_DEGREE - Config.HIGHWAY_GROWTH_MIN_DEGREE;
            Road approvedRoad = approvedSeg.road;

            for (int i = 0; i < 4; ++i)
            {
                // get a growth digree randomly
                float growthDigree = Random.Range(-digreeDiff, digreeDiff);
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
                pendingSegs.Add(potentialSeg);
            }

            // pick out the road where has the most population density
            var maxDensityRoad =
                pendingSegs
                .OrderByDescending(x => ((HighwayMetaInfo)x.metaInformation).populationDensity)
                .Take(2);
            potentialSegs.AddRange(maxDensityRoad);

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
