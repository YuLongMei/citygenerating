using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;


public class CityGenerator : MonoBehaviour {

    public Vector2 mapSize = new Vector2(100, 100);
    private float mapLeft, mapRight, mapForward, mapBack;
    public GameObject groundObject = null;
    public GameObject roadObject = null;
    public GameObject buildingObject = null;

    public string seed = null;

    public float roadWidth;

    public float buildingMinHeight, buildingMaxHeight;
    public float buildingMinLength, buildingMaxLength;
    public float buildingSpace;

    private ObjectContainer container = new ObjectContainer();

    public enum WorkProgress
    {
        NotStarted,
        RoadmapAccomplished,
        AllotmentsAccomplished,
        BuildingsAccomplished,
    }
    private WorkProgress workProgress = WorkProgress.NotStarted;

    // Use this for initialization
    void Start() {
        if (!groundObject)
        {
            Debug.Log("There's no ground object.");
            return;
        }

        mapRight = mapSize.x * 0.5f;
        mapLeft = -mapRight;
        mapForward = mapSize.y * 0.5f;
        mapBack = -mapForward;
        GameObject ground = Instantiate(groundObject, Vector3.zero, Quaternion.identity) as GameObject;
        ground.transform.localScale =
            new Vector3(mapSize.x * groundObject.transform.localScale.x, 1, mapSize.y * groundObject.transform.localScale.x);

        initSeed();

        generateCity();
    }

    public bool isInRange(Vector3 point)
    {
        return (point.x > mapLeft || Mathf.Approximately(point.x, mapLeft)) &&
            (point.x <= mapRight || Mathf.Approximately(point.x, mapRight)) &&
            (point.z >= mapBack || Mathf.Approximately(point.z, mapBack)) &&
            (point.z <= mapForward || Mathf.Approximately(point.z, mapForward));
    }

    private void initSeed(string seed = null)
    {
        if (this.seed == "" || seed != null)
        {
            this.seed = seed;
        }
        
        if (this.seed != null)
        {
            Random.InitState(this.seed.GetHashCode());
        }
    }
	
    private void generateCity()
    {
        StopAllCoroutines();

        workProgress = WorkProgress.NotStarted;

        StartCoroutine(generateCoroutine());
    }

    private IEnumerator generateCoroutine()
    {
        while (true)
        {
            switch (workProgress)
            {
                case WorkProgress.NotStarted:
                    yield return StartCoroutine(generateGridRoadmap(instantiateRoad));
                    break;
                case WorkProgress.RoadmapAccomplished:
                    yield return StartCoroutine(generateAllotments(instantiateAllotments));
                    break;
                case WorkProgress.AllotmentsAccomplished:
                    yield return StartCoroutine(generateBuildings(instantiateBuildings));
                    break;
                case WorkProgress.BuildingsAccomplished:
                    Debug.Log("Accomplished");
                    yield break;
            }
        }
    }

    int a = 0, a_supposed = 2, a_pace = 9;
    int b = 0, b_supposed = 0, b_pace = 2;
    int c = 0, c_pace = 50;
    private IEnumerator generateRandomRoadmap(Action<RoadInfo> onInstantiateRoad)
    {
        for (int i = 0; i < a_pace; ++i)
        {

            RoadInfo road;

            if (randomRoad(out road))
            {
                onInstantiateRoad(road);
            }
            else
            {
                continue;
            }

            ++a;
            Debug.Log("Roadmap generated " + a);

            if (a >= a_supposed)
            {
                workProgress = WorkProgress.RoadmapAccomplished;
                break;
            }
        }

        yield return null;
    }

