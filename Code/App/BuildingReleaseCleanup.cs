namespace PickyParking.App
{
    
    
    
    public static class BuildingReleaseCleanup
    {
        public static void HandleReleaseBuilding(ushort buildingId)
        {
            var context = ParkingRuntimeContext.GetCurrentOrLog("BuildingReleaseCleanup.HandleReleaseBuilding");
            if (context == null || context.ParkingRestrictionsConfigRegistry == null)
                return;

            context.ParkingRestrictionsConfigRegistry.Remove(buildingId);
        }
    }
}
