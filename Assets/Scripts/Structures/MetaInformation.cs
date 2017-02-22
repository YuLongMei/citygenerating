using System;

namespace CityGen.Struct
{
    public abstract class MetaInformation
    {
        protected string type;
        public string Type
        {
            get { return type; }
        }

        public abstract bool applyTo(ref Road road);
    }

    public class HighwayMetaInfo : MetaInformation
    {
        public HighwayMetaInfo()
        {
            type = "Highway";
        }

        internal float populationDensity;

        public override bool applyTo(ref Road road)
        {
            throw new NotImplementedException();
        }
    }
}
