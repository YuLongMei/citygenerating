using C5;
using CityGen.ParaMaps;
using CityGen.Struct;
using CityGen.Util;
using System.Collections;
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

        public GameObject terrainPrefab;
        public GameObject buildingPrefab;

        private MapOrganizer map;
        private ParameterMaps paraMaps;
        private BuildingPool pool;

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
            var width = maxMapPostion.x - minMapPostion.x;
            var length = maxMapPostion.y - minMapPostion.y;
            paraMaps = new ParameterMaps(
                (int)width, (int)length);
            pool = GetComponent<BuildingPool>();

            GameObject terrainObject =
                Instantiate(terrainPrefab, new Vector3(minMapPostion.x, 0f, minMapPostion.y), Quaternion.identity);
            terrainObject.GetComponent<Terrain>().terrainData.size =
                new Vector3(width, 0, length);

            localConstraints += generalLocalConstraint;
            globalGoals += makeCandidatesByPopulationDensity;

            StartCoroutine(generate(seed));
        }

        void Update()
        {
            drawDebug();
        }

        protected static SimulatedParaMap perlin;
        public IEnumerator generate(string seed)
        {
            // initalize random state by the hash code of seed
            Random.InitState(seed.GetHashCode());

            int roadCount = 0;

            // population density for global goals
            // ------------------------------TEST---------------------------------
            perlin = new SimulatedParaMap(
                paraMaps.Width,
                paraMaps.Height,
                null,
                Random.Range(-99999f, 99999f),
                Random.Range(-99999f, 99999f));
            // ------------------------------TEST---------------------------------

            // priority queue
            var priQueue = new IntervalHeap<RoadSegment<MetaInformation>>();

            // first road segment
            Vector2 centre = (maxMapPostion + minMapPostion) * .5f;
            var centre3 = new Junction(centre.x, 0, centre.y);
            Road rootRoad = new Road(
                centre3,
                new Vector3(centre.x + Config.HIGHWAY_DEFAULT_LENGTH, 0, centre.y),
                Config.HIGHWAY_DEFAULT_WIDTH);
            MetaInformation meta = new HighwayMetaInfo();
            var rootSegment = new RoadSegment<MetaInformation>(0, rootRoad, meta);
            priQueue.Add(rootSegment);

            rootRoad = new Road(
                centre3,
                new Vector3(centre.x - Config.HIGHWAY_DEFAULT_LENGTH, 0, centre.y),
                Config.HIGHWAY_DEFAULT_WIDTH);
            meta = new HighwayMetaInfo();
            rootSegment = new RoadSegment<MetaInformation>(0, rootRoad, meta);
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
                    if (!localConstraints(ref minSeg))
                    {
                        if (minSeg.discarded)
                        {
                            foreach (var road in minSeg.roads)
                            {
                                map.deleteRoad(road);
                            }
                        }
                        continue;
                    }
                }
                
                // It's valid, so add it to list. It is now part of the final result
                map.insertRoad(minSeg.getLastRoad());

                // produce potential segments leading off this road according to some global goal
                List<RoadSegment<MetaInformation>> pendingSegs = null;
                if (globalGoals != null && !minSeg.successionBlocked)
                {
                    globalGoals(minSeg, out pendingSegs);

                    foreach (RoadSegment<MetaInformation> seg in pendingSegs)
                    {
                        if (seg.metaInformation.Type.Equals("Highway"))
                        {
                            seg.timeDelay += minSeg.timeDelay;
                        }
                        else if (seg.metaInformation.Type.Equals("Street"))
                        {
                            seg.timeDelay += minSeg.timeDelay + priQueue.Count;
                        }
                        priQueue.Add(seg);
                    }
                }

                ++roadCount;
                if (roadCount >= Config.ROAD_COUNT_PER_FRAME)
                {
                    roadCount = 0;
                    yield return null;
                }
            }

            Debug.Log("Road counts: " + map.Roads.Count);
            Debug.Log("Junction counts: " + map.Junctions.Count);
            debug_drawBlocks = true;

            yield return StartCoroutine(findBlocks());
        }

        #region Road Generation
        #region Local Constraint
        /// <summary>
        /// General local constraint
        /// </summary>
        /// <param name="seg"></param>
        /// <returns>if this road can be accepted</returns>
        protected bool generalLocalConstraint(
            ref RoadSegment<MetaInformation> seg)
        {
            // 0.1 Out of bounds.
            if (!withinRange(seg.getLastRoad()))
            {
                seg.growthBlocked = true;
                seg.successionBlocked = true;
                seg.discarded = false;
                return false;
            }
            
            // 1. If a candidate segment crosses another segment 
            // then join the roads together to form a T-Junction.
            bool originalIntersectWithAnotherRoad =
                makeTJunction(ref seg);

            if (originalIntersectWithAnotherRoad)
            {
                seg.growthBlocked = true;
                seg.successionBlocked = true;
                seg.discarded = isDiscarded(seg);
                return !seg.discarded;
            }

            // 2. If a candidate segment stops near an existing T-Junction 
            // then extend it to join the junction and form a cross-junction.
            bool originalCloseToACrossing =
                makeCrossJunction(ref seg);

            if (originalCloseToACrossing)
            {
                seg.growthBlocked = true;
                seg.successionBlocked = true;
                seg.discarded = isDiscarded(seg);
                return !seg.discarded;
            }
            
            // 3. If a candidate segment stops near to another segment
            // then extend it to join the roads and form a T-Junction.
            var extendedSeg =
                new RoadSegment<MetaInformation>(seg);
            extendedSeg.updateLastRoad(
                extendedSeg.getLastRoad().stretch(Config.DETECTIVE_RADIUS_FROM_ENDS));

            bool originalCloseToARoad =
                makeTJunction(ref extendedSeg);

            if (originalCloseToARoad)
            {
                seg = extendedSeg;
                seg.growthBlocked = true;
                seg.successionBlocked = true;
                seg.discarded = isDiscarded(seg);
                return !seg.discarded;
            }
            
            // 4.1 If segment grows beyond the fixed length
            if (seg.metaInformation.Type.Equals("Highway") &&
                seg.TotalLength >= Config.HIGHWAY_SEGMENT_MAX_LENGTH)
            {
                seg.growthBlocked = true;
                seg.successionBlocked = false;
                seg.discarded = false;
                return true;
            }

            // 4.2 Accept this segment without any changes.
            seg.growthBlocked = false;
            seg.successionBlocked = false;
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

            // to find the nearest intersection
            Vector3? intersection;
            var nearestSeg = pendingRoads
                .Select(road =>
                {
                    if (road.isIntersectingWith(originalRoad, out intersection)
                    && intersection.HasValue)
                    {
                        Road proposedRoad = new Road(originalRoad.start, intersection.Value, originalRoad.width);
                        float distance = proposedRoad.Length;
                        return new IntersectionInfo(
                            distance, intersection.Value, proposedRoad, road);
                    }
                    return null;
                })
                .Min();

            // there's no intersection
            if (nearestSeg == null)
            {
                return false;
            }

            // Distance equals to zero which means 
            // there's another road has connected with the end.
            if (nearestSeg.distance == 0f)
            {
                seg.tooShortJudgment = false;
                seg.deleteLastRoad();
                return true;
            }

            // restructuring the roads
            List<Road> newRoads = nearestSeg.intersectedRoad.split(nearestSeg.proposedRoad.end);
            newRoads.Add(nearestSeg.proposedRoad);
            foreach (Road road in newRoads)
            {
                map.insertRoad(road);
            }
            map.deleteRoad(nearestSeg.intersectedRoad, false);

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

            // There's no junction near the end.
            if (closest == null)
            {
                return false;
            }

            // A new road connected to closest junction.
            var newRoad = new Road(originalRoad.start, closest, originalRoad.width);
            
            // Roads which maybe have intersections.
            var intersectiveRoads = map.Roads.Intersects(newRoad.Bound);

            // Finding out if overlapped.
            if (intersectiveRoads
                .Any(road => road.getAngleWith(newRoad) == 0f))
            {
                seg.tooShortJudgment = false;
                seg.deleteLastRoad();
                return true;
            }

            // There's no overlapping, then accept the new road.
            seg.updateLastRoad(newRoad);
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

        // Original solution 2:
        // Detecting the angles among the connected roads
        // before making a too short judgment.
        private bool isDiscarded(RoadSegment<MetaInformation> seg)
        {
            var lastRoad = seg.getLastRoad();
            var firstRoad = seg.getFirstRoad();

            // Get the junctions of road segment in map.
            var startJunction = seg.getStart();
            var endJunction = seg.getEnd();

            if (startJunction != null && seg.tooShortJudgment)
            {
                var angles =
                    startJunction.Roads
                    .Where(road => !road.Equals(firstRoad))
                    .Select(road => road.getAngleWith(firstRoad));

                // Road grows from another segment.
                if (angles.Any(angle =>
                    angle >= 180 - Config.SMALLEST_DEGREE_BETWEEN_TWO_ROADS))
                {
                    seg.tooShortJudgment = false;
                }
            }

            if (endJunction != null)
            {
                // Get every angle among the connected roads.
                var angles =
                    endJunction.Roads
                    .Where(road => !road.Equals(lastRoad))
                    .Select(road => lastRoad.getAngleWith(road));

                // The angle between two certain roads is too small.
                if (angles.Any(angle => angle < Config.SMALLEST_DEGREE_BETWEEN_TWO_ROADS))
                {
                    seg.discarded = true;
                }
            }

            return seg.tooShortJudgment ? 
                seg.TotalLength <= Config.SHORTEST_ROAD_LENGTH : seg.discarded;
            //return false;
        }
        #endregion

        #region Global Goals
        protected bool makeCandidatesByPopulationDensity(
            RoadSegment<MetaInformation> approvedSeg,
            out List<RoadSegment<MetaInformation>> potentialSegs)
        {
            potentialSegs = new List<RoadSegment<MetaInformation>>();

            if (approvedSeg.metaInformation.Type.Equals("Highway"))
            {
                makeHighwaysByPopulationDensity(approvedSeg, potentialSegs);
            }
            else if (approvedSeg.metaInformation.Type.Equals("Street"))
            {
                makeStreets(approvedSeg, potentialSegs);
            }

            return potentialSegs.Count > 0;
        }

        protected bool makeHighwaysByPopulationDensity(
            RoadSegment<MetaInformation> approvedSeg,
            List<RoadSegment<MetaInformation>> potentialSegs)
        {
            // Vaialble declarations
            bool highwayBranchAppeared = false;
            var pendingBranchSegs = new List<RoadSegment<MetaInformation>>();
            float branchDigreeDiff = 0f;

            // If segment grows beyond the fixed length,
            // several branches will appear.
            if (approvedSeg.TotalLength >= Config.HIGHWAY_SEGMENT_MAX_LENGTH)
            {
                highwayBranchAppeared = true;
            }

            // growth segment will grow along last segment.
            var pendingGrowthSegs = new List<RoadSegment<MetaInformation>>();
            float growthDigreeDiff = Config.HIGHWAY_GROWTH_MAX_DEGREE - Config.HIGHWAY_GROWTH_MIN_DEGREE;
            // branch segment will make a branch from the end.
            if (highwayBranchAppeared)
            {
                branchDigreeDiff = Config.HIGHWAY_BRANCH_MAX_DEGREE - Config.HIGHWAY_BRANCH_MIN_DEGREE;
            }
            else
            {
                branchDigreeDiff = Config.STREET_BRANCH_MAX_DEGREE - Config.STREET_BRANCH_MIN_DEGREE;
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
                var potentialRoadEnd = approvedRoad.end.position +
                    rotation * approvedRoad.Direction.normalized * Config.HIGHWAY_DEFAULT_LENGTH;

                // create new road
                var potentialRoad = new Road(approvedRoad.end, potentialRoadEnd, Config.HIGHWAY_DEFAULT_WIDTH);
                var metaInfo = new HighwayMetaInfo();
                metaInfo.populationDensity = perlin.getValue(potentialRoadEnd.x, potentialRoadEnd.z);
                var potentialSeg = new RoadSegment<MetaInformation>(0, potentialRoad, metaInfo);
                pendingGrowthSegs.Add(potentialSeg);

                if (highwayBranchAppeared)
                {
                    // get a branch digree randomly
                    float branchDigree = Random.Range(-branchDigreeDiff, branchDigreeDiff);
                    branchDigree += (branchDigree > 0) ?
                        Config.HIGHWAY_BRANCH_MIN_DEGREE : -Config.HIGHWAY_BRANCH_MIN_DEGREE;

                    // figure out the end point of new branch road
                    rotation = Quaternion.Euler(0, branchDigree, 0);
                    potentialRoadEnd = approvedRoad.end.position +
                        rotation * approvedRoad.Direction.normalized * Config.HIGHWAY_DEFAULT_LENGTH;

                    // create new branch road
                    potentialRoad = new Road(approvedRoad.end, potentialRoadEnd, Config.HIGHWAY_DEFAULT_WIDTH);
                    metaInfo = new HighwayMetaInfo();
                    metaInfo.populationDensity = perlin.getValue(potentialRoadEnd.x, potentialRoadEnd.z);
                    potentialSeg = new RoadSegment<MetaInformation>(0, potentialRoad, metaInfo);
                    pendingBranchSegs.Add(potentialSeg);
                }
                else
                {
                    // get a branch digree randomly
                    float branchDigree = Random.Range(-branchDigreeDiff, branchDigreeDiff);
                    branchDigree += (branchDigree > 0) ?
                        Config.STREET_BRANCH_MIN_DEGREE : -Config.STREET_BRANCH_MIN_DEGREE;

                    // figure out the end point of new branch road
                    rotation = Quaternion.Euler(0, branchDigree, 0);
                    potentialRoadEnd = approvedRoad.end.position +
                        rotation * approvedRoad.Direction.normalized * Config.STREET_DEFAULT_LENGTH;

                    // create new branch road
                    // appears where people live only
                    var populationDensity = perlin.getValue(potentialRoadEnd.x, potentialRoadEnd.z);
                    if (populationDensity > Config.MIN_POPULATION_DENSITY_VALUE + 0.15f)
                    {
                        potentialRoad = new Road(approvedRoad.end, potentialRoadEnd, Config.STREET_DEFAULT_WIDTH);
                        var streetMetaInfo = new StreetMetaInfo();
                        streetMetaInfo.populationDensity = populationDensity;
                        potentialSeg = new RoadSegment<MetaInformation>(0, potentialRoad, streetMetaInfo);
                        pendingBranchSegs.Add(potentialSeg);
                    }
                }
            }

            // pick out the road where has the most population density
            var maxDensityGrowthRoad =
                pendingGrowthSegs
                .OrderByDescending(x => ((HighwayMetaInfo)x.metaInformation).populationDensity)
                .FirstOrDefault();

            if (highwayBranchAppeared)
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
                var maxDensityBranchRoad =
                pendingBranchSegs
                .OrderByDescending(x => ((StreetMetaInfo)x.metaInformation).populationDensity)
                .Take(1);

                // segment grows
                approvedSeg.grow(maxDensityGrowthRoad.getLastRoad());
                approvedSeg.metaInformation = maxDensityGrowthRoad.metaInformation;

                potentialSegs.Add(approvedSeg);

                // add the street
                potentialSegs.AddRange(maxDensityBranchRoad);
            }

            return potentialSegs.Count > 0;
        }

        protected bool makeStreets(
           RoadSegment<MetaInformation> approvedSeg,
           List<RoadSegment<MetaInformation>> potentialSegs)
        {
            // Vaialble declarations
            bool branchAppeared = false;
            var pendingBranchSegs = new List<RoadSegment<MetaInformation>>();
            float branchDigreeDiff = 0f;
            Road approvedRoad = approvedSeg.getLastRoad();

            // If segment grows beyond the fixed length,
            // several branches will appear.
            if (approvedSeg.TotalLength >= Config.STREET_SEGMENT_MAX_LENGTH)
            {
                branchAppeared = true;
            }

            // growth segment will grow along last segment.
            var pendingGrowthSegs = new List<RoadSegment<MetaInformation>>();
            float growthDigreeDiff = Config.STREET_GROWTH_MAX_DEGREE - Config.STREET_GROWTH_MIN_DEGREE;
            // branch segment will make a branch from the end.
            if (branchAppeared)
            {
                branchDigreeDiff = Config.STREET_BRANCH_MAX_DEGREE - Config.STREET_BRANCH_MIN_DEGREE;
            }

            for (int i = 0; i < 4; ++i)
            {
                // get a growth digree randomly
                float growthDigree = Random.Range(-growthDigreeDiff, growthDigreeDiff);
                growthDigree += (growthDigree > 0) ?
                    Config.STREET_GROWTH_MIN_DEGREE : -Config.STREET_GROWTH_MIN_DEGREE;

                // figure out the end point of new road
                var rotation = Quaternion.Euler(0, growthDigree, 0);
                var potentialRoadEnd = approvedRoad.end.position +
                    rotation * approvedRoad.Direction.normalized * Config.STREET_DEFAULT_LENGTH;

                // create new road
                var potentialRoad = new Road(approvedRoad.end, potentialRoadEnd, Config.STREET_DEFAULT_WIDTH);
                var metaInfo = new StreetMetaInfo();
                metaInfo.populationDensity = perlin.getValue(potentialRoadEnd.x, potentialRoadEnd.z);
                var potentialSeg = new RoadSegment<MetaInformation>(0, potentialRoad, metaInfo);
                pendingGrowthSegs.Add(potentialSeg);

                if (branchAppeared)
                {
                    // get a branch digree randomly
                    float branchDigree = Random.Range(-branchDigreeDiff, branchDigreeDiff);
                    branchDigree += (branchDigree > 0) ?
                        Config.STREET_BRANCH_MIN_DEGREE : -Config.STREET_BRANCH_MIN_DEGREE;

                    // Original solution 1:
                    // While a branch is being generated, it will appear at
                    // the centre of whole segment. This is to avoid those 
                    // which are too close to other roads.
                    var branchStart =
                        approvedSeg.roads[approvedSeg.roads.Count / 2].start;

                    // figure out the end point of new branch road
                    rotation = Quaternion.Euler(0, branchDigree, 0);
                    potentialRoadEnd = branchStart.position +
                        rotation * approvedRoad.Direction.normalized * Config.STREET_DEFAULT_LENGTH;

                    // create new branch road
                    // appears where people live only
                    var populationDensity = perlin.getValue(potentialRoadEnd.x, potentialRoadEnd.z);
                    if (populationDensity > Config.MIN_STREET_APPEAR_POPULATION_DENSITY_VALUE)
                    {
                        potentialRoad = new Road(branchStart, potentialRoadEnd, Config.STREET_DEFAULT_WIDTH);
                        var streetMetaInfo = new StreetMetaInfo();
                        streetMetaInfo.populationDensity = populationDensity;
                        potentialSeg = new RoadSegment<MetaInformation>(0, potentialRoad, streetMetaInfo);
                        pendingBranchSegs.Add(potentialSeg);
                    }
                }
            }

            // pick out the road where has the most population density
            var maxDensityGrowthRoad =
                pendingGrowthSegs
                .OrderByDescending(x => ((StreetMetaInfo)x.metaInformation).populationDensity)
                .FirstOrDefault();

            if (branchAppeared)
            {
                var maxDensityBranchRoad =
                pendingBranchSegs
                .OrderByDescending(x => ((StreetMetaInfo)x.metaInformation).populationDensity)
                .Take(1);

                // add the street
                potentialSegs.AddRange(maxDensityBranchRoad);

                // as for growth road, add it to result directly
                if (maxDensityBranchRoad.Count() > 0)
                {
                    potentialSegs.Add(maxDensityGrowthRoad);
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
        #endregion

        #region Allotment Generation
        protected IEnumerator findBlocks()
        {
            var twowayRoadsObjects = map.twowayRoads;
            var junctionsEnumerator = map.JunctionsEnumerable.GetEnumerator();
            int junctionCount = 0;

            while (junctionsEnumerator.MoveNext())
            {
                // Current junction
                var junction = junctionsEnumerator.Current;

                var connectedRoadsEnumerator = junction.Roads.GetEnumerator();
                while (connectedRoadsEnumerator.MoveNext())
                {
                    // Current road
                    var road = connectedRoadsEnumerator.Current;
                    var block = new Block();

                    // Current junction as start position
                    var endJunction = road.start.Equals(junction) ? road.end : road.start;
                    var desiredRoad = new Road(junction, endJunction, road.width);

                    // Loop until roads form a closed block.
                    while (twowayRoadsObjects.Count > 0)
                    {
                        // The road which we want is not exist.
                        if (!twowayRoadsObjects.Remove(desiredRoad))
                        {
                            break;
                        }

                        block.addEdges(desiredRoad);

                        if (block.isClosure())
                        {
                            // Add it to global map
                            map.addBlock(block);
                            break;
                        }

                        // Get the most left turn road.
                        var leftTurnRoad = endJunction.Roads
                            .Where(item => !(item.Equals(desiredRoad) || item.Equals(desiredRoad.reverse())))
                            .OrderBy(item =>
                            {
                                Vector3 roadDir = desiredRoad.getRelativeDirection(item);
                                return Math.angle360(desiredRoad.Direction, roadDir);
                            })
                            .FirstOrDefault();

                        if (leftTurnRoad == null)
                        {
                            break;
                        }

                        var startJunction = endJunction;
                        endJunction = 
                            leftTurnRoad.start.Equals(endJunction) ? leftTurnRoad.end : leftTurnRoad.start;
                        desiredRoad =
                            new Road(startJunction, endJunction, leftTurnRoad.width);
                    }
                }

                ++junctionCount;
                if (junctionCount >= Config.JUNCTION_COUNT_TO_FIND_BLOCKS_PER_FRAME)
                {
                    junctionCount = 0;
                    yield return null;
                }
            }

            Debug.Log("Block counts: " + map.Blocks.Count);
            debug_drawRoadmap = false;
            yield return StartCoroutine(buildBuildings());
        }

        internal static float getPopulationDensityValue(Polygon polygon)
        {
            var point = polygon.Centre;
            return perlin.getValue(point.x, point.y);
        }
        #endregion

        #region Building Generation
        protected IEnumerator buildBuildings()
        {
            int buildingCount = 0;

            var blocks = map.Blocks;
            foreach (var block in blocks)
            {
                var lotsEnumerator = block.Lots.GetEnumerator();
                while (lotsEnumerator.MoveNext())
                {
                    var lot = lotsEnumerator.Current;
                    var buildingObject = Instantiate(buildingPrefab);
                    buildingObject.GetComponent<Building>().construct(lot, pool);

                    ++buildingCount;
                    if (buildingCount >= Config.BUILDING_COUNT_PER_FRAME)
                    {
                        buildingCount = 0;
                        yield return null;
                    }
                }
            }

            debug_drawBlocks = false;
            yield return null;
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
        private bool debug_drawRoadmap = true;
        private bool debug_drawBlocks = false;

        void drawDebug()
        {
            Color block_purple = new Color(0.2902f, 0f, 0.56078f);
            Color highway_red = new Color(1f, 0.32157f, 0.32157f);
            Color street_blue = new Color(0.16078f, 0.58039f, 1f);

            if (debug_drawBlocks)
            {
                var blocks = map.Blocks;
                foreach (var b in blocks)
                {
                    // Blocks:
                    /*var boundary = b.Boundary.ToList();
                    for (int i = 0; i < boundary.Count - 1; ++i)
                    {
                        Debug.DrawLine(boundary[i], boundary[i + 1], block_purple);
                    }*/

                    // Bounding Box:
                    /*var e_boundingbox = b.BoundingBox;
                    if (e_boundingbox != null)
                    {
                        var boundingbox = e_boundingbox.ToList();
                        for (int i = 0; i < boundingbox.Count; ++i)
                        {
                            int next = (i + 1) % boundingbox.Count;
                            Debug.DrawLine(boundingbox[i], boundingbox[next], Color.green);
                        }
                    }*/

                    // Lots:
                    var lots = b.Lots.ToList();
                    for (int i = 0; i < lots.Count; ++i)
                    {
                        var v = lots[i].vertices;
                        for (int j = 0; j < v.Count - 1; ++j)
                        {
                            Debug.DrawLine(v[j], v[j + 1], block_purple);
                        }
                    }
                }
            }
            
            if (debug_drawRoadmap)
            {
                var roads = map.Roads.GetEnumerator();
                while (roads.MoveNext())
                {
                    var road = roads.Current.Value;
                    if (road.width == Config.HIGHWAY_DEFAULT_WIDTH)
                    {
                        Debug.DrawLine(road.start.position, road.end.position, highway_red);
                    }
                    else if (road.width == Config.STREET_DEFAULT_WIDTH)
                    {
                        Debug.DrawLine(road.start.position, road.end.position, street_blue);
                    }
                }
            }
        }

        /*void OnDrawGizmos()
        {
            var junctions = map.Junctions.GetEnumerator();
            while (junctions.MoveNext())
            {
                var junction = junctions.Current.Value;
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(junction.position, .5f);
            }
        }*/
        #endregion
    }
}