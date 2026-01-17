using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Logging;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Features.Debug;
using UnityEngine;
using PickyParking.Settings;

namespace PickyParking.Patching.TMPE
{
    internal static class TMPE_FindParkingSpacePropAtBuildingPatch
    {
        private const string TargetTypeName = "TrafficManager.Manager.Impl.AdvancedParkingManager, TrafficManager";
        private const string TargetMethodName = "FindParkingSpacePropAtBuilding";
        [ThreadStatic] private static bool _suppressDiagnostics;

        public static void Apply(Harmony harmony)
        {
            var type = System.Type.GetType(TargetTypeName, throwOnError: false);
            if (type == null)
            {
                Log.Info(DebugLogCategory.Tmpe, "[TMPE] AdvancedParkingManager not found; skipping FindParkingSpacePropAtBuilding patch.");
                return;
            }

            MethodInfo method = AccessTools.Method(type, TargetMethodName);
            if (method == null)
            {
                Log.Info(DebugLogCategory.Tmpe, "[TMPE] FindParkingSpacePropAtBuilding not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(TMPE_FindParkingSpacePropAtBuildingPatch), nameof(Prefix))
            );

            Log.Info(DebugLogCategory.Tmpe, "[TMPE] Patched FindParkingSpacePropAtBuilding (enforcement).");
        }

        internal static bool ConsumeSuppressDiagnostics()
        {
            bool value = _suppressDiagnostics;
            _suppressDiagnostics = false;
            return value;
        }

        private static bool Prefix(
            ref bool __result,
            [HarmonyArgument(0)] VehicleInfo vehicleInfo,
            [HarmonyArgument(3)] ushort buildingId,
            [HarmonyArgument(9)] ref Vector3 parkPos,
            [HarmonyArgument(10)] ref Quaternion parkRot,
            [HarmonyArgument(11)] ref float parkOffset)
        {
            _suppressDiagnostics = false;
            if (ParkingDebugSettings.DisableTMPECandidateBlocking)
                return true;
            bool callOriginal = ParkingCandidateBlockerPatchHandler.HandleFindParkingSpacePropAtBuildingPrefix(
                vehicleInfo,
                buildingId,
                ref parkPos,
                ref parkRot,
                ref parkOffset,
                ref __result);
            if (!callOriginal)
                _suppressDiagnostics = true;
            return callOriginal;
        }
    }
}


