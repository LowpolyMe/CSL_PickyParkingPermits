namespace PickyParking.Features.Debug
{
    public static class ParkingDebugSettings
    {
        public static bool EnableLotInspectionLogs;
        public static bool DisableTMPECandidateBlocking;
        public static bool DisableClearKnownParkingOnDenied = false;
        public static bool DisableParkingEnforcement;
        public static ushort SelectedBuildingId;

        public static bool IsSelectedBuilding(ushort buildingId)
        {
            return SelectedBuildingId != 0 && buildingId == SelectedBuildingId;
        }
    }
}
