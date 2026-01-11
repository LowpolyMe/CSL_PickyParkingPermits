using System;
using System.Reflection;
using ColossalFramework;
using HarmonyLib;
using PickyParking.Features.Debug;
using PickyParking.Logging;
using PickyParking.ModLifecycle;
using UnityEngine;

namespace PickyParking.Patching.Diagnostics.TMPE
{
    internal static class TMPE_StartPassengerCarPathFindDiagnosticsPatch
    {
        private const string TargetTypeName = "TrafficManager.Manager.Impl.VehicleBehaviorManager, TrafficManager";
        private const string TargetMethodName = "StartPassengerCarPathFind";
        private const string GlobalConfigTypeName = "TrafficManager.State.GlobalConfig, TrafficManager";

        private struct State
        {
            public ushort VehicleId;
            public uint CitizenId;
            public ushort DriverInstanceId;
            public string PathMode;
            public int FailedAttempts;
            public ushort TargetBuildingId;
            public bool IsOutsideConnection;
            public string ParkingLocation;
            public ushort ParkingLocationId;
        }

        private static bool _maxAttemptsChecked;
        private static bool _maxAttemptsWarned;
        private static int _maxAttemptsCached = -1;
        private static bool _parkingAiChecked;
        private static bool _parkingAiWarned;
        private static bool _parkingAiCached;
        private static FieldInfo _savedGameOptionsParkingAiField;
        private static PropertyInfo _savedGameOptionsInstanceProp;

        public static void Apply(Harmony harmony)
        {
            Type type = Type.GetType(TargetTypeName, throwOnError: false);
            if (type == null)
            {
                if (Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled)
                    Log.Info("[TMPE] VehicleBehaviorManager not found; skipping StartPassengerCarPathFind diagnostics patch.");
                return;
            }

            MethodInfo method = FindTargetMethod(type);
            if (method == null)
            {
                if (Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled)
                    Log.Info("[TMPE] StartPassengerCarPathFind overload not found; skipping diagnostics patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(TMPE_StartPassengerCarPathFindDiagnosticsPatch), nameof(Prefix)),
                postfix: new HarmonyMethod(typeof(TMPE_StartPassengerCarPathFindDiagnosticsPatch), nameof(Postfix))
            );

            if (Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled)
                Log.Info("[TMPE] Patched StartPassengerCarPathFind (diagnostics).");
        }

        private static MethodInfo FindTargetMethod(Type vehicleBehaviorManagerType)
        {
            foreach (var m in vehicleBehaviorManagerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(m.Name, TargetMethodName, StringComparison.Ordinal))
                    continue;

                var ps = m.GetParameters();
                if (ps.Length != 14)
                    continue;

                if (ps[0].ParameterType != typeof(ushort)) continue;
                if (!IsByRefOf(ps[1].ParameterType, typeof(Vehicle))) continue;
                if (ps[2].ParameterType != typeof(VehicleInfo)) continue;
                if (ps[3].ParameterType != typeof(ushort)) continue;
                if (!IsByRefOf(ps[4].ParameterType, typeof(CitizenInstance))) continue;

                if (!ps[5].ParameterType.IsByRef) continue;
                var extElem = ps[5].ParameterType.GetElementType();
                if (extElem == null || !string.Equals(extElem.Name, "ExtCitizenInstance", StringComparison.Ordinal)) continue;

                if (ps[6].ParameterType != typeof(Vector3)) continue;
                if (ps[7].ParameterType != typeof(Vector3)) continue;

                for (int i = 8; i < 14; i++)
                {
                    if (ps[i].ParameterType != typeof(bool))
                        goto ContinueOuter;
                }

                return m;

            ContinueOuter:
                continue;
            }

            return null;
        }

        private static bool IsByRefOf(Type maybeByRef, Type elementType)
        {
            if (!maybeByRef.IsByRef) return false;
            return maybeByRef.GetElementType() == elementType;
        }

