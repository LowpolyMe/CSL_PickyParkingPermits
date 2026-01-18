using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Features.Debug;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Logging;
using PickyParking.Settings;

namespace PickyParking.Patching.Game
{
    internal static class PassengerCarAI_UpdateParkedVehicleContextPatch
    {
        private const string TargetMethodName = "UpdateParkedVehicle";

        public static void Apply(Harmony harmony)
        {
            MethodInfo method = FindTargetMethod();
            if (method == null)
            {
                Log.Info(DebugLogCategory.Enforcement, "[Vanilla] PassengerCarAI.UpdateParkedVehicle not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(PassengerCarAI_UpdateParkedVehicleContextPatch), nameof(Prefix)),
                finalizer: new HarmonyMethod(typeof(PassengerCarAI_UpdateParkedVehicleContextPatch), nameof(Finalizer))
            );

            Log.Info(DebugLogCategory.Enforcement, "[Vanilla] Patched PassengerCarAI.UpdateParkedVehicle (vanilla context injection).");
        }

        private static MethodInfo FindTargetMethod()
        {
            Type type = typeof(PassengerCarAI);

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(method.Name, TargetMethodName, StringComparison.Ordinal))
                    continue;

                ParameterInfo[] ps = method.GetParameters();
                if (ps.Length != 2)
                    continue;

                if (ps[0].ParameterType != typeof(ushort)) continue;
                if (!IsByRefOf(ps[1].ParameterType, typeof(VehicleParked))) continue;

                return method;
            }

            return null;
        }

        private static bool IsByRefOf(Type maybeByRef, Type elementType)
        {
            if (!maybeByRef.IsByRef) return false;
            return maybeByRef.GetElementType() == elementType;
        }

        private static void Prefix([HarmonyArgument(1)] ref VehicleParked parkedData, ref bool __state)
        {
            VanillaParkingContextInjector.BeginUpdateParkedVehicle(ref parkedData, ref __state);
        }

        private static Exception Finalizer(Exception __exception, bool __state)
        {
            return VanillaParkingContextInjector.EndUpdateParkedVehicle(__exception, __state);
        }
    }
}
