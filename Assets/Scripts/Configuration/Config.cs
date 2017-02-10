namespace CityGen
{
    public static class Config
    {
        #region Road
        internal static float HIGHWAY_DEFAULT_WIDTH = 8f;
        internal static float HIGHWAY_DEFAULT_LENGTH = 20f;
        internal static float STREET_DEFAULT_WIDTH = 4f;
        internal static float STREET_DEFAULT_LENGTH = 10f;
        internal static float HIGHWAY_GROWTH_MIN_DEGREE = 30f;
        internal static float HIGHWAY_GROWTH_MAX_DEGREE = 100f;
        #endregion

        #region RoadGenerating
        internal static int ROAD_COUNT_LIMIT = 3000;
        internal static float DETECTIVE_RADIUS_FROM_ENDS = .5f;
        #endregion

        #region Other
        internal static float FLOAT_DELTA = .0000001f;
        #endregion
    }
}
