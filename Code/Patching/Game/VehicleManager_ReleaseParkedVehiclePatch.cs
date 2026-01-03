using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Logging;

namespace PickyParking.Patching.Game
{
    internal static class VehicleManager_ReleaseParkedVehiclePatch
    {
        private const string TargetMethodName = "ReleaseParkedVehicle";

        public static void Apply(Harmony harmony)
        {
            MethodInfo method = AccessTools.Method(typeof(VehicleManager), TargetMethodName, new[] { typeof(ushort) });
            if (method == null)
            {
                Log.Info("[Parking] ReleaseParkedVehicle not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(VehicleManager_ReleaseParkedVehiclePatch), nameof(Prefix))
            );

            Log.Info("[Parking] Patched ReleaseParkedVehicle (parked removal logging).");
        }

        private static void Prefix(ushort parked)
        {
            ParkedVehicleRemovalLogger.LogIfNearDebugLot(parked, "VehicleManager.ReleaseParkedVehicle");
        }
    }
}
