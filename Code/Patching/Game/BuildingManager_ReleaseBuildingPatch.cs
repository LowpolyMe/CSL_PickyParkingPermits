using HarmonyLib;
using PickyParking.Features.ParkingPolicing;
namespace PickyParking.Patching.Game
{
    
    
    
    
    internal static class BuildingManager_ReleaseBuildingPatch
    {
        public static void Apply(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(global::BuildingManager), "ReleaseBuilding", new[] { typeof(ushort) });
            if (original == null) return;

            var prefix = new HarmonyMethod(typeof(BuildingManager_ReleaseBuildingPatch), nameof(Prefix));
            harmony.Patch(original, prefix: prefix);
        }

        private static void Prefix(ushort building)
        {
            BuildingReleaseCleanup.HandleReleaseBuilding(building);
        }
    }
}

