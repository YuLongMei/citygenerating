using CityGen.Struct;
using CityGen.Util;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityGen
{
    public class BuildingPool : MonoBehaviour
    {
        public GameObject[] buildingPrefabs;
        private List<Transform> buildings;

        // Use this for initialization
        void Start()
        {
            buildings = new List<Transform>();
            integrate();
        }

        protected bool integrate()
        {
            if (buildingPrefabs.Length <= 0)
            {
                return false;
            }

            foreach (var obj in buildingPrefabs)
            {
                if (obj == null)
                {
                    continue;
                }

                var prefab = obj.transform;
                var childCount = prefab.childCount;

                // This prefab is a collection.
                if (childCount > 0)
                {
                    foreach (Transform childPrefab in prefab)
                    {
                        extractModel(childPrefab);
                    }
                }
                // Single prefab without child.
                else
                {
                    extractModel(prefab);
                }
            }

            return true;
        }

        protected void calculateModelInformation(Transform model)
        {
            var mesh = model.GetComponent<MeshFilter>().sharedMesh;

            if (mesh == null)
            {
                return;
            }

            var meshSize = mesh.bounds.size;
            var meshScale = model.lossyScale;

            var realSize = Vector3.Scale(meshSize, meshScale);
            var info = model.GetComponent<ModelInformation>();
            info.inverseRotation = Quaternion.Inverse(model.rotation);
            realSize = info.inverseRotation * realSize;
            info.height = Mathf.Abs(realSize.y);
            info.u = Mathf.Abs(realSize.x);
            info.v = Mathf.Abs(realSize.z);
        }

        protected void extractModel(Transform model)
        {
            calculateModelInformation(model);
            buildings.Add(model);
        }

        public Transform getModel(Building building)
        {
            var model = buildings
                .Aggregate((prefab, next) =>
                {
                    var score1 = evaluate(prefab, building);
                    var score2 = evaluate(next, building);
                    return score1 > score2 ? prefab : next;
                });
            var rotation = rotate(model, building) * model.rotation;
            return Instantiate(model, building.Centre, rotation);
        }

        protected float evaluate(Transform model, Building building)
        {
            var info = model.GetComponent<ModelInformation>();

            float modelLongerEdge, modelShorterEdge,
                footprintLongerEdge, footprintShorterEdge;
            if (info.u > info.v)
            {
                modelLongerEdge = info.u;
                modelShorterEdge = info.v;
            }
            else
            {
                modelLongerEdge = info.v;
                modelShorterEdge = info.u;
            }
            if (building.U > building.V)
            {
                footprintLongerEdge = building.U;
                footprintShorterEdge = building.V;
            }
            else
            {
                footprintLongerEdge = building.V;
                footprintShorterEdge = building.U;
            }

            float diff1 = footprintLongerEdge - modelLongerEdge;
            float diff2 = footprintShorterEdge - modelShorterEdge;
            float score = 0f;
            if (diff1 < 0f || diff2 < 0f)
            {
                return score;
            }

            score = modelLongerEdge + modelShorterEdge;

            score += 30f - Mathf.Abs(building.height - info.height);

            return score;
        }

        protected Quaternion rotate(Transform model, Building building)
        {
            var info = model.GetComponent<ModelInformation>();
            Vector3 targetDir =
                info.u > info.v ? building.ShortEdgeDir : building.LongEdgeDir;

            return Quaternion.AngleAxis(
                Math.angle360(info.inverseRotation * model.forward, targetDir), Vector3.up);
        }
    }
}
