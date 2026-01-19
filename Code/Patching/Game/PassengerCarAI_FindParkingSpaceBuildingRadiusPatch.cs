using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Features.Debug;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Logging;
using UnityEngine;

namespace PickyParking.Patching.Game
{
    internal static class PassengerCarAI_FindParkingSpaceBuildingRadiusPatch
    {
        private const string TargetMethodName = "FindParkingSpaceBuilding";

        public static void Apply(Harmony harmony)
        {
            MethodInfo method = FindTargetMethod();
            if (method == null)
            {
                Log.Info(DebugLogCategory.Enforcement, "[Vanilla] PassengerCarAI.FindParkingSpaceBuilding (outer) not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(PassengerCarAI_FindParkingSpaceBuildingRadiusPatch), nameof(Prefix))
            );

            Log.Info(DebugLogCategory.Enforcement, "[Vanilla] Patched PassengerCarAI.FindParkingSpaceBuilding (vanilla radius override).");
            if (Log.IsVerboseEnabled)
                Log.Info(DebugLogCategory.None, "[Vanilla] Radius patch applied.");
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
                if (ps.Length != 9)
                    continue;

                if (ps[0].ParameterType != typeof(bool)) continue;
                if (ps[1].ParameterType != typeof(ushort)) continue;
                if (ps[2].ParameterType != typeof(ushort)) continue;
                if (ps[3].ParameterType != typeof(Vector3)) continue;
                if (ps[4].ParameterType != typeof(float)) continue;
                if (ps[5].ParameterType != typeof(float)) continue;
                if (ps[6].ParameterType != typeof(float)) continue;
                if (!IsByRefOf(ps[7].ParameterType, typeof(Vector3))) continue;
                if (!IsByRefOf(ps[8].ParameterType, typeof(Quaternion))) continue;

                return method;
            }

            return null;
        }

        private static bool IsByRefOf(Type maybeByRef, Type elementType)
        {
            if (!maybeByRef.IsByRef) return false;
            return maybeByRef.GetElementType() == elementType;
        }

        private static void Prefix([HarmonyArgument(2)] ushort ignoreParked, [HarmonyArgument(6)] ref float maxDistance)
        {
            VanillaSearchRadiusOverride.ApplyPrefix(ref maxDistance, ignoreParked);
        }
    }
}
