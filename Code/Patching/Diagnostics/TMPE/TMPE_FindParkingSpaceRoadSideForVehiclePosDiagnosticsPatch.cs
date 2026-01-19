using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Features.Debug;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Logging;
using UnityEngine;
using PickyParking.Settings;

namespace PickyParking.Patching.Diagnostics.TMPE
{
    internal static class TMPE_FindParkingSpaceRoadSideForVehiclePosDiagnosticsPatch
    {
        private const string TargetTypeName = "TrafficManager.Manager.Impl.AdvancedParkingManager, TrafficManager";
        private const string TargetMethodName = "FindParkingSpaceRoadSideForVehiclePos";

        public static void Apply(Harmony harmony)
        {
            Type type = Type.GetType(TargetTypeName, throwOnError: false);
            if (type == null)
            {
                if (Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled)
                    Log.Info(DebugLogCategory.Tmpe, "[TMPE] AdvancedParkingManager not found; skipping FindParkingSpaceRoadSideForVehiclePos diagnostics patch.");
                return;
            }

            MethodInfo method = FindTargetMethod(type);
            if (method == null)
            {
                if (Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled)
                    Log.Info(DebugLogCategory.Tmpe, "[TMPE] FindParkingSpaceRoadSideForVehiclePos overload not found; skipping diagnostics patch.");
                return;
            }

            harmony.Patch(
                method,
                postfix: new HarmonyMethod(typeof(TMPE_FindParkingSpaceRoadSideForVehiclePosDiagnosticsPatch), nameof(Postfix))
            );

            if (Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled)
                Log.Info(DebugLogCategory.Tmpe, "[TMPE] Patched FindParkingSpaceRoadSideForVehiclePos (diagnostics).");
        }

        private static MethodInfo FindTargetMethod(Type advancedParkingManagerType)
        {
            foreach (var m in advancedParkingManagerType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(m.Name, TargetMethodName, StringComparison.Ordinal))
                    continue;

                var ps = m.GetParameters();
                if (ps.Length != 9)
                    continue;

                if (ps[0].ParameterType != typeof(VehicleInfo)) continue;
                if (ps[1].ParameterType != typeof(ushort)) continue;
                if (ps[2].ParameterType != typeof(ushort)) continue;
                if (ps[3].ParameterType != typeof(Vector3)) continue;
                if (!IsByRefOf(ps[4].ParameterType, typeof(Vector3))) continue;
                if (!IsByRefOf(ps[5].ParameterType, typeof(Quaternion))) continue;
                if (!IsByRefOf(ps[6].ParameterType, typeof(float))) continue;
                if (!IsByRefOf(ps[7].ParameterType, typeof(uint))) continue;
                if (!IsByRefOf(ps[8].ParameterType, typeof(int))) continue;

                return m;
            }

            return null;
        }

        private static bool IsByRefOf(Type maybeByRef, Type elementType)
        {
            if (!maybeByRef.IsByRef) return false;
            return maybeByRef.GetElementType() == elementType;
        }

        private static void Postfix(
            bool __result,
            [HarmonyArgument(0)] VehicleInfo vehicleInfo,
            [HarmonyArgument(2)] ushort segmentId,
            [HarmonyArgument(3)] Vector3 refPos)
        {
            if (__result)
                return;

            if (!Log.IsVerboseEnabled || !Log.IsTmpeDebugEnabled)
                return;

            if (!ParkingSearchContext.HasContext)
                return;

            string prefabName = vehicleInfo != null ? vehicleInfo.name : "UNKNOWN";

            Log.Info(DebugLogCategory.Tmpe,
                "[TMPE] FindParkingSpaceRoadSideForVehiclePos failed " +
                $"segmentId={segmentId} refPos=({refPos.x:F1},{refPos.y:F1},{refPos.z:F1}) " +
                $"vehiclePrefab={prefabName} vehicleId={ParkingSearchContext.VehicleId} " +
                $"citizenId={ParkingSearchContext.CitizenId} source={ParkingSearchContext.Source ?? "NULL"}"
            );
        }
    }
}
