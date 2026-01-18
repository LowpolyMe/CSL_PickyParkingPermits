using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Logging;
using PickyParking.Settings;
using UnityEngine;

namespace PickyParking.Patching.Game
{
    internal static class PassengerCarAI_FindParkingSpaceBuildingPatch
    {
        private const string TargetMethodName = "FindParkingSpaceBuilding";

        public static void Apply(Harmony harmony)
        {
            MethodInfo method = FindTargetMethod();
            if (method == null)
            {
                Log.Info(DebugLogCategory.Enforcement, "[Vanilla] PassengerCarAI.FindParkingSpaceBuilding not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(PassengerCarAI_FindParkingSpaceBuildingPatch), nameof(Prefix))
            );

            Log.Info(DebugLogCategory.Enforcement, "[Vanilla] Patched PassengerCarAI.FindParkingSpaceBuilding (vanilla candidate filtering).");
        }

        private static MethodInfo FindTargetMethod()
        {
            Type type = typeof(PassengerCarAI);

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(method.Name, TargetMethodName, StringComparison.Ordinal))
                    continue;

                if (method.ReturnType != typeof(bool))
                    continue;

                ParameterInfo[] ps = method.GetParameters();
                if (ps.Length != 11)
                    continue;

                if (ps[0].ParameterType != typeof(bool)) continue;
                if (ps[1].ParameterType != typeof(ushort)) continue;
                if (ps[2].ParameterType != typeof(ushort)) continue;
                if (ps[3].ParameterType != typeof(ushort)) continue;
                if (!IsByRefOf(ps[4].ParameterType, typeof(Building))) continue;
                if (ps[5].ParameterType != typeof(Vector3)) continue;
                if (ps[6].ParameterType != typeof(float)) continue;
                if (ps[7].ParameterType != typeof(float)) continue;
                if (!IsByRefOf(ps[8].ParameterType, typeof(float))) continue;
                if (!IsByRefOf(ps[9].ParameterType, typeof(Vector3))) continue;
                if (!IsByRefOf(ps[10].ParameterType, typeof(Quaternion))) continue;

                return method;
            }

            return null;
        }

        private static bool IsByRefOf(Type maybeByRef, Type elementType)
        {
            if (!maybeByRef.IsByRef) return false;
            return maybeByRef.GetElementType() == elementType;
        }

        private static bool Prefix([HarmonyArgument(3)] ushort buildingId, ref bool __result)
        {
            return VanillaCandidateSearchFilter.ShouldRunOriginal(buildingId, ref __result);
        }
    }
}
