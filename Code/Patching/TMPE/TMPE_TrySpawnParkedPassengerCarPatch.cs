using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using PickyParking.App;
using PickyParking.Infrastructure;


namespace PickyParking.Patching.TMPE
{
    internal static class TMPE_TrySpawnParkedPassengerCarPatch
    {
        private const string TargetTypeName = "TrafficManager.Manager.Impl.AdvancedParkingManager, TrafficManager";

        public static void Apply(Harmony harmony)
        {
            var type = Type.GetType(TargetTypeName, throwOnError: false);
            if (type == null)
            {
                Log.Info("[TMPE] AdvancedParkingManager not found; skipping TrySpawnParkedPassengerCar patch.");
                return;
            }

            var method = FindTrySpawnParkedPassengerCar(type);
            if (method == null)
            {
                Log.Info("[TMPE] TrySpawnParkedPassengerCar overload not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(TMPE_TrySpawnParkedPassengerCarPatch), nameof(Prefix)),
                finalizer: new HarmonyMethod(typeof(TMPE_TrySpawnParkedPassengerCarPatch), nameof(Finalizer))
            );

            Log.Info("[TMPE] Patched TrySpawnParkedPassengerCar (context push/pop).");
        }

        private static MethodInfo FindTrySpawnParkedPassengerCar(Type advancedParkingManagerType)
        {
            foreach (var m in advancedParkingManagerType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(m.Name, "TrySpawnParkedPassengerCar", StringComparison.Ordinal))
                    continue;

                var ps = m.GetParameters();
                
                
                
                if (ps.Length != 7)
                    continue;

                if (ps[0].ParameterType != typeof(uint)) continue;
                if (!IsByRefOf(ps[1].ParameterType, typeof(Citizen))) continue;
                if (ps[2].ParameterType != typeof(ushort)) continue;
                if (ps[3].ParameterType != typeof(Vector3)) continue;
                if (ps[4].ParameterType != typeof(VehicleInfo)) continue;
                if (!IsByRefOf(ps[5].ParameterType, typeof(Vector3))) continue; 
                
                if (!ps[6].ParameterType.IsByRef) continue;
                var errElem = ps[6].ParameterType.GetElementType();
                if (errElem == null || !string.Equals(errElem.Name, "ParkingError", StringComparison.Ordinal)) continue;

                return m;
            }
            return null;
        }

        private static bool IsByRefOf(Type maybeByRef, Type elementType)
        {
            if (!maybeByRef.IsByRef) return false;
            return maybeByRef.GetElementType() == elementType;
        }

        private static void Prefix([HarmonyArgument(0)] uint citizenId, ref bool __state)
        {
            ParkingSearchContextSetup.BeginTrySpawnParkedCar(citizenId, ref __state);
        }


        private static Exception Finalizer(Exception __exception, bool __state)
        {
            return ParkingSearchContextSetup.EndTrySpawnParkedCar(__exception, __state);
        }
    }
}


