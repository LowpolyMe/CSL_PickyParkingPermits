using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Logging;
using UnityEngine;

namespace PickyParking.Patching.TMPE
{
    internal static class TMPE_FindParkingSpaceForCitizenPatch
    {
        private const string TargetTypeName = "TrafficManager.Manager.Impl.AdvancedParkingManager, TrafficManager";
        private const string TargetMethodName = "FindParkingSpaceForCitizen";

        public static void Apply(Harmony harmony)
        {
            var type = Type.GetType(TargetTypeName, throwOnError: false);
            if (type == null)
            {
                Log.Error("[TMPE] AdvancedParkingManager not found; skipping FindParkingSpaceForCitizen patch.");
                return;
            }

            MethodInfo method = FindTargetMethod(type);
            if (method == null)
            {
                Log.Error("[TMPE] FindParkingSpaceForCitizen overload not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(TMPE_FindParkingSpaceForCitizenPatch), nameof(Prefix)),
                finalizer: new HarmonyMethod(typeof(TMPE_FindParkingSpaceForCitizenPatch), nameof(Finalizer))
            );

            Log.Info("[TMPE] Patched FindParkingSpaceForCitizen (context injection).");
        }

        private static MethodInfo FindTargetMethod(Type advancedParkingManagerType)
        {
            foreach (var m in advancedParkingManagerType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(m.Name, TargetMethodName, StringComparison.Ordinal))
                    continue;

                var ps = m.GetParameters();
                if (ps.Length != 11)
                    continue;

                if (ps[0].ParameterType != typeof(Vector3)) continue;
                if (ps[1].ParameterType != typeof(VehicleInfo)) continue;
                if (!IsByRefOf(ps[2].ParameterType, typeof(CitizenInstance))) continue;

                if (!ps[3].ParameterType.IsByRef) continue;
                var extElem = ps[3].ParameterType.GetElementType();
                if (extElem == null || !string.Equals(extElem.Name, "ExtCitizenInstance", StringComparison.Ordinal)) continue;

                if (ps[4].ParameterType != typeof(ushort)) continue;
                if (ps[5].ParameterType != typeof(bool)) continue;
                if (ps[6].ParameterType != typeof(ushort)) continue;
                if (ps[7].ParameterType != typeof(bool)) continue;

                if (!IsByRefOf(ps[8].ParameterType, typeof(Vector3))) continue;
                if (!IsByRefOf(ps[9].ParameterType, typeof(PathUnit.Position))) continue;
                if (!IsByRefOf(ps[10].ParameterType, typeof(bool))) continue;

                return m;
            }

            return null;
        }

        private static bool IsByRefOf(Type maybeByRef, Type elementType)
        {
            if (!maybeByRef.IsByRef) return false;
            var elem = maybeByRef.GetElementType();
            return elem == elementType;
        }

        private static void Prefix(
            [HarmonyArgument(2)] ref CitizenInstance driverInstance,
            [HarmonyArgument(6)] ushort vehicleId,
            ref bool __state)
        {
            ParkingSearchContextPatchHandler.BeginFindParkingForCitizen(ref driverInstance, vehicleId, ref __state);
        }

        private static Exception Finalizer(Exception __exception, bool __state)
        {
            return ParkingSearchContextPatchHandler.EndFindParkingForCitizen(__exception, __state);
        }
    }
}
