using System;
using System.Reflection;
using ColossalFramework;
using HarmonyLib;
using PickyParking.Features.Debug;
using PickyParking.Logging;
using PickyParking.Settings;

namespace PickyParking.Patching.Diagnostics.Game
{
    internal static class VehicleManager_ReleaseVehicleDiagnosticsPatch
    {
        private const string TargetMethodName = "ReleaseVehicle";

        public static void Apply(Harmony harmony)
        {
            MethodInfo method = FindTargetMethod();
            if (method == null)
            {
                if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
                    Log.Info(DebugLogCategory.Enforcement, "[Diagnostics] ReleaseVehicle not found; skipping diagnostics patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(VehicleManager_ReleaseVehicleDiagnosticsPatch), nameof(Prefix))
            );

            if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
                Log.Info(DebugLogCategory.Enforcement, "[Diagnostics] Patched ReleaseVehicle (diagnostics).");
        }

        private static MethodInfo FindTargetMethod()
        {
            Type type = typeof(VehicleManager);

            foreach (var m in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(m.Name, TargetMethodName, StringComparison.Ordinal))
                    continue;

                var ps = m.GetParameters();
                if (ps.Length == 1 && ps[0].ParameterType == typeof(ushort))
                    return m;

                if (ps.Length == 2 && ps[0].ParameterType == typeof(ushort) && IsByRefOf(ps[1].ParameterType, typeof(Vehicle)))
                    return m;
            }

            return null;
        }

        private static bool IsByRefOf(Type maybeByRef, Type elementType)
        {
            if (!maybeByRef.IsByRef) return false;
            return maybeByRef.GetElementType() == elementType;
        }

        private static void Prefix(object[] __args)
        {
            if (!Log.IsVerboseEnabled || !Log.IsEnforcementDebugEnabled)
                return;

            try
            {
                if (__args == null || __args.Length == 0 || !(__args[0] is ushort vehicleId))
                    return;

                ref Vehicle vehicle = ref Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleId];
                if (!IsPassengerCar(ref vehicle))
                    return;
                uint citizenUnits = vehicle.m_citizenUnits;
                ushort sourceBuilding = vehicle.m_sourceBuilding;
                ushort targetBuilding = vehicle.m_targetBuilding;

                VehicleDespawnReasonCache.Prune();
                string reason = VehicleDespawnReasonCache.TryConsume(vehicleId, out var cachedReason)
                    ? cachedReason
                    : "Unknown";

                string aiName = GetVehicleAiName(ref vehicle);
                if (targetBuilding != 0) //indicates outside connection, in which case its a normal expected behaviour
                {
                    //disabled because in vanilla enforcement, this will be spammed constantly and its a normal behaviour for vehicles to despawn before respawning as parked. keeping this for tmpe diagnostics and possible future use
                    /*Log.Warn(DebugLogCategory.Enforcement,
                        "[Diagnostics] ReleaseVehicle called. " +
                        $"vehicleId={vehicleId} reason={reason} ai={aiName} citizenUnits={citizenUnits} sourceBuilding={sourceBuilding} targetBuilding={targetBuilding} flags={vehicle.m_flags}"
                    );*/
                }
            }
            catch (Exception ex)
            {
                Log.AlwaysError("[Diagnostics] ReleaseVehicle prefix exception\n" + ex);
            }
        }

        private static bool IsPassengerCar(ref Vehicle vehicle)
        {
            VehicleInfo info = vehicle.Info;
            if (info == null)
                return true;

            if ((info.m_vehicleType & VehicleInfo.VehicleType.Car) == VehicleInfo.VehicleType.None)
                return false;

            string aiName = GetVehicleAiName(ref vehicle);

            if (!(info.m_vehicleAI is PassengerCarAI) && info.m_vehicleAI.GetType().Name != "CustomPassengerCarAI")
                return false;

            return true;
        }

        private static string GetVehicleAiName(ref Vehicle vehicle)
        {
            VehicleInfo info = vehicle.Info;
            if (info == null || info.m_vehicleAI == null)
                return "UNKNOWN";

            return info.m_vehicleAI.GetType().Name ?? "UNKNOWN";
        }
    }
}

