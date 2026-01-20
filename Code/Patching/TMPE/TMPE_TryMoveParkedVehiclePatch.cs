using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Features.Debug;
using PickyParking.Logging;
using PickyParking.Settings;

namespace PickyParking.Patching.TMPE
{
    internal static class TMPE_TryMoveParkedVehiclePatch
    {
        private const string TargetTypeName = "TrafficManager.Manager.Impl.AdvancedParkingManager, TrafficManager";
        private const string TargetMethodName = "TryMoveParkedVehicle";

        public static void Apply(Harmony harmony)
        {
            Type type = Type.GetType(TargetTypeName, throwOnError: false);
            if (type == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                {
                    Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "PatchSkippedMissingType", "type=AdvancedParkingManager");
                }
                return;
            }

            MethodInfo method = AccessTools.Method(type, TargetMethodName);
            if (method == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                {
                    Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "PatchSkippedMissingMethod", "type=AdvancedParkingManager | method=" + TargetMethodName);
                }
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(TMPE_TryMoveParkedVehiclePatch), nameof(Prefix)),
                finalizer: new HarmonyMethod(typeof(TMPE_TryMoveParkedVehiclePatch), nameof(Finalizer))
            );

            if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
            {
                Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "PatchApplied", "type=AdvancedParkingManager | method=" + TargetMethodName + " | behavior=ContextInjection");
            }
        }

        private static void Prefix([HarmonyArgument(1)] ref VehicleParked parkedVehicle, ref bool __state)
        {
            ParkingSearchContextPatchHandler.BeginTryMoveParkedVehicle(ref parkedVehicle, ref __state);
        }

        private static Exception Finalizer(Exception __exception, bool __state)
        {
            return ParkingSearchContextPatchHandler.EndTryMoveParkedVehicle(__exception, __state);
        }
    }
}