        private static void Prefix(
            [HarmonyArgument(0)] ushort vehicleId,
            [HarmonyArgument(3)] ushort driverInstanceId,
            [HarmonyArgument(4)] ref CitizenInstance driverInstance,
            [HarmonyArgument(5)] object extDriverInstance,
            ref State __state)
        {
            __state = default;
            if (!SimThread.IsSimulationThread())
                return;
            try
            {
                __state.VehicleId = vehicleId;
                __state.DriverInstanceId = driverInstanceId;
                __state.CitizenId = driverInstance.m_citizen;
                __state.TargetBuildingId = driverInstance.m_targetBuilding;
                __state.IsOutsideConnection = IsOutsideConnection(driverInstance.m_targetBuilding);

                if (extDriverInstance == null)
                    return;

                Type extType = extDriverInstance.GetType();
                FieldInfo pathModeField = AccessTools.Field(extType, "pathMode");
                FieldInfo failedAttemptsField = AccessTools.Field(extType, "failedParkingAttempts");
                FieldInfo locationField = AccessTools.Field(extType, "parkingSpaceLocation");
                FieldInfo locationIdField = AccessTools.Field(extType, "parkingSpaceLocationId");

                object pathModeValue = pathModeField != null ? pathModeField.GetValue(extDriverInstance) : null;
                __state.PathMode = pathModeValue != null ? pathModeValue.ToString() : "UNKNOWN";

                if (failedAttemptsField != null && failedAttemptsField.GetValue(extDriverInstance) is int failedAttempts)
                    __state.FailedAttempts = failedAttempts;
                else
                    __state.FailedAttempts = -1;

                object locationValue = locationField != null ? locationField.GetValue(extDriverInstance) : null;
                if (locationValue != null)
                    __state.ParkingLocation = locationValue.ToString();

                if (locationIdField != null && locationIdField.GetValue(extDriverInstance) is ushort locId)
                    __state.ParkingLocationId = locId;
            }
            catch (Exception ex)
            {
                Log.Error("[TMPE] StartPassengerCarPathFind prefix exception\n" + ex);
            }
        }

        private static void Postfix(bool __result, ref State __state)
        {
            if (!SimThread.IsSimulationThread())
                return;
            if (IsDecisionLoggingEnabled())
            {
                TryGetFinalExtState(__state.DriverInstanceId, out string finalPathMode, out int finalFailedAttempts, out string finalLocation, out ushort finalLocationId);
                string parkingAi = TryGetParkingAiEnabled(out bool enabled) ? enabled.ToString() : "UNKNOWN";
                Log.Info(
                    "[TMPE] SPF decision. " +
                    $"vehicleId={__state.VehicleId} driverInstanceId={__state.DriverInstanceId} " +
                    $"parkingAI={parkingAi} outsideConn={__state.IsOutsideConnection} " +
                    $"initialPathMode={__state.PathMode} finalPathMode={finalPathMode} " +
                    $"failedAttempts={__state.FailedAttempts}->{finalFailedAttempts} " +
                    $"location={finalLocation} locationId={finalLocationId}"
                );

                if (__state.IsOutsideConnection &&
                    string.Equals(finalPathMode, "CalculatingCarPathToTarget", StringComparison.Ordinal))
                {
                    Log.Info(
                        "[TMPE] SPF branch: outside connection -> CalculatingCarPathToTarget. " +
                        $"targetBuildingId={__state.TargetBuildingId}"
                    );
                }

                if (string.Equals(__state.PathMode, "ParkingFailed", StringComparison.Ordinal) &&
                    string.Equals(finalPathMode, "CalculatingCarPathToAltParkPos", StringComparison.Ordinal))
                {
                    Log.Info(
                        "[TMPE] SPF branch: ParkingFailed -> CalculatingCarPathToAltParkPos. " +
                        $"failedAttempts={__state.FailedAttempts}"
                    );
                }

                if (string.Equals(finalPathMode, "CalculatingCarPathToKnownParkPos", StringComparison.Ordinal))
                {
                    Log.Info(
                        "[TMPE] SPF branch: presearch known parking. " +
                        $"location={finalLocation} locationId={finalLocationId}"
                    );
                }
            }

            if (__result)
                return;

            if (!Log.IsVerboseEnabled || !Log.IsTmpeDebugEnabled)
                return;

            string pathMode = __state.PathMode ?? "UNKNOWN";
            if (!IsParkingFailurePathMode(pathMode))
                return;

            int maxAttempts = GetMaxParkingAttempts();
            if (maxAttempts < 0)
            {
                Log.Info(
                    "[TMPE] StartPassengerCarPathFind returned false after parking failure (maxAttempts unknown). " +
                    $"vehicleId={__state.VehicleId} citizenId={__state.CitizenId} driverInstanceId={__state.DriverInstanceId} " +
                    $"pathMode={pathMode} failedAttempts={__state.FailedAttempts}"
                );
                VehicleDespawnReasonCache.Record(__state.VehicleId, $"ParkingFailed attempts={__state.FailedAttempts}");
                return;
            }

            if (__state.FailedAttempts <= maxAttempts)
                return;

            VehicleDespawnReasonCache.Record(
                __state.VehicleId,
                $"ParkingFailed attempts={__state.FailedAttempts} max={maxAttempts}"
            );

            Log.Warn(
                "[TMPE] StartPassengerCarPathFind returned false after parking failure. " +
                $"vehicleId={__state.VehicleId} citizenId={__state.CitizenId} driverInstanceId={__state.DriverInstanceId} " +
                $"pathMode={pathMode} failedAttempts={__state.FailedAttempts} maxAttempts={maxAttempts}"
            );
        }

