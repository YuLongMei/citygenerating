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

        internal float populationDensity;

        public abstract bool applyTo(ref Road road);
    }

    public class HighwayMetaInfo : MetaInformation
    {
        public HighwayMetaInfo()
        {
            type = "Highway";
        }

        public override bool applyTo(ref Road road)
        {
            throw new NotImplementedException();
        }
    }

    public class StreetMetaInfo : MetaInformation
    {
        public StreetMetaInfo()
        {
            type = "Street";
        }

        public override bool applyTo(ref Road road)
        {
            throw new NotImplementedException();
        }
    }
}
