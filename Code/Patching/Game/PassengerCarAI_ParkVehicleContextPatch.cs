using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Logging;
using PickyParking.Settings;

namespace PickyParking.Patching.Game
{
    internal static class PassengerCarAI_ParkVehicleContextPatch
    {
        private const string TargetMethodName = "ParkVehicle";

        public static void Apply(Harmony harmony)
        {
            MethodInfo method = FindTargetMethod();
            if (method == null)
            {
                Log.Info(DebugLogCategory.Enforcement, "[Vanilla] PassengerCarAI.ParkVehicle not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(PassengerCarAI_ParkVehicleContextPatch), nameof(Prefix)),
                finalizer: new HarmonyMethod(typeof(PassengerCarAI_ParkVehicleContextPatch), nameof(Finalizer))
            );

            Log.Info(DebugLogCategory.Enforcement, "[Vanilla] Patched PassengerCarAI.ParkVehicle (vanilla context injection).");
        }

        private static MethodInfo FindTargetMethod()
        {
            Type type = typeof(PassengerCarAI);

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(method.Name, TargetMethodName, StringComparison.Ordinal))
                    continue;

                ParameterInfo[] ps = method.GetParameters();
                if (ps.Length != 6)
                    continue;

                if (ps[0].ParameterType != typeof(ushort)) continue;
                if (!IsByRefOf(ps[1].ParameterType, typeof(Vehicle))) continue;
                if (ps[2].ParameterType != typeof(PathUnit.Position)) continue;
                if (ps[3].ParameterType != typeof(uint)) continue;
                if (ps[4].ParameterType != typeof(int)) continue;
                if (!IsByRefOf(ps[5].ParameterType, typeof(byte))) continue;

                return method;
            }

            return null;
        }

        private static bool IsByRefOf(Type maybeByRef, Type elementType)
        {
            if (!maybeByRef.IsByRef) return false;
            return maybeByRef.GetElementType() == elementType;
        }

        private static void Prefix([HarmonyArgument(0)] ushort vehicleId, ref bool __state)
        {
            VanillaParkingContextInjector.BeginParkVehicle(vehicleId, ref __state);
        }

        private static Exception Finalizer(Exception __exception, bool __state)
        {
            return VanillaParkingContextInjector.EndParkVehicle(__exception, __state);
        }
    }
}
