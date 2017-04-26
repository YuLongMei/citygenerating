namespace CityGen
{
    public static class Config
    {
        #region InputSimulation
        internal static readonly float PARAMAP_GRANULARITY = 800f;
        internal static readonly float MIN_POPULATION_DENSITY_VALUE = 0.5f;
        internal static readonly float MAX_POPULATION_DENSITY_VALUE = 1f;
        #endregion

        #region Road
        internal static readonly float HIGHWAY_DEFAULT_WIDTH = 8f;
        internal static readonly float HIGHWAY_DEFAULT_LENGTH = 40f;
        internal static readonly float STREET_DEFAULT_WIDTH = 4f;
        internal static readonly float STREET_DEFAULT_LENGTH = 15f;
        #endregion

        #region RoadGenerating
        internal static readonly int ROAD_COUNT_PER_FRAME = 50;
        internal static readonly int ROAD_COUNT_LIMIT = 100000;
        internal static readonly float DETECTIVE_RADIUS_FROM_ENDS = .5f * STREET_DEFAULT_LENGTH;
        internal static readonly float SHORTEST_ROAD_LENGTH = .9f * STREET_DEFAULT_LENGTH;
        internal static readonly float SMALLEST_DEGREE_BETWEEN_TWO_ROADS = 30f;
        internal static readonly float HIGHWAY_GROWTH_MIN_DEGREE = 0f;
        internal static readonly float HIGHWAY_GROWTH_MAX_DEGREE = 1.2f;
        internal static readonly float HIGHWAY_BRANCH_MIN_DEGREE = 80f;
        internal static readonly float HIGHWAY_BRANCH_MAX_DEGREE = 100f;
        internal static readonly float HIGHWAY_SEGMENT_MAX_LENGTH = 600f;
        internal static readonly float STREET_GROWTH_MIN_DEGREE = 0f;
        internal static readonly float STREET_GROWTH_MAX_DEGREE = .5f;
        internal static readonly float STREET_BRANCH_MIN_DEGREE = 88f;
        internal static readonly float STREET_BRANCH_MAX_DEGREE = 92f;
        internal static readonly float STREET_SEGMENT_MAX_LENGTH = 40f;
        #endregion

        #region Other
        internal static readonly float FLOAT_DELTA = .0000001f;
        #endregion
    }
}
