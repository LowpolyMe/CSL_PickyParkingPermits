using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Features.Debug;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Logging;
using PickyParking.Patching.TMPE;
using PickyParking.Settings;

namespace PickyParking.Patching.Diagnostics.TMPE
{
    internal static class TMPE_ParkPassengerCarDiagnosticsPatch
    {
        private const string TargetTypeName = "TrafficManager.Manager.Impl.VehicleBehaviorManager, TrafficManager";
        private const string TargetMethodName = "ParkPassengerCar";

        public static void Apply(Harmony harmony)
        {
            Type type = Type.GetType(TargetTypeName, throwOnError: false);
            if (type == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                {
                    Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "DiagnosticsSkippedMissingType", "type=VehicleBehaviorManager");
                }
                return;
            }

            MethodInfo method = AccessTools.Method(type, TargetMethodName);
            if (method == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                {
                    Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "DiagnosticsSkippedMissingMethod", "type=VehicleBehaviorManager | method=" + TargetMethodName);
                }
                return;
            }

            harmony.Patch(
                method,
                postfix: new HarmonyMethod(typeof(TMPE_ParkPassengerCarDiagnosticsPatch), nameof(Postfix))
            );

            if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
            {
                Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "DiagnosticsPatchApplied", "method=" + TargetMethodName);
            }
        }

        private static void Postfix(
            bool __result,
            [HarmonyArgument(0)] ushort vehicleId,
            [HarmonyArgument(3)] uint driverCitizenId,
            [HarmonyArgument(7)] object extDriverInstance,
            [HarmonyArgument(8)] ushort targetBuildingId)
        {
            if (IsDecisionLoggingEnabled())
            {
                if (TryGetExtDriverState(extDriverInstance, out string decisionPathMode, out string decisionLocation, out ushort decisionLocationId))
                {
                    string branch = DetermineBranch(decisionPathMode, decisionLocation);
                    Log.Dev.Info(
                        DebugLogCategory.Tmpe,
                        LogPath.TMPE,
                        "ParkVehicleDecision",
                        "pathMode=" + decisionPathMode +
                        " | storedLocation=" + decisionLocation +
                        " | locationId=" + decisionLocationId +
                        " | branch=" + branch);

                    if (string.Equals(branch, "Vanilla", StringComparison.Ordinal))
                    {
                        Log.Dev.Info(
                            DebugLogCategory.Tmpe,
                            LogPath.TMPE,
                            "ParkVehicleVanillaFallback",
                            "pathMode=" + decisionPathMode +
                            " | storedLocation=" + decisionLocation);
                    }
                }
            }

            if (__result)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                {
                    ushort successVehicleId = vehicleId;

                    string successLocation = "UNKNOWN";
                    ushort successLocationId = 0;
                    TryGetKnownParkingLocation(extDriverInstance, ref successLocation, ref successLocationId);
                    if (string.Equals(successLocation, "Building", StringComparison.Ordinal))
                    {
                        Log.Dev.Info(
                            DebugLogCategory.Tmpe,
                            LogPath.TMPE,
                            "ParkPassengerCarSucceeded",
                            "knownLocation=" + successLocation + " | locationId=" + successLocationId);
                    }

                    if (TryGetExtDriverState(extDriverInstance, out string successPathMode, out string successParkingLocation, out ushort successParkingLocationId))
                    {
                        Log.Dev.Info(
                            DebugLogCategory.Tmpe,
                            LogPath.TMPE,
                            "ParkPassengerCarState",
                            "pathMode=" + successPathMode +
                            " | parkingLocation=" + successParkingLocation +
                            " | parkingLocationId=" + successParkingLocationId);
                    }

                    if (successVehicleId != 0)
                    {
                        if (extDriverInstance != null)
                        {
                            ParkingPathModeTracker.RecordParkingLocation(
                                "ParkPassengerCar",
                                successVehicleId,
                                extDriverInstance);
                        }

                        if (ParkingPathModeTracker.TryGetStatus(successVehicleId, out bool hadKnown, out bool hadAlt))
                        {
                            Log.Dev.Info(
                                DebugLogCategory.Tmpe,
                                LogPath.TMPE,
                                "ParkPassengerCarPathHistory",
                                "knownCalculated=" + hadKnown + " | altCalculated=" + hadAlt);
                        }
                        ParkingPathModeTracker.Clear(successVehicleId);
                    }
                }
                return;
            }

            if (__result)
                return;

            if (!Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                return;

            string pathMode = "UNKNOWN";
            int failedAttempts = -1;

            try
            {
                if (extDriverInstance != null)
                {
                    var extType = extDriverInstance.GetType();
                    var pathModeField = AccessTools.Field(extType, "pathMode");
                    var failedAttemptsField = AccessTools.Field(extType, "failedParkingAttempts");
                    object pathModeValue = pathModeField != null ? pathModeField.GetValue(extDriverInstance) : null;
                    if (pathModeValue != null)
                        pathMode = pathModeValue.ToString();
                    if (failedAttemptsField != null && failedAttemptsField.GetValue(extDriverInstance) is int fa)
                        failedAttempts = fa;
                }
            }
            catch (Exception ex)
            {
                Log.Dev.Warn(
                    DebugLogCategory.Tmpe,
                    LogPath.TMPE,
                    "ExtDriverStateReadFailed",
                    "error=" + ex,
                    "TMPE.ParkPassengerCarDiagnostics.ReadState");
                pathMode = "ERR";
            }

            string parkingLocation = "UNKNOWN";
            ushort parkingLocationId = 0;

            TryGetKnownParkingLocation(extDriverInstance, ref parkingLocation, ref parkingLocationId);

            Log.Dev.Info(
                DebugLogCategory.Tmpe,
                LogPath.TMPE,
                "ParkPassengerCarFailed",
                "vehicleId=" + vehicleId +
                " | driverCitizenId=" + driverCitizenId +
                " | pathMode=" + pathMode +
                " | failedAttempts=" + failedAttempts +
                " | knownLocation=" + parkingLocation +
                " | locationId=" + parkingLocationId +
                " | targetBuildingId=" + targetBuildingId);

            if (TryGetExtDriverState(extDriverInstance, out string extPathMode, out string extParkingLocation, out ushort extParkingLocationId))
            {
                Log.Dev.Info(
                    DebugLogCategory.Tmpe,
                    LogPath.TMPE,
                    "ParkPassengerCarState",
                    "pathMode=" + extPathMode +
                    " | parkingLocation=" + extParkingLocation +
                    " | parkingLocationId=" + extParkingLocationId);
            }

            if (ParkingPathModeTracker.TryGetStatus(vehicleId, out bool failedHadKnown, out bool failedHadAlt))
            {
                Log.Dev.Info(
                    DebugLogCategory.Tmpe,
                    LogPath.TMPE,
                    "ParkPassengerCarPathHistory",
                    "knownCalculated=" + failedHadKnown + " | altCalculated=" + failedHadAlt);
            }

            if (vehicleId != 0 && extDriverInstance != null)
            {
                ParkingPathModeTracker.RecordParkingLocation(
                    "ParkPassengerCar",
                    vehicleId,
                    extDriverInstance);
            }
            ParkingPathModeTracker.Clear(vehicleId);

            if (vehicleId != 0)
            {
                VehicleDespawnReasonCache.Record(
                    vehicleId,
                    $"ParkPassengerCarFailed location={parkingLocation} id={parkingLocationId} target={targetBuildingId}"
                );
            }
        }

        private static void TryGetKnownParkingLocation(object extDriverInstance, ref string location, ref ushort locationId)
        {
            if (extDriverInstance == null)
                return;

            try
            {
                Type extType = extDriverInstance.GetType();

                FieldInfo locationField = AccessTools.Field(extType, "parkingSpaceLocation");
                FieldInfo locationIdField = AccessTools.Field(extType, "parkingSpaceLocationId");

                object locationValue = locationField != null ? locationField.GetValue(extDriverInstance) : null;
                if (locationValue != null)
                    location = locationValue.ToString();

                if (locationIdField != null && locationIdField.GetValue(extDriverInstance) is ushort id)
                    locationId = id;
            }
            catch (Exception ex)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                {
                    Log.Dev.Warn(
                        DebugLogCategory.Tmpe,
                        LogPath.TMPE,
                        "KnownParkingLocationReadFailed",
                        "error=" + ex,
                        "TMPE.ParkPassengerCarDiagnostics.KnownLocation");
                }
            }
        }

        private static bool TryGetExtDriverState(
            object extDriverInstance,
            out string pathMode,
            out string parkingLocation,
            out ushort parkingLocationId)
        {
            pathMode = "UNKNOWN";
            parkingLocation = "UNKNOWN";
            parkingLocationId = 0;

            if (extDriverInstance == null)
                return false;

            try
            {
                Type extType = extDriverInstance.GetType();

                FieldInfo pathModeField = AccessTools.Field(extType, "pathMode");
                FieldInfo locationField = AccessTools.Field(extType, "parkingSpaceLocation");
                FieldInfo locationIdField = AccessTools.Field(extType, "parkingSpaceLocationId");

                object pathModeValue = pathModeField != null ? pathModeField.GetValue(extDriverInstance) : null;
                if (pathModeValue != null)
                    pathMode = pathModeValue.ToString();

                object locationValue = locationField != null ? locationField.GetValue(extDriverInstance) : null;
                if (locationValue != null)
                    parkingLocation = locationValue.ToString();

                object locationIdValue = locationIdField != null ? locationIdField.GetValue(extDriverInstance) : null;
                if (locationIdValue is ushort id)
                    parkingLocationId = id;

                return true;
            }
            catch (Exception ex)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                {
                    Log.Dev.Warn(
                        DebugLogCategory.Tmpe,
                        LogPath.TMPE,
                        "ExtDriverStateReadFailed",
                        "error=" + ex,
                        "TMPE.ParkPassengerCarDiagnostics.ExtState");
                }
                return false;
            }
        }

        private static bool IsDecisionLoggingEnabled()
        {
            return Log.Dev.IsEnabled(DebugLogCategory.Tmpe);
        }

        private static string DetermineBranch(string pathMode, string location)
        {
            if (string.Equals(pathMode, "DrivingToKnownParkPos", StringComparison.Ordinal) ||
                string.Equals(pathMode, "DrivingToAltParkPos", StringComparison.Ordinal))
            {
                if (string.Equals(location, "Building", StringComparison.Ordinal))
                    return "KnownBuilding";
                if (string.Equals(location, "RoadSide", StringComparison.Ordinal))
                    return "KnownRoadSide";
                return "KnownUnknown";
            }

            return "Vanilla";
        }
    }
}