        private static bool IsParkingFailurePathMode(string pathMode)
        {
            return string.Equals(pathMode, "ParkingFailed", StringComparison.Ordinal)
                || string.Equals(pathMode, "CalculatingCarPathToAltParkPos", StringComparison.Ordinal);
        }

        private static bool IsDecisionLoggingEnabled()
        {
            return Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled;
        }

        private static bool IsOutsideConnection(ushort buildingId)
        {
            try
            {
                if (buildingId == 0)
                    return false;

                ref Building building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId];
                return (building.m_flags & Building.Flags.IncomingOutgoing) != Building.Flags.None;
            }
            catch
            {
                return false;
            }
        }

        private static void TryGetFinalExtState(
            ushort driverInstanceId,
            out string pathMode,
            out int failedAttempts,
            out string parkingLocation,
            out ushort parkingLocationId)
        {
            pathMode = "UNKNOWN";
            failedAttempts = -1;
            parkingLocation = "UNKNOWN";
            parkingLocationId = 0;

            try
            {
                if (driverInstanceId == 0)
                    return;

                var extMgrType = Type.GetType("TrafficManager.Manager.Impl.ExtCitizenInstanceManager, TrafficManager", throwOnError: false);
                if (extMgrType == null)
                    return;

                object instance = null;
                PropertyInfo instanceProp = AccessTools.Property(extMgrType, "Instance");
                if (instanceProp != null)
                    instance = instanceProp.GetValue(null, null);
                if (instance == null)
                {
                    FieldInfo instanceField = AccessTools.Field(extMgrType, "Instance");
                    if (instanceField != null)
                        instance = instanceField.GetValue(null);
                }
                if (instance == null)
                    return;

                object extInstances = null;
                PropertyInfo extInstancesProp = AccessTools.Property(extMgrType, "ExtInstances");
                if (extInstancesProp != null)
                    extInstances = extInstancesProp.GetValue(instance, null);
                if (extInstances == null)
                {
                    FieldInfo extInstancesField = AccessTools.Field(extMgrType, "ExtInstances");
                    if (extInstancesField != null)
                        extInstances = extInstancesField.GetValue(instance);
                }
                if (extInstances == null)
                    return;

                Array extArray = extInstances as Array;
                if (extArray == null || driverInstanceId >= extArray.Length)
                    return;

                object extDriver = extArray.GetValue(driverInstanceId);
                if (extDriver == null)
                    return;

                Type extType = extDriver.GetType();
                FieldInfo pathModeField = AccessTools.Field(extType, "pathMode");
                FieldInfo failedAttemptsField = AccessTools.Field(extType, "failedParkingAttempts");
                FieldInfo locationField = AccessTools.Field(extType, "parkingSpaceLocation");
                FieldInfo locationIdField = AccessTools.Field(extType, "parkingSpaceLocationId");

                object pathModeValue = pathModeField != null ? pathModeField.GetValue(extDriver) : null;
                if (pathModeValue != null)
                    pathMode = pathModeValue.ToString();

                if (failedAttemptsField != null && failedAttemptsField.GetValue(extDriver) is int fa)
                    failedAttempts = fa;

                object locationValue = locationField != null ? locationField.GetValue(extDriver) : null;
                if (locationValue != null)
                    parkingLocation = locationValue.ToString();

                if (locationIdField != null && locationIdField.GetValue(extDriver) is ushort locId)
                    parkingLocationId = locId;
            }
            catch
            {
            }
        }

