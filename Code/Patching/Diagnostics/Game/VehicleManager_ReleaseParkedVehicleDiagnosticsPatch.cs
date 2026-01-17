using HarmonyLib;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Logging;
using PickyParking.Settings;

namespace PickyParking.Patching.Diagnostics.Game
{
    internal static class VehicleManager_ReleaseParkedVehicleDiagnosticsPatch
    {
        private const string TargetMethodName = "ReleaseParkedVehicle";

        public static void Apply(Harmony harmony)
        {
            var method = AccessTools.Method(typeof(VehicleManager), TargetMethodName, new[] { typeof(ushort) });
            if (method == null)
            {
                if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
                    Log.Info(DebugLogCategory.Enforcement, "[Parking] ReleaseParkedVehicle not found; skipping diagnostics patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(VehicleManager_ReleaseParkedVehicleDiagnosticsPatch), nameof(Prefix))
            );

            if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
                Log.Info(DebugLogCategory.Enforcement, "[Parking] Patched ReleaseParkedVehicle (diagnostics).");
        }

        private static void Prefix(ushort parked)
        {
            ParkedVehicleRemovalLogger.LogIfNearDebugLot(parked, "VehicleManager.ReleaseParkedVehicle");
        }
    }
}
