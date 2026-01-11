using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Features.Debug;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Logging;
using UnityEngine;

namespace PickyParking.Patching.Diagnostics.Game
{
    internal static class VehicleManager_CreateParkedVehicleDiagnosticsPatch
    {
        private const string TargetMethodName = "CreateParkedVehicle";

        public static void Apply(Harmony harmony)
        {
            MethodInfo method = FindTargetMethod();
            if (method == null)
            {
                if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
                    Log.Info("[Parking] CreateParkedVehicle not found; skipping diagnostics patch.");
                return;
            }

            harmony.Patch(
                method,
                postfix: new HarmonyMethod(typeof(VehicleManager_CreateParkedVehicleDiagnosticsPatch), nameof(Postfix))
            );

            if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
                Log.Info("[Parking] Patched CreateParkedVehicle (diagnostics).");
        }

        private static MethodInfo FindTargetMethod()
        {
            Type type = typeof(VehicleManager);

            foreach (var m in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(m.Name, TargetMethodName, StringComparison.Ordinal))
                    continue;

                var ps = m.GetParameters();
                if (ps.Length != 6)
                    continue;

                if (!IsByRefOf(ps[0].ParameterType, typeof(ushort))) continue;
                if (!IsByRefOf(ps[1].ParameterType, typeof(ColossalFramework.Math.Randomizer))) continue;
                if (ps[2].ParameterType != typeof(VehicleInfo)) continue;
                if (ps[3].ParameterType != typeof(Vector3)) continue;
                if (ps[4].ParameterType != typeof(Quaternion)) continue;
                if (ps[5].ParameterType != typeof(uint)) continue;

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
            ushort parked,
            VehicleInfo info,
            Vector3 position,
            uint ownerCitizen)
        {
            if (!Log.IsEnforcementDebugEnabled || !Log.IsVerboseEnabled)
                return;

            if (!__result)
            {
                if (!ParkingSearchContext.HasContext)
                    return;

                string failedPrefabName = info != null ? info.name : "UNKNOWN";
                string failedSource = ParkingSearchContext.Source ?? "NULL";

                Log.Info(
                    "[Parking] CreateParkedVehicle failed " +
                    $"prefab={failedPrefabName} ownerCitizen={ownerCitizen} pos=({position.x:F1},{position.y:F1},{position.z:F1}) " +
                    $"source={failedSource} vehicleId={ParkingSearchContext.VehicleId} citizenId={ParkingSearchContext.CitizenId}"
                );
                return;
            }

            if (parked == 0)
                return;

            if (!ParkingCandidateBlocker.TryGetRuleBuildingAtPosition(position, out ushort buildingId))
                return;

            if (!ParkingDebugSettings.IsBuildingDebugEnabled(buildingId))
                return;

            string prefabName = info != null ? info.name : "UNKNOWN";
            string source = ParkingSearchContext.Source ?? "NULL";

            Log.Info(
                "[Parking] CreateParkedVehicle created " +
                $"buildingId={buildingId} parkedId={parked} prefab={prefabName} ownerCitizen={ownerCitizen} " +
                $"pos=({position.x:F1},{position.y:F1},{position.z:F1}) " +
                $"source={source} vehicleId={ParkingSearchContext.VehicleId} citizenId={ParkingSearchContext.CitizenId}"
            );
        }
    }
}

