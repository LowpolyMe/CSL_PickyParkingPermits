using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using PickyParking.Logging;
using PickyParking.Settings;

namespace PickyParking.Patching.TMPE
{
    internal static class ParkingPathModeTracker
    {
        private static readonly HashSet<ushort> KnownPathVehicles = new HashSet<ushort>();
        private static readonly HashSet<ushort> AltPathVehicles = new HashSet<ushort>();
        private static readonly Dictionary<ushort, LastStatus> LastStatusByVehicle = new Dictionary<ushort, LastStatus>();
        private static readonly Dictionary<ushort, LastStatus> PreUpdateStatusByVehicle = new Dictionary<ushort, LastStatus>();
        private static Type _extType;
        private static FieldInfo _pathModeField;
        private static FieldInfo _parkingLocationField;
        private static FieldInfo _parkingLocationIdField;
        private static FieldInfo _failedAttemptsField;

        private struct LastStatus
        {
            public string PathMode;
            public string ParkingLocation;
            public ushort ParkingLocationId;
            public int FailedAttempts;
        }

        public static void RecordIfCalculating(object[] args)
        {
            if (!TryExtractUpdateArgs(args, out ushort vehicleId, out object extDriver, out _))
                return;

            string pathMode = ReadPathMode(extDriver);
            if (string.IsNullOrEmpty(pathMode))
                return;

            if (string.Equals(pathMode, "CalculatingCarPathToKnownParkPos", StringComparison.Ordinal))
            {
                KnownPathVehicles.Add(vehicleId);
                return;
            }

            if (string.Equals(pathMode, "CalculatingCarPathToAltParkPos", StringComparison.Ordinal))
            {
                AltPathVehicles.Add(vehicleId);
            }
        }

        public static void RecordIfCalculating(ushort vehicleId, object extDriver)
        {
            if (vehicleId == 0 || extDriver == null)
                return;

            string pathMode = ReadPathMode(extDriver);
            if (string.IsNullOrEmpty(pathMode))
                return;

            if (string.Equals(pathMode, "CalculatingCarPathToKnownParkPos", StringComparison.Ordinal))
            {
                KnownPathVehicles.Add(vehicleId);
                return;
            }

            if (string.Equals(pathMode, "CalculatingCarPathToAltParkPos", StringComparison.Ordinal))
            {
                AltPathVehicles.Add(vehicleId);
            }
        }

        public static void RecordFromUpdateCarPathState(object[] args)
        {
            if (!Log.IsVerboseEnabled || !Log.IsTmpeDebugEnabled)
                return;

            if (!TryExtractUpdateArgs(args, out ushort vehicleId, out object extDriver, out object pathStateObj))
                return;

            if (!TryReadStatus(extDriver, out LastStatus status))
                return;

            string pathState = pathStateObj != null ? pathStateObj.ToString() : "UNKNOWN";
            if (LastStatusByVehicle.TryGetValue(vehicleId, out LastStatus last))
            {
                if (!string.Equals(last.PathMode, status.PathMode, StringComparison.Ordinal))
                {
                    Log.Info(DebugLogCategory.Tmpe,
                        "[TMPE] UpdateCarPathState pathMode changed. " +
                        $"vehicleId={vehicleId} prev={last.PathMode ?? "NULL"} next={status.PathMode ?? "NULL"} " +
                        $"pathState={pathState} parkingLocation={status.ParkingLocation} parkingLocationId={status.ParkingLocationId} " +
                        $"failedAttempts={status.FailedAttempts}"
                    );
                }

                if (!string.Equals(last.ParkingLocation, status.ParkingLocation, StringComparison.Ordinal) ||
                    last.ParkingLocationId != status.ParkingLocationId)
                {
                    Log.Info(DebugLogCategory.Tmpe,
                        "[TMPE] UpdateCarPathState parking location changed. " +
                        $"vehicleId={vehicleId} prev={last.ParkingLocation ?? "NULL"}#{last.ParkingLocationId} " +
                        $"next={status.ParkingLocation ?? "NULL"}#{status.ParkingLocationId} " +
                        $"pathMode={status.PathMode ?? "NULL"} pathState={pathState}"
                    );
                }
            }

            LastStatusByVehicle[vehicleId] = status;
        }

