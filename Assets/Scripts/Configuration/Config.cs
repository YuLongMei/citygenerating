namespace CityGen
{
    public static class Config
    {
        #region Input Simulation
        internal static readonly float PARAMAP_GRANULARITY = /*5000f;//*/800f;
        internal static readonly float MIN_POPULATION_DENSITY_VALUE = 0.5f;
        internal static readonly float MAX_POPULATION_DENSITY_VALUE = 1f;
        #endregion

        #region Road
        internal static readonly float HIGHWAY_DEFAULT_WIDTH = 15f;
        internal static readonly float HIGHWAY_DEFAULT_LENGTH = 100f;
        internal static readonly float STREET_DEFAULT_WIDTH = 4f;
        internal static readonly float STREET_DEFAULT_LENGTH = 10f;
        #endregion

        #region Road Generation
        internal static readonly int ROAD_COUNT_PER_FRAME = 500;
        internal static readonly int ROAD_COUNT_LIMIT = 1000000;
        internal static readonly float DETECTIVE_RADIUS_FROM_ENDS = .5f * STREET_DEFAULT_LENGTH;
        internal static readonly float SHORTEST_ROAD_LENGTH = 1f * STREET_DEFAULT_LENGTH;
        internal static readonly float SMALLEST_DEGREE_BETWEEN_TWO_ROADS = 45f;
        internal static readonly float HIGHWAY_GROWTH_MIN_DEGREE = 0f;
        internal static readonly float HIGHWAY_GROWTH_MAX_DEGREE = 1.2f;
        internal static readonly float HIGHWAY_BRANCH_MIN_DEGREE = 80f;
        internal static readonly float HIGHWAY_BRANCH_MAX_DEGREE = 100f;
        internal static readonly float HIGHWAY_SEGMENT_MAX_LENGTH = 600f;
        internal static readonly float MIN_STREET_APPEAR_POPULATION_DENSITY_VALUE = MIN_POPULATION_DENSITY_VALUE + .15f;
        internal static readonly float STREET_GROWTH_MIN_DEGREE = 0f;
        internal static readonly float STREET_GROWTH_MAX_DEGREE = .5f;
        internal static readonly float STREET_BRANCH_MIN_DEGREE = 88f;
        internal static readonly float STREET_BRANCH_MAX_DEGREE = 92f;
        internal static readonly float STREET_SEGMENT_MAX_LENGTH = 60f;
        #endregion

        #region Allotment Generation
        internal static readonly int JUNCTION_COUNT_TO_FIND_BLOCKS_PER_FRAME = 300;
        internal static readonly float EPSILON_TRANSLATION_FOR_OBB = .2f;
        internal static readonly float SPLITTER_LEFT_LIMIT = .35f;
        internal static readonly float SPLITTER_RIGHT_LIMIT = .65f;
        internal static readonly float LOT_AREA_BASIS = 50f;
        internal static readonly float LOT_AREA_MULTIPLE = 150f;
        internal static readonly float LOT_AREA_EXPONENT = 1.2f;
        internal static readonly float LOT_AREA_CORRECTION = 50f;
        internal static readonly float BUILDING_HEIGHT_BASIS = 2.5f;
        internal static readonly float BUILDING_HEIGHT_MULTIPLE = 3f;
        internal static readonly float BUILDING_HEIGHT_EXPONENT = 5f;
        internal static readonly float BUILDING_HEIGHT_CORRECTION = 20f;
        #endregion

        #region Other
        internal static readonly float FLOAT_DELTA = .0000001f;
        #endregion
    }
}
