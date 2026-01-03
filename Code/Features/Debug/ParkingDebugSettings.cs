namespace PickyParking.Features.Debug
{
    public static class ParkingDebugSettings
    {
        public static bool EnableGameAccessLogs;
        public static bool EnableCreateParkedVehicleLogs;
        public static bool EnableBuildingDebugLogs;
        public static ushort BuildingDebugId;

        public static bool IsBuildingDebugEnabled(ushort buildingId)
        {
            return EnableBuildingDebugLogs && BuildingDebugId != 0 && buildingId == BuildingDebugId;
        }
    }
}