        public static void RecordFromUpdateCarPathState(ushort vehicleId, object extDriver, object pathStateObj)
        {
            if (!Log.IsVerboseEnabled || !Log.IsTmpeDebugEnabled)
                return;

            if (vehicleId == 0 || extDriver == null)
                return;

            if (!TryReadStatus(extDriver, out LastStatus status))
                return;

            string pathState = pathStateObj != null ? pathStateObj.ToString() : "UNKNOWN";
            if (LastStatusByVehicle.TryGetValue(vehicleId, out LastStatus last))
            {
                if (!string.Equals(last.PathMode, status.PathMode, StringComparison.Ordinal))
                {
                    Log.Info(DebugLogCategory.Tmpe,
                        "[TMPE] UpdateCarPathState pathMode changed. " +
                        $"vehicleId={vehicleId} prev={last.PathMode ?? "NULL"} next={status.PathMode ?? "NULL"} " +
                        $"pathState={pathState} parkingLocation={status.ParkingLocation} parkingLocationId={status.ParkingLocationId} " +
                        $"failedAttempts={status.FailedAttempts}"
                    );
                }

                if (!string.Equals(last.ParkingLocation, status.ParkingLocation, StringComparison.Ordinal) ||
                    last.ParkingLocationId != status.ParkingLocationId)
                {
                    Log.Info(DebugLogCategory.Tmpe,
                        "[TMPE] UpdateCarPathState parking location changed. " +
                        $"vehicleId={vehicleId} prev={last.ParkingLocation ?? "NULL"}#{last.ParkingLocationId} " +
                        $"next={status.ParkingLocation ?? "NULL"}#{status.ParkingLocationId} " +
                        $"pathMode={status.PathMode ?? "NULL"} pathState={pathState}"
                    );
                }
            }

            LastStatusByVehicle[vehicleId] = status;
        }

        public static void RecordBeforeUpdateCarPathState(object[] args)
        {
            if (!Log.IsVerboseEnabled || !Log.IsTmpeDebugEnabled)
                return;

            if (!TryExtractUpdateArgs(args, out ushort vehicleId, out object extDriver, out object pathStateObj))
                return;

            if (!TryReadStatus(extDriver, out LastStatus status))
                return;

            PreUpdateStatusByVehicle[vehicleId] = status;
        }

        public static void RecordBeforeUpdateCarPathState(ushort vehicleId, object extDriver, object pathStateObj)
        {
            if (!Log.IsVerboseEnabled || !Log.IsTmpeDebugEnabled)
                return;

            if (vehicleId == 0 || extDriver == null)
                return;

            if (!TryReadStatus(extDriver, out LastStatus status))
                return;

            PreUpdateStatusByVehicle[vehicleId] = status;
        }

        public static void RecordAfterUpdateCarPathState(object[] args)
        {
            if (!Log.IsVerboseEnabled || !Log.IsTmpeDebugEnabled)
                return;

            if (!TryExtractUpdateArgs(args, out ushort vehicleId, out object extDriver, out object pathStateObj))
                return;

            if (!TryReadStatus(extDriver, out LastStatus status))
                return;

            string pathState = pathStateObj != null ? pathStateObj.ToString() : "UNKNOWN";

            if (PreUpdateStatusByVehicle.TryGetValue(vehicleId, out LastStatus before))
            {
                if (!string.Equals(before.PathMode, status.PathMode, StringComparison.Ordinal))
                {
                    Log.Info(DebugLogCategory.Tmpe,
                        "[TMPE] UpdateCarPathState changed pathMode. " +
                        $"vehicleId={vehicleId} prev={before.PathMode ?? "NULL"} next={status.PathMode ?? "NULL"} " +
                        $"pathState={pathState} parkingLocation={status.ParkingLocation} parkingLocationId={status.ParkingLocationId} " +
                        $"failedAttempts={status.FailedAttempts}"
                    );
                }

                if (!string.Equals(before.ParkingLocation, status.ParkingLocation, StringComparison.Ordinal) ||
                    before.ParkingLocationId != status.ParkingLocationId)
                {
                    Log.Info(DebugLogCategory.Tmpe,
                        "[TMPE] UpdateCarPathState changed parking location. " +
                        $"vehicleId={vehicleId} prev={before.ParkingLocation ?? "NULL"}#{before.ParkingLocationId} " +
                        $"next={status.ParkingLocation ?? "NULL"}#{status.ParkingLocationId} " +
                        $"pathMode={status.PathMode ?? "NULL"} pathState={pathState}"
                    );
                }
            }

            LastStatusByVehicle[vehicleId] = status;
            PreUpdateStatusByVehicle.Remove(vehicleId);
        }

