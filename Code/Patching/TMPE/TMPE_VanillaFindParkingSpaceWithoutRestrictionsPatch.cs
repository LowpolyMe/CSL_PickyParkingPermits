using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Features.Debug;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Logging;
using UnityEngine;

namespace PickyParking.Patching.TMPE
{
    internal static class TMPE_VanillaFindParkingSpaceWithoutRestrictionsPatch
    {
        private const string TargetTypeName = "TrafficManager.Manager.Impl.AdvancedParkingManager, TrafficManager";
        private const string TargetMethodName = "VanillaFindParkingSpaceWithoutRestrictions";

        public static void Apply(Harmony harmony)
        {
            var type = Type.GetType(TargetTypeName, throwOnError: false);
            if (type == null)
            {
                Log.Info("[TMPE] AdvancedParkingManager not found; skipping VanillaFindParkingSpaceWithoutRestrictions patch.");
                return;
            }

            MethodInfo method = FindTargetMethod(type);
            if (method == null)
            {
                Log.Info("[TMPE] VanillaFindParkingSpaceWithoutRestrictions not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                postfix: new HarmonyMethod(typeof(TMPE_VanillaFindParkingSpaceWithoutRestrictionsPatch), nameof(Postfix))
            );

            Log.Info("[TMPE] Patched VanillaFindParkingSpaceWithoutRestrictions (enforcement).");
        }

        private static MethodInfo FindTargetMethod(Type advancedParkingManagerType)
        {
            foreach (var m in advancedParkingManagerType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(m.Name, TargetMethodName, StringComparison.Ordinal))
                    continue;

                var ps = m.GetParameters();
                if (ps.Length < 8)
                    continue;

                if (!IsByRefOf(ps[7].ParameterType, typeof(Vector3)))
                    continue;

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

        private static void Postfix(ref bool __result, [HarmonyArgument(7)] ref Vector3 parkPos)
        {
            if (!__result)
                return;

            if (ParkingDebugSettings.DisableTMPECandidateBlocking)
                return;

            if (!ParkingSearchContext.HasCitizenId)
                return;

            uint citizenId = ParkingSearchContext.CitizenId;
            if (citizenId == 0u)
                return;

            if (ParkingCandidateBlocker.ShouldBlockCreateParkedVehicle(citizenId, parkPos))
            {
                __result = false;
                parkPos = Vector3.zero;
                ParkingStatsCounter.IncrementVanillaFallbackFlipped();
            }
        }
    }
}
