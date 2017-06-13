using CityGen.Util;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityGen.Struct
{
    public class Building : MonoBehaviour
    {
        public float height;
        private Material material;
        protected Polygon footprint;

        void Start()
        {
            material = GetComponent<MeshRenderer>().material;
            footprint = null;
        }

        internal float U
        {
            get
            {
                return footprint == null ?
                    0f : footprint.boundingBox.U;
            }
        }

        internal float V
        {
            get
            {
                return footprint == null ?
                    0f : footprint.boundingBox.V;
            }
        }

        internal Vector3 Centre
        {
            get
            {
                return footprint == null ?
                    Vector3.zero : footprint.boundingBox.Centre;
            }
        }

        internal Vector3 ShortEdgeDir
        {
            get
            {
                return footprint == null ?
                    Vector3.zero : footprint.boundingBox.ShortEdgeDir;
            }
        }

        internal Vector3 LongEdgeDir
        {
            get
            {
                return footprint == null ?
                    Vector3.zero : footprint.boundingBox.LongEdgeDir;
            }
        }

        protected bool canBeConstructed(Polygon footprint)
        {
            return footprint.Area >= Config.MIN_AREA_FOR_BUILDING &&
                footprint.boundingBox.AspectRatio < Config.MAX_ASPECT_RATIO;
        }

        public bool construct(Polygon footprint, BuildingPool pool)
        {
            if (!canBeConstructed(footprint))
            {
                return false;
            }

            this.footprint = footprint;
            // Height of the building.
            height = buildingHeight(footprint.Area, footprint.getCentrePopulationDensity());

            switch (Config.BUILDING_GENERATING_MODE)
            {
                case Config.BuildingGeneratingMode.Procedural:
                    constructProcedurally();
                    break;
                case Config.BuildingGeneratingMode.ReadyMade:
                    buildUsingReadyMadeModel(pool);
                    break;
                default:
                    break;
            }

            return true;
        }

        protected void constructProcedurally()
        {
            var verticesCount = footprint.vertices.Count - 1;
            var vertices = footprint.vertices.GetRange(0, verticesCount);

            int layers = 1;// (int)(height / Config.BUILDING_LAYER_HEIGHT);

            System.Func<Vector3, Vector2> V3_to_V2 =
                v3 => new Vector2(v3.x, v3.z);

            // uv
            Vector2 uv_00 = Vector2.zero;
            Vector2 uv_10 = new Vector2(1f, 0f);
            Vector2 uv_01 = new Vector2(0f, 1f * layers);
            Vector2 uv_11 = new Vector2(1f, 1f * layers);

            // Roof
            Triangulator tris = new Triangulator(
                vertices
                .Select(V3_to_V2));
            var indices = tris.Triangulate();
            var mesh_triangles = new List<int>(indices);
            var mesh_vertices = new List<Vector3>(vertices
                .Select(v => new Vector3(v.x, height, v.z)));
            var mesh_uv = new List<Vector2>(mesh_vertices
                .Select(tri => uv_00));

            // Bottom
            var indices_bottom = new List<int>(indices);
            indices_bottom.Reverse();
            mesh_triangles.AddRange(indices_bottom
                .Select(index => correspondingVertexIndex(verticesCount, index)));
            mesh_vertices.AddRange(vertices);
            mesh_uv.AddRange(mesh_uv);

            // Sides
            for (int p = verticesCount - 1, q = 0; q < verticesCount; p = q++)
            {
                var r = correspondingVertexIndex(verticesCount, p);
                var s = correspondingVertexIndex(verticesCount, q);
                var count = 2 * verticesCount + q * 6;
                mesh_vertices.AddRange(
                    new Vector3[]
                    {
                        mesh_vertices[r], mesh_vertices[p], mesh_vertices[q],
                        mesh_vertices[r], mesh_vertices[q], mesh_vertices[s],
                    });
                mesh_triangles.AddRange(
                    new int[]
                    {
                        count, count + 1, count + 2,
                        count + 3, count + 4, count + 5
                    });
                mesh_uv.AddRange(
                    new Vector2[]
                    {
                        uv_00, uv_01, uv_11,
                        uv_00, uv_11, uv_10
                    });
            }

            // Meshes of the building.
            Mesh mesh = new Mesh();
            mesh.vertices = mesh_vertices.ToArray();
            mesh.triangles = mesh_triangles.ToArray();
            mesh.uv = mesh_uv.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Set up game object with mesh.
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            gameObject.GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        protected void buildUsingReadyMadeModel(BuildingPool pool)
        {
            var model = pool.getModel(this);
            model.parent = transform;
        }

        protected float buildingHeight(float area, float populationDensity)
        {
            populationDensity = (populationDensity - Config.MIN_STREET_APPEAR_POPULATION_DENSITY_VALUE) /
                (Config.MAX_POPULATION_DENSITY_VALUE - Config.MIN_STREET_APPEAR_POPULATION_DENSITY_VALUE);
            populationDensity = Mathf.Max(0f, populationDensity);

            var lowerBound = Config.MIN_RESIDENTAL_DISTRICT_BUILDING_HEIGHT +
                Mathf.Pow(
                    Config.MIN_COMMERCIAL_DISTRICT_BUILDING_HEIGHT - Config.MIN_RESIDENTAL_DISTRICT_BUILDING_HEIGHT,
                    populationDensity);
            var upperBound = Config.MAX_RESIDENTAL_DISTRICT_BUILDING_HEIGHT +
                Mathf.Pow(
                    Config.MAX_COMMERCIAL_DISTRICT_BUILDING_HEIGHT - Config.MAX_RESIDENTAL_DISTRICT_BUILDING_HEIGHT,
                    populationDensity);

            var height = Mathf.Lerp(lowerBound, upperBound, Random.value);

            return height;
        }

        protected int correspondingVertexIndex(int count, int index)
        {
            return index + count;
        }
    }
}