        public static void RecordAfterUpdateCarPathState(ushort vehicleId, object extDriver, object pathStateObj)
        {
            if (!Log.IsVerboseEnabled || !Log.IsTmpeDebugEnabled)
                return;

            if (vehicleId == 0 || extDriver == null)
                return;

            if (!TryReadStatus(extDriver, out LastStatus status))
                return;

            string pathState = pathStateObj != null ? pathStateObj.ToString() : "UNKNOWN";

            if (PreUpdateStatusByVehicle.TryGetValue(vehicleId, out LastStatus before))
            {
                if (!string.Equals(before.PathMode, status.PathMode, StringComparison.Ordinal))
                {
                    Log.Info(DebugLogCategory.Tmpe,
                        "[TMPE] UpdateCarPathState changed pathMode. " +
                        $"vehicleId={vehicleId} prev={before.PathMode ?? "NULL"} next={status.PathMode ?? "NULL"} " +
                        $"pathState={pathState} parkingLocation={status.ParkingLocation} parkingLocationId={status.ParkingLocationId} " +
                        $"failedAttempts={status.FailedAttempts}"
                    );
                }

                if (!string.Equals(before.ParkingLocation, status.ParkingLocation, StringComparison.Ordinal) ||
                    before.ParkingLocationId != status.ParkingLocationId)
                {
                    Log.Info(DebugLogCategory.Tmpe,
                        "[TMPE] UpdateCarPathState changed parking location. " +
                        $"vehicleId={vehicleId} prev={before.ParkingLocation ?? "NULL"}#{before.ParkingLocationId} " +
                        $"next={status.ParkingLocation ?? "NULL"}#{status.ParkingLocationId} " +
                        $"pathMode={status.PathMode ?? "NULL"} pathState={pathState}"
                    );
                }
            }

            LastStatusByVehicle[vehicleId] = status;
            PreUpdateStatusByVehicle.Remove(vehicleId);
        }

        public static bool TryDescribeUpdateArgs(object[] args, out string description)
        {
            description = "args=NULL";
            if (args == null)
                return false;

            var sb = new System.Text.StringBuilder();
            sb.Append("argsLen=").Append(args.Length);
            sb.Append(" types=[");
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(args[i]?.GetType().Name ?? "null");
            }
            sb.Append("]");

            if (TryExtractUpdateArgs(args, out ushort vehicleId, out object extDriver, out object pathStateObj))
            {
                sb.Append(" vehicleId=").Append(vehicleId);
                sb.Append(" pathState=").Append(pathStateObj != null ? pathStateObj.ToString() : "UNKNOWN");
                sb.Append(" extType=").Append(extDriver.GetType().FullName);

                if (TryReadStatus(extDriver, out LastStatus status))
                {
                    sb.Append(" pathMode=").Append(status.PathMode ?? "NULL");
                    sb.Append(" location=").Append(status.ParkingLocation ?? "NULL");
                    sb.Append(" locationId=").Append(status.ParkingLocationId);
                    sb.Append(" failedAttempts=").Append(status.FailedAttempts);
                }
                else
                {
                    sb.Append(" status=UNREADABLE");
                }
            }
            else
            {
                sb.Append(" extracted=false");
            }

            description = sb.ToString();
            return true;
        }

        public static bool TryDescribeUpdateArgs(ushort vehicleId, object extDriver, object pathStateObj, out string description)
        {
            description = "args=NULL";
            if (vehicleId == 0 || extDriver == null)
                return false;

            var sb = new System.Text.StringBuilder();
            sb.Append("vehicleId=").Append(vehicleId);
            sb.Append(" pathState=").Append(pathStateObj != null ? pathStateObj.ToString() : "UNKNOWN");
            sb.Append(" extType=").Append(extDriver.GetType().FullName);

            if (TryReadStatus(extDriver, out LastStatus status))
            {
                sb.Append(" pathMode=").Append(status.PathMode ?? "NULL");
                sb.Append(" location=").Append(status.ParkingLocation ?? "NULL");
                sb.Append(" locationId=").Append(status.ParkingLocationId);
                sb.Append(" failedAttempts=").Append(status.FailedAttempts);
            }
            else
            {
                sb.Append(" status=UNREADABLE");
            }

            description = sb.ToString();
            return true;
        }