    private RoadInfo datumRoad = null;
    private int gridRoadmapProgress = 0;
    private Vector3 gridRoadmapGeneratedDirection = Vector3.zero;
    private IEnumerator generateGridRoadmap(Action<RoadInfo> onInstantiateRoad)
    {
        for (int i = 0; i < a_pace; ++i)
        {
            // there's no datum road, then generate one.
            if (container.roads.Count == 0)
            {
                if (randomRoad(out datumRoad))
                {
                    onInstantiateRoad(datumRoad);
                }
                else
                {
                    continue;
                }
            }
            else if (container.roads.Count == 1)
            {
                float pointDistance = Random.Range(0f, datumRoad.Length);
                Vector3 point = datumRoad.LineSegment.GetPoint(pointDistance);
                StraightLine line = new StraightLine(point, Vector3.Cross(datumRoad.Direction, Vector3.up));
                onInstantiateRoad(generateRoadByLine(line));
            }
            // if exist one, then use it
            else
            {
                if (gridRoadmapGeneratedDirection == Vector3.zero)
                {
                    gridRoadmapGeneratedDirection = Vector3.Cross(datumRoad.Direction, Vector3.up).normalized;
                }

                float roadInterval = roadWidth * Random.Range(10f, 15f);
                Vector3 translation = roadInterval * gridRoadmapGeneratedDirection;

                RoadInfo offsetRoad = generateRoadByTranslation(datumRoad, translation);

                if (offsetRoad != null)
                {
                    onInstantiateRoad(offsetRoad);
                    datumRoad = offsetRoad;
                }
                else
                {
                    switch (gridRoadmapProgress)
                    {
                        case 0:
                            datumRoad = container.roads[0].road;
                            gridRoadmapGeneratedDirection = -gridRoadmapGeneratedDirection;
                            break;
                        case 1:
                            datumRoad = container.roads[1].road;
                            gridRoadmapGeneratedDirection = Vector3.zero;
                            break;
                        case 2:
                            datumRoad = container.roads[1].road;
                            gridRoadmapGeneratedDirection = -gridRoadmapGeneratedDirection;
                            break;
                        case 3:
                        default:
                            workProgress = WorkProgress.RoadmapAccomplished;
                            yield return null;
                            break;
                    }

                    ++gridRoadmapProgress;
                    continue;
                }
            }

            ++a;
            Debug.Log("Roadmap generated " + a);
        }

        yield return null;
    }

    private void instantiateRoad(RoadInfo road)
    {
        Debug.Log("Generating a Road");
        GameObject roadEntity = Instantiate(roadObject, road.Position, road.rotation) as GameObject;
        roadEntity.transform.localScale = 
            new Vector3(road.Length, roadObject.transform.localScale.y, road.width);
        container.roads.Add(new RoadItem(road, roadEntity));
        return;
    }

    private IEnumerator generateAllotments(Action<GameObject> onInstantiateAllotment)
    {
        for (int i = 0; i < b_pace; ++i)
        {
            ++b;
            onInstantiateAllotment(new GameObject());
            Debug.Log("Allotments generated " + b);

            if (b >= b_supposed)
            {
                workProgress = WorkProgress.AllotmentsAccomplished;
                break;
            }
        }
       
        yield return null;
    }

    private void instantiateAllotments(GameObject go)
    {
        Debug.Log("Generating a Allotment");
        return;
    }

    private int roadCount = 0;
    private bool anotherSide = false;
    private IEnumerator generateBuildings(Action<BuildingInfo> onInstantiateBuilding)
    {
        for (int i = 0; i < c_pace; ++i)
        {
            if (roadCount >= container.roads.Count)
            {
                workProgress = WorkProgress.BuildingsAccomplished;
                break;
            }

            RoadInfo road = container.roads[roadCount].road;
            BuildingInfo building;
            randomBuilding(road, out building, anotherSide);
            
            if (building != null)
            {
                onInstantiateBuilding(building);
            }
            else
            {
                if (buildingOccupiedLength == 0f)
                {
                    anotherSide = !anotherSide;
                    if (anotherSide == false)
                    {
                        ++roadCount;
                    }
                }
                continue;
            }

            ++c;
            Debug.Log("Building generated " + c);
        }

        yield return null;
    }

    private void instantiateBuildings(BuildingInfo building)
    {
        Debug.Log("Generating a Building");

        Vector3 scale = new Vector3(building.length, building.height, building.width);

        // eliminate overlapped building
        Collider[] colliders = Physics.OverlapBox(building.position, scale * 0.5f, building.rotation, ~LayerMask.GetMask("Ignore Raycast"));
        if (colliders.Length > 0)
        {
            return;
        }

        GameObject buildingEntity = Instantiate(buildingObject, building.position, building.rotation) as GameObject;
        buildingEntity.transform.localScale = scale;
        container.buildings.Add(new BuildingItem(building, buildingEntity));
        return;
    }

