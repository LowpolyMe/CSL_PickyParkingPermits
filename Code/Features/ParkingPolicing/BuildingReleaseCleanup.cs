using PickyParking.Features.ParkingPolicing.Runtime;

namespace PickyParking.Features.ParkingPolicing
{
    public static class BuildingReleaseCleanup
    {
        public static void HandleReleaseBuilding(ushort buildingId)
        {
            var context = ParkingRuntimeContext.GetCurrentOrLog("BuildingReleaseCleanup.HandleReleaseBuilding");
            if (context == null || context.ParkingRulesConfigRegistry == null)
                return;

            context.ParkingRulesConfigRegistry.Remove(buildingId);
        }
    }
}