        public static void RecordParkingLocation(string source, ushort vehicleId, object extDriver)
        {
            if (!Log.IsVerboseEnabled || !Log.IsTmpeDebugEnabled)
                return;

            if (vehicleId == 0 || extDriver == null)
                return;

            if (!TryReadStatus(extDriver, out LastStatus status))
                return;

            if (LastStatusByVehicle.TryGetValue(vehicleId, out LastStatus last))
            {
                if (!string.Equals(last.ParkingLocation, status.ParkingLocation, StringComparison.Ordinal) ||
                    last.ParkingLocationId != status.ParkingLocationId)
                {
                    Log.Info(DebugLogCategory.Tmpe,
                        "[TMPE] Parking location changed. " +
                        $"source={source} vehicleId={vehicleId} " +
                        $"prev={last.ParkingLocation ?? "NULL"}#{last.ParkingLocationId} " +
                        $"next={status.ParkingLocation ?? "NULL"}#{status.ParkingLocationId} " +
                        $"pathMode={status.PathMode ?? "NULL"} failedAttempts={status.FailedAttempts}"
                    );
                }
            }

            LastStatusByVehicle[vehicleId] = status;
        }

        public static bool TryGetStatus(ushort vehicleId, out bool hadKnown, out bool hadAlt)
        {
            hadKnown = false;
            hadAlt = false;

            if (vehicleId == 0)
                return false;

            hadKnown = KnownPathVehicles.Contains(vehicleId);
            hadAlt = AltPathVehicles.Contains(vehicleId);
            return hadKnown || hadAlt;
        }

        public static void Clear(ushort vehicleId)
        {
            if (vehicleId == 0)
                return;

            KnownPathVehicles.Remove(vehicleId);
            AltPathVehicles.Remove(vehicleId);
            LastStatusByVehicle.Remove(vehicleId);
            PreUpdateStatusByVehicle.Remove(vehicleId);
        }

        private static string ReadPathMode(object extDriver)
        {
            try
            {
                Type extType = extDriver.GetType();
                FieldInfo pathModeField = AccessTools.Field(extType, "pathMode");
                object pathModeValue = pathModeField != null ? pathModeField.GetValue(extDriver) : null;
                return pathModeValue != null ? pathModeValue.ToString() : null;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryExtractUpdateArgs(object[] args, out ushort vehicleId, out object extDriver, out object pathState)
        {
            vehicleId = 0;
            extDriver = null;
            pathState = null;

            if (args == null)
                return false;

            for (int i = 0; i < args.Length; i++)
            {
                if (vehicleId == 0 && args[i] is ushort v && v != 0)
                    vehicleId = v;

                if (pathState == null && args[i] != null && args[i].GetType().Name == "ExtPathState")
                    pathState = args[i];

                if (extDriver == null && args[i] != null && HasField(args[i].GetType(), "pathMode"))
                    extDriver = args[i];
            }

            return vehicleId != 0 && extDriver != null;
        }

        private static bool HasField(Type type, string fieldName)
        {
            if (type == null)
                return false;
            return AccessTools.Field(type, fieldName) != null;
        }

        private static bool TryReadStatus(object extDriver, out LastStatus status)
        {
            status = default;
            try
            {
                if (_extType == null || _extType != extDriver.GetType())
                {
                    _extType = extDriver.GetType();
                    _pathModeField = AccessTools.Field(_extType, "pathMode");
                    _parkingLocationField = AccessTools.Field(_extType, "parkingSpaceLocation");
                    _parkingLocationIdField = AccessTools.Field(_extType, "parkingSpaceLocationId");
                    _failedAttemptsField = AccessTools.Field(_extType, "failedParkingAttempts");
                }

                object pathModeValue = _pathModeField != null ? _pathModeField.GetValue(extDriver) : null;
                object locationValue = _parkingLocationField != null ? _parkingLocationField.GetValue(extDriver) : null;
                object locationIdValue = _parkingLocationIdField != null ? _parkingLocationIdField.GetValue(extDriver) : null;
                object failedAttemptsValue = _failedAttemptsField != null ? _failedAttemptsField.GetValue(extDriver) : null;

                status.PathMode = pathModeValue != null ? pathModeValue.ToString() : "UNKNOWN";
                status.ParkingLocation = locationValue != null ? locationValue.ToString() : "UNKNOWN";
                status.ParkingLocationId = locationIdValue is ushort id ? id : (ushort)0;
                status.FailedAttempts = failedAttemptsValue is int fa ? fa : -1;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
