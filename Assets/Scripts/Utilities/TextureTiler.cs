using UnityEngine;

namespace CityGen.Util
{
    public class TextureTiler : MonoBehaviour
    {

        public Vector2 mainTexFactor = Vector2.one;
        public Vector2 secTexScale = Vector2.one;

        private Renderer _renderer;

        // Use this for initialization
        void Start()
        {
            _renderer = GetComponent<Renderer>();

            tile();
        }

        // Update is called once per framea
        void Update()
        {
            //tile();
        }

        protected void tile()
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            var scale = transform.lossyScale;
            mpb.SetVector("_MainTex_ST", new Vector4(scale.x * mainTexFactor.x, scale.z * mainTexFactor.y));
            mpb.SetVector("_DetailAlbedoMap_ST", new Vector4(1f, scale.z * secTexScale.y));
            _renderer.SetPropertyBlock(mpb);
        }
    }
}
