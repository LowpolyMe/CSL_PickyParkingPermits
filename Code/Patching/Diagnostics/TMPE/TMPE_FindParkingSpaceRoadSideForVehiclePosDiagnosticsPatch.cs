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
                if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                {
                    Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "DiagnosticsSkippedMissingType", "type=AdvancedParkingManager");
                }
                return;
            }

            MethodInfo method = FindTargetMethod(type);
            if (method == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                {
                    Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "DiagnosticsSkippedMissingMethod", "type=AdvancedParkingManager | method=" + TargetMethodName);
                }
                return;
            }

            harmony.Patch(
                method,
                postfix: new HarmonyMethod(typeof(TMPE_FindParkingSpaceRoadSideForVehiclePosDiagnosticsPatch), nameof(Postfix))
            );

            if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
            {
                Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "DiagnosticsPatchApplied", "method=" + TargetMethodName);
            }
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

            if (!Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                return;

            if (!ParkingSearchContext.HasContext)
                return;

            string prefabName = vehicleInfo != null ? vehicleInfo.name : "UNKNOWN";

            Log.Dev.Info(
                DebugLogCategory.Tmpe,
                LogPath.TMPE,
                "FindParkingSpaceRoadSideForVehiclePosFailed",
                "segmentId=" + segmentId +
                " | refPosX=" + refPos.x.ToString("F1") +
                " | refPosY=" + refPos.y.ToString("F1") +
                " | refPosZ=" + refPos.z.ToString("F1") +
                " | vehiclePrefab=" + prefabName +
                " | vehicleId=" + ParkingSearchContext.VehicleId +
                " | citizenId=" + ParkingSearchContext.CitizenId +
                " | source=" + (ParkingSearchContext.Source ?? "NULL"));
        }
    }
}
