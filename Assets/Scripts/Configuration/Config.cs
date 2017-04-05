namespace CityGen
{
    public static class Config
    {
        #region InputSimulation
        internal static readonly float PARAMAP_GRANULARITY = 800f;
        #endregion

        #region Road
        internal static readonly float HIGHWAY_DEFAULT_WIDTH = 8f;
        internal static readonly float HIGHWAY_DEFAULT_LENGTH = 50f;
        internal static readonly float STREET_DEFAULT_WIDTH = 4f;
        internal static readonly float STREET_DEFAULT_LENGTH = 10f;
        internal static readonly float HIGHWAY_GROWTH_MIN_DEGREE = 0f;
        internal static readonly float HIGHWAY_GROWTH_MAX_DEGREE = 10f;
        internal static readonly float HIGHWAY_BRANCH_MIN_DEGREE = 75f;
        internal static readonly float HIGHWAY_BRANCH_MAX_DEGREE = 105f;
        #endregion

        #region RoadGenerating
        internal static readonly int ROAD_COUNT_LIMIT = 3000;
        internal static readonly float DETECTIVE_RADIUS_FROM_ENDS = .2f * HIGHWAY_DEFAULT_WIDTH;
        #endregion

        #region Other
        internal static readonly float FLOAT_DELTA = .0000001f;
        #endregion
    }
}
