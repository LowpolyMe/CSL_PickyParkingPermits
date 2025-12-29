using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using PickyParking.App;
using PickyParking.Infrastructure;

namespace PickyParking.Patching.Game
{
    
    
    
    
    internal static class VehicleManager_CreateParkedVehiclePatch
    {
        private const string TargetMethodName = "CreateParkedVehicle";
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
                prefix: new HarmonyMethod(typeof(VehicleManager_CreateParkedVehiclePatch), nameof(Prefix))
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
            return ParkingCandidateBlocker.HandleCreateParkedVehiclePrefix(
                ref parked,
                ref r,
                info,
                position,
                rotation,
                ownerCitizen,
                ref __result);
        }
    }
}



