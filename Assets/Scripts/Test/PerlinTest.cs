using UnityEngine;
using System.Collections;

namespace CityGen.Test
{
    public class PerlinTest : MonoBehaviour
    {
        public int pixWidth;
        public int pixHeight;
        public float xOrg;
        public float yOrg;
        public float scale = 1.0F;
        private Perlin p;
        private Renderer rend;
        void Start()
        {
            rend = GetComponent<Renderer>();
            p = new Perlin(pixWidth, pixHeight);
            rend.material.mainTexture = p.texture;
        }

        void Update()
        {
            p.makeNoises(xOrg, yOrg, scale);
        }
    }
}