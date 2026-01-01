using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Features.ParkingPolicing;
using UnityEngine;
using PickyParking.Logging;
using PickyParking.Patching;

namespace PickyParking.Patching.Game
{
    
    
    
    
    internal static class VehicleManager_CreateParkedVehiclePatch
    {
        private const string TargetMethodName = "CreateParkedVehicle";
        private const ushort DebugIndustry1BuildingId = 27392;
        private const ushort DebugIndustry2BuildingId = 40969;
        internal static bool EnableCreateParkedVehicleLogs = false;
        public static void Apply(Harmony harmony)
        {
            MethodInfo method = FindTargetMethod();
            if (method == null)
            {
                Log.Info("[Parking] CreateParkedVehicle not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(VehicleManager_CreateParkedVehiclePatch), nameof(Prefix)),
                postfix: new HarmonyMethod(typeof(VehicleManager_CreateParkedVehiclePatch), nameof(Postfix))
            );

            Log.Info("[Parking] Patched CreateParkedVehicle (parking violation logging).");
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

        private static bool Prefix(
            ref ushort parked,
            ref ColossalFramework.Math.Randomizer r,
            VehicleInfo info,
            Vector3 position,
            Quaternion rotation,
            uint ownerCitizen,
            ref bool __result)
        {
            return ParkingCandidateBlockerPatchHandler.HandleCreateParkedVehiclePrefix(
                ref parked,
                ref r,
                info,
                position,
                rotation,
                ownerCitizen,
                ref __result);
        }

        private static void Postfix(
            bool __result,
            ushort parked,
            VehicleInfo info,
            Vector3 position,
            uint ownerCitizen)
        {
            if (!EnableCreateParkedVehicleLogs || !__result || parked == 0 || !Log.IsVerboseEnabled)
                return;

            if (!ParkingCandidateBlocker.TryGetRuleBuildingAtPosition(position, out ushort buildingId))
                return;

            if (buildingId != DebugIndustry1BuildingId && buildingId != DebugIndustry2BuildingId)
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



