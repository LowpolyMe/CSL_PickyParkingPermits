using PickyParking.App;

namespace PickyParking.Patching.Game
{
    internal static class BuildingReleaseCleanupAdapter
    {
        public static void HandleReleaseBuilding(ushort buildingId)
        {
            BuildingReleaseCleanup.HandleReleaseBuilding(buildingId);
        }
    }
}
