namespace PickyParking.Features.Debug
{
    public static class ParkingDebugSettings
    {
        public static bool EnableLotInspectionLogs;
        public static bool DisableTMPECandidateBlocking;
        public static bool DisableClearKnownParkingOnDenied = false;
        public static bool DisableParkingEnforcement;
        public static ushort BuildingDebugId;

        public static bool IsBuildingDebugEnabled(ushort buildingId)
        {
            return EnableLotInspectionLogs && BuildingDebugId != 0 && buildingId == BuildingDebugId;
        }
    }
}
