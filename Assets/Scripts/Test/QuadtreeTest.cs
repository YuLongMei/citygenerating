using CityGen.Struct;
using CityGen.Util;
using UnityEngine;

namespace CityGen.Test
{
    public class QuadtreeTest : MonoBehaviour
    {
        Quadtree<Junction> junctions;

        // Use this for initialization
        void Start()
        {
            Vector2 minMapPostion = new Vector2(-1000, -1000);
            Vector2 maxMapPostion = new Vector2(1000, 1000);
            Rect r = new Rect(minMapPostion, maxMapPostion - minMapPostion);

            junctions = new Quadtree<Junction>(r, 63, 8);

            Junction j = new Junction(1, 0, 1);
            junctions.Insert(j.Bound, j);
        }

        // Update is called once per frame
        void Update()
        {
            Junction j = new Junction(1, 0, 1);
            var foundJ = junctions.Intersects(j.Bound);

            Debug.Log(j.Bound);
            Debug.Log(j.Bound.Contains(new Vector2(1, 1)));

            foreach (var jo in foundJ)
            {
                jo.add(new Road(Vector3.back, Vector3.forward, 1f));
                Debug.Log(jo.RoadsCount);
            }
        }
    }
}