        private static bool TryGetParkingAiEnabled(out bool enabled)
        {
            enabled = false;
            if (_parkingAiChecked)
            {
                enabled = _parkingAiCached;
                return true;
            }

            _parkingAiChecked = true;
            try
            {
                Type savedOptionsType = Type.GetType("TrafficManager.State.SavedGameOptions, TrafficManager", throwOnError: false);
                if (savedOptionsType == null)
                {
                    WarnMissingParkingAi("SavedGameOptions type not found");
                    return false;
                }

                _savedGameOptionsInstanceProp = AccessTools.Property(savedOptionsType, "Instance");
                object instance = _savedGameOptionsInstanceProp != null ? _savedGameOptionsInstanceProp.GetValue(null, null) : null;
                if (instance == null)
                {
                    WarnMissingParkingAi("SavedGameOptions.Instance not found");
                    return false;
                }

                _savedGameOptionsParkingAiField = AccessTools.Field(instance.GetType(), "parkingAI");
                if (_savedGameOptionsParkingAiField == null)
                {
                    WarnMissingParkingAi("SavedGameOptions.parkingAI not found");
                    return false;
                }

                if (_savedGameOptionsParkingAiField.GetValue(instance) is bool value)
                {
                    _parkingAiCached = value;
                    enabled = value;
                    return true;
                }

                WarnMissingParkingAi("SavedGameOptions.parkingAI not bool");
                return false;
            }
            catch (Exception ex)
            {
                WarnMissingParkingAi("Exception: " + ex.Message);
                return false;
            }
        }

        private static void WarnMissingParkingAi(string reason)
        {
            if (_parkingAiWarned || !Log.IsVerboseEnabled || !Log.IsTmpeDebugEnabled)
                return;

            _parkingAiWarned = true;
            Log.Info("[TMPE] Parking decision logs: cannot read SavedGameOptions.parkingAI (" + reason + ").");
        }

        private static int GetMaxParkingAttempts()
        {
            if (_maxAttemptsChecked)
                return _maxAttemptsCached;

            _maxAttemptsChecked = true;

            try
            {
                Type globalConfigType = Type.GetType(GlobalConfigTypeName, throwOnError: false);
                if (globalConfigType == null)
                {
                    WarnMissingMaxAttempts("GlobalConfig type not found");
                    return _maxAttemptsCached;
                }

                PropertyInfo instanceProp = AccessTools.Property(globalConfigType, "Instance");
                object instance = instanceProp != null ? instanceProp.GetValue(null, null) : null;
                if (instance == null)
                {
                    WarnMissingMaxAttempts("GlobalConfig.Instance not found");
                    return _maxAttemptsCached;
                }

                PropertyInfo parkingAiProp = AccessTools.Property(instance.GetType(), "ParkingAI");
                object parkingAi = parkingAiProp != null ? parkingAiProp.GetValue(instance, null) : null;
                if (parkingAi == null)
                {
                    WarnMissingMaxAttempts("GlobalConfig.ParkingAI not found");
                    return _maxAttemptsCached;
                }

                PropertyInfo maxAttemptsProp = AccessTools.Property(parkingAi.GetType(), "MaxParkingAttempts");
                object maxAttemptsValue = maxAttemptsProp != null ? maxAttemptsProp.GetValue(parkingAi, null) : null;
                if (!(maxAttemptsValue is int maxAttempts))
                {
                    WarnMissingMaxAttempts("ParkingAI.MaxParkingAttempts not found");
                    return _maxAttemptsCached;
                }

                _maxAttemptsCached = maxAttempts;
                return _maxAttemptsCached;
            }
            catch (Exception ex)
            {
                WarnMissingMaxAttempts("Exception: " + ex.Message);
                return _maxAttemptsCached;
            }
        }

        private static void WarnMissingMaxAttempts(string reason)
        {
            if (_maxAttemptsWarned || !Log.IsVerboseEnabled || !Log.IsTmpeDebugEnabled)
                return;

            _maxAttemptsWarned = true;
            Log.Info("[TMPE] Parking failure stack traces disabled: cannot read MaxParkingAttempts (" + reason + ").");
        }
    }
}

