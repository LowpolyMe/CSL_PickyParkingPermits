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
                if (Log.Dev.IsEnabled(DebugLogCategory.Enforcement))
                {
                    Log.Dev.Info(DebugLogCategory.Enforcement, LogPath.Vanilla, "PatchSkippedMissingMethod", "type=PassengerCarAI | method=" + TargetMethodName + " | overload=Outer");
                }
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(PassengerCarAI_FindParkingSpaceBuildingRadiusPatch), nameof(Prefix))
            );

            if (Log.Dev.IsEnabled(DebugLogCategory.Enforcement))
            {
                Log.Dev.Info(DebugLogCategory.Enforcement, LogPath.Vanilla, "PatchApplied", "type=PassengerCarAI | method=" + TargetMethodName + " | behavior=RadiusOverride");
                Log.Dev.Info(DebugLogCategory.Enforcement, LogPath.Vanilla, "VanillaRadiusPatchApplied");
            }
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
