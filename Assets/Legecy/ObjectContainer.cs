using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoadItem
{
    public RoadInfo road;
    public GameObject roadObject;

    public RoadItem(RoadInfo road, GameObject roadObject)
    {
        this.road = road;
        this.roadObject = roadObject;
    }
}

public class BuildingItem
{
    public BuildingInfo building;
    public GameObject buildingObject;

    public BuildingItem(BuildingInfo building, GameObject buildingObject)
    {
        this.building = building;
        this.buildingObject = buildingObject;
    }
}

public class ObjectContainer {

    public List<RoadItem> roads = new List<RoadItem>();
    public List<BuildingItem> buildings = new List<BuildingItem>();
    
}
