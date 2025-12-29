using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Logging;

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
                Log.Info("[TMPE] AdvancedParkingManager not found; skipping TryMoveParkedVehicle patch.");
                return;
            }

            MethodInfo method = AccessTools.Method(type, TargetMethodName);
            if (method == null)
            {
                Log.Info("[TMPE] TryMoveParkedVehicle not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(TMPE_TryMoveParkedVehiclePatch), nameof(Prefix)),
                finalizer: new HarmonyMethod(typeof(TMPE_TryMoveParkedVehiclePatch), nameof(Finalizer))
            );

            Log.Info("[TMPE] Patched TryMoveParkedVehicle (context injection).");
        }

        private static void Prefix(MethodBase __originalMethod, object[] __args, ref bool __state)
        {
            ParkingSearchContextPatchHandler.BeginTryMoveParkedVehicle(__originalMethod, __args, ref __state);
        }

        private static Exception Finalizer(Exception __exception, bool __state)
        {
            return ParkingSearchContextPatchHandler.EndTryMoveParkedVehicle(__exception, __state);
        }
    }
}


