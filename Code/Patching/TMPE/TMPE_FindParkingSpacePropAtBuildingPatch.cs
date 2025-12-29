using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Infrastructure;
using PickyParking.Patching;

namespace PickyParking.Patching.TMPE
{
    
    
    
    
    
    
    
    internal static class TMPE_FindParkingSpacePropAtBuildingPatch
    {
        private const string TargetTypeName = "TrafficManager.Manager.Impl.AdvancedParkingManager, TrafficManager";
        private const string TargetMethodName = "FindParkingSpacePropAtBuilding";

        public static void Apply(Harmony harmony)
        {
            Type type = Type.GetType(TargetTypeName, throwOnError: false);
            if (type == null)
            {
                Log.Info("[TMPE] AdvancedParkingManager not found; skipping FindParkingSpacePropAtBuilding patch.");
                return;
            }

            MethodInfo method = AccessTools.Method(type, TargetMethodName);
            if (method == null)
            {
                Log.Info("[TMPE] FindParkingSpacePropAtBuilding not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(TMPE_FindParkingSpacePropAtBuildingPatch), nameof(Prefix))
            );

            Log.Info("[TMPE] Patched FindParkingSpacePropAtBuilding (enforcement).");
        }

        private static bool Prefix(ref bool __result, object[] __args)
        {
            return ParkingCandidateBlockerAdapter.HandleFindParkingSpacePropAtBuildingPrefix(ref __result, __args);
        }
    }
}



