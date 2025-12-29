using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Infrastructure;

namespace PickyParking.Patching.TMPE
{
    
    
    
    
    
    
    
    
    internal static class TMPE_ParkPassengerCarPatch
    {
        private const string TargetTypeName = "TrafficManager.Manager.Impl.VehicleBehaviorManager, TrafficManager";
        private const string TargetMethodName = "ParkPassengerCar";

        public static void Apply(Harmony harmony)
        {
            Type type = Type.GetType(TargetTypeName, throwOnError: false);
            if (type == null)
            {
                Log.Info("[TMPE] VehicleBehaviorManager not found; skipping ParkPassengerCar patch.");
                return;
            }

            MethodInfo method = AccessTools.Method(type, TargetMethodName);
            if (method == null)
            {
                Log.Info("[TMPE] ParkPassengerCar not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(TMPE_ParkPassengerCarPatch), nameof(Prefix)),
                finalizer: new HarmonyMethod(typeof(TMPE_ParkPassengerCarPatch), nameof(Finalizer))
            );

            Log.Info("[TMPE] Patched ParkPassengerCar (context injection).");
        }

        private static void Prefix(MethodBase __originalMethod, object[] __args, ref bool __state)
        {
            ParkingSearchContextSetupAdapter.BeginParkPassengerCar(__originalMethod, __args, ref __state);
        }

        private static Exception Finalizer(Exception __exception, bool __state)
        {
            return ParkingSearchContextSetupAdapter.EndParkPassengerCar(__exception, __state);
        }
    }
}