    /// <summary>
    /// random a straight road by random its path line
    /// </summary>
    /// <param name="road"></param>
    /// <returns></returns>
    private bool randomRoad(out RoadInfo road)
    {
        Vector3 roadPassedPoint = new Vector3(Random.Range(mapLeft, mapRight), 0, Random.Range(mapBack, mapForward));
        Vector2 roadDirection = Random.insideUnitCircle.normalized;

        StraightLine line = new StraightLine(roadPassedPoint, new Vector3(roadDirection.x, roadPassedPoint.y, roadDirection.y));
        road = generateRoadByLine(line);

        return road != null;
    }

    private RoadInfo generateRoadByTranslation(RoadInfo road, Vector3 translation)
    {
        StraightLine line = new StraightLine(road.tail + translation, road.Direction);
        return generateRoadByLine(line);
    }

    private RoadInfo generateRoadByLine(StraightLine line)
    {
        List<Vector3> intersections = new List<Vector3>();
        Vector3? intersection = line.getIntersectionWithX(mapLeft);
        Debug.Log(intersection.Value);
        if (intersection != null && isInRange(intersection.Value))
        {
            intersections.Add(intersection.Value);
        }
        intersection = line.getIntersectionWithX(mapRight);
        Debug.Log(intersection.Value);
        if (intersection != null && isInRange(intersection.Value))
        {
            intersections.Add(intersection.Value);
        }
        intersection = line.getIntersectionWithZ(mapForward);
        Debug.Log(intersection.Value);
        if (intersection != null && isInRange(intersection.Value))
        {
            intersections.Add(intersection.Value);
        }
        intersection = line.getIntersectionWithZ(mapBack);
        Debug.Log(intersection.Value);
        if (intersection != null && isInRange(intersection.Value))
        {
            intersections.Add(intersection.Value);
        }

        if (intersections.Count == 2)
        {
            RoadInfo r = new RoadInfo();

            r.head = intersections[0];
            r.tail = intersections[1];
            r.width = roadWidth;
            r.rotation = line.RotationIn2D;

            return r;
        }
        else
        {
            return null;
        }
    }

    private float buildingOccupiedLength = 0f;
    private bool randomBuilding(RoadInfo road, out BuildingInfo building, bool anotherSide = false)
    {
        float roadLength = road.Length;
        if (buildingOccupiedLength >= roadLength)
        {
            buildingOccupiedLength = 0f;
            building = null;
            return false;
        }

        Vector3 offsetDir = Quaternion.Euler(0, 90, 0) * road.Direction.normalized;
        if (anotherSide)
        {
            offsetDir = -offsetDir;
        }

        BuildingInfo b = new BuildingInfo();
        float maxWidth1 = float.MaxValue;
        float maxWidth2 = float.MaxValue;

        // random building parameter 1
        float maxLength = Mathf.Min(buildingMaxLength, roadLength - buildingOccupiedLength);

        if (buildingMinLength <= maxLength)
        {
            b.length =
                Random.Range(buildingMinLength, maxLength);

            // detect if there're any obstacles
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(road.LineSegment.GetPoint(buildingOccupiedLength), offsetDir, out hit))
            {
                maxWidth1 = hit.distance;
            }
            if (Physics.Raycast(road.LineSegment.GetPoint(buildingOccupiedLength + b.length), offsetDir, out hit))
            {
                maxWidth2 = hit.distance;
            }

            // random building parameter 2
            float minWidth = 0.5f * b.length;
            float maxWidth = Mathf.Min(2f * b.length, maxWidth1, maxWidth2);

            if (minWidth <= maxWidth)
            {
                b.width =
                    Random.Range(minWidth, maxWidth);
                b.height =
                    Random.Range(buildingMinHeight, buildingMaxHeight);

                // calculate the position
                b.position =
                    road.LineSegment.GetPoint(buildingOccupiedLength + 0.5f * b.length) +
                    (road.width + b.width + 0.01f) * 0.5f * offsetDir;

                b.rotation = road.rotation;

                // for next time
                buildingOccupiedLength += b.length + buildingSpace;

                building = b;
                return true;
            }
            else
            {
                buildingOccupiedLength += b.length + buildingSpace;
            }
           
        }
        else
        {
            // for next time
            buildingOccupiedLength = 0f;
        }

        building = null;
        return false;
    }
}

