using HarmonyLib;
using PickyParking.Features.Debug;
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
                if (Log.Dev.IsEnabled(DebugLogCategory.Enforcement))
                {
                    Log.Dev.Info(DebugLogCategory.Enforcement, LogPath.Any, "DiagnosticsSkippedMissingMethod", "type=VehicleManager | method=" + TargetMethodName);
                }
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(VehicleManager_ReleaseParkedVehicleDiagnosticsPatch), nameof(Prefix))
            );

            if (Log.Dev.IsEnabled(DebugLogCategory.Enforcement))
            {
                Log.Dev.Info(DebugLogCategory.Enforcement, LogPath.Any, "DiagnosticsPatchApplied", "type=VehicleManager | method=" + TargetMethodName);
            }
        }

        private static void Prefix(ushort parked)
        {
            ParkedVehicleRemovalLogger.LogIfNearDebugLot(parked, "VehicleManager.ReleaseParkedVehicle");
        }
    }
}
