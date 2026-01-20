using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ColossalFramework;
using HarmonyLib;
using PickyParking.Features.Debug;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Logging;
using PickyParking.Patching.TMPE;
using UnityEngine;
using PickyParking.Settings;

namespace PickyParking.Patching.Diagnostics.TMPE
{
    internal static class TMPE_FindParkingSpaceForCitizenDiagnosticsPatch
    {
        private const string TargetTypeName = "TrafficManager.Manager.Impl.AdvancedParkingManager, TrafficManager";
        private const string TargetMethodName = "FindParkingSpaceForCitizen";
        private const float SummaryIntervalSeconds = 10f;
        private static int _failCount;
        private static int _failCandidatesZero;
        private static int _failAllDenied;
        private static int _failAllowedButFailed;
        private static int _failNonTouristCandidatesZero;
        private static int _failNonTouristAllDenied;
        private static float _nextSummaryTime;
        private static readonly Dictionary<uint, int> _lastFailedAttemptsByInstance = new Dictionary<uint, int>();
        private static FieldInfo _extInstanceIdField;
        private static FieldInfo _extFailedAttemptsField;
        private static FieldInfo _extPathModeField;
        private static Type _extInstanceType;
        private static readonly HashSet<Type> _extInstanceFieldWarned = new HashSet<Type>();

        public static void ClearAll()
        {
            _failCount = 0;
            _failCandidatesZero = 0;
            _failAllDenied = 0;
            _failAllowedButFailed = 0;
            _failNonTouristCandidatesZero = 0;
            _failNonTouristAllDenied = 0;
            _nextSummaryTime = 0f;
            _lastFailedAttemptsByInstance.Clear();
            _extInstanceIdField = null;
            _extFailedAttemptsField = null;
            _extPathModeField = null;
            _extInstanceType = null;
            _extInstanceFieldWarned.Clear();
        }

        public static void Apply(Harmony harmony)
        {
            Type type = Type.GetType(TargetTypeName, throwOnError: false);
            if (type == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                {
                    Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "DiagnosticsSkippedMissingType", "type=AdvancedParkingManager");
                }
                return;
            }

            MethodInfo method = FindTargetMethod(type);
            if (method == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                {
                    Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "DiagnosticsSkippedMissingMethod", "type=AdvancedParkingManager | method=" + TargetMethodName);
                }
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(TMPE_FindParkingSpaceForCitizenDiagnosticsPatch), nameof(Prefix)),
                postfix: new HarmonyMethod(typeof(TMPE_FindParkingSpaceForCitizenDiagnosticsPatch), nameof(Postfix))
            );

            if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
            {
                Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "DiagnosticsPatchApplied", "method=" + TargetMethodName);
            }
        }

        private static MethodInfo FindTargetMethod(Type advancedParkingManagerType)
        {
            foreach (var m in advancedParkingManagerType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(m.Name, TargetMethodName, StringComparison.Ordinal))
                    continue;

                var ps = m.GetParameters();
                if (ps.Length != 11)
                    continue;

                if (ps[0].ParameterType != typeof(Vector3)) continue;
                if (ps[1].ParameterType != typeof(VehicleInfo)) continue;

                if (!IsByRefOf(ps[2].ParameterType, typeof(CitizenInstance))) continue;

                if (!ps[3].ParameterType.IsByRef) continue;
                var extElem = ps[3].ParameterType.GetElementType();
                if (extElem == null || !string.Equals(extElem.Name, "ExtCitizenInstance", StringComparison.Ordinal)) continue;

                if (ps[4].ParameterType != typeof(ushort)) continue;
                if (ps[5].ParameterType != typeof(bool)) continue;
                if (ps[6].ParameterType != typeof(ushort)) continue;
                if (ps[7].ParameterType != typeof(bool)) continue;

                if (!IsByRefOf(ps[8].ParameterType, typeof(Vector3))) continue;
                if (!IsByRefOf(ps[9].ParameterType, typeof(PathUnit.Position))) continue;
                if (!IsByRefOf(ps[10].ParameterType, typeof(bool))) continue;

                return m;
            }

            return null;
        }

        private static bool IsByRefOf(Type maybeByRef, Type elementType)
        {
            if (!maybeByRef.IsByRef) return false;
            var elem = maybeByRef.GetElementType();
            return elem == elementType;
        }

        private static void Prefix(
            [HarmonyArgument(2)] ref CitizenInstance driverInstance,
            [HarmonyArgument(3)] object extDriverInstanceObj,
            [HarmonyArgument(6)] ushort vehicleId)
        {
            MaybeLogFailedAttempts(extDriverInstanceObj, ref driverInstance, vehicleId);

            TryGetDriverTourist(ref driverInstance, out _);
        }

        private static void Postfix(
            bool __result,
            [HarmonyArgument(0)] Vector3 endPos,
            [HarmonyArgument(2)] ref CitizenInstance driverInstance,
            [HarmonyArgument(3)] object extDriverInstanceObj,
            [HarmonyArgument(4)] ushort homeId,
            [HarmonyArgument(5)] bool goingHome,
            [HarmonyArgument(6)] ushort vehicleId,
            [HarmonyArgument(7)] bool allowTourists)
        {
            if (__result && Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
            {
                if (TryGetExtParkingLocation(extDriverInstanceObj, out string location, out ushort locationId))
                {
                    Log.Dev.Info(
                        DebugLogCategory.Tmpe,
                        LogPath.TMPE,
                        "FindParkingSpaceForCitizenSucceeded",
                        "vehicleId=" + vehicleId +
                        " | citizenId=" + driverInstance.m_citizen +
                        " | targetBuildingId=" + driverInstance.m_targetBuilding +
                        " | parkingLocation=" + location +
                        " | parkingLocationId=" + locationId);
                }

                if (vehicleId != 0)
                {
                    ParkingPathModeTracker.RecordParkingLocation(
                        "FindParkingSpaceForCitizen",
                        vehicleId,
                        extDriverInstanceObj);
                }
            }

            if (__result)
                return;

            if (!Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                return;

            TryGetDriverTourist(ref driverInstance, out bool driverIsTourist);

            if (ParkingSearchContext.TryGetEpisodeSnapshot(out var snapshot))
            {
                AccumulateFailure(snapshot, driverIsTourist);
                bool isAllDenied = snapshot.AllowedCount == 0 && snapshot.DeniedCount > 0;
                bool isCandidateZero = snapshot.CandidateChecks == 0;
                bool isAllowedButFailed = snapshot.AllowedCount > 0;
                bool shouldLogDetail = !driverIsTourist && (isAllDenied || isCandidateZero);
                if (shouldLogDetail)
                {
                    string allDeniedNote = isAllDenied ? " allCandidatesDenied=true" : string.Empty;
                    string fields =
                        "vehicleId=" + snapshot.VehicleId +
                        " | citizenId=" + snapshot.CitizenId +
                        " | isVisitor=" + snapshot.IsVisitor +
                        " | source=" + (snapshot.Source ?? "NULL") +
                        " | candidates=" + snapshot.CandidateChecks +
                        " | denied=" + snapshot.DeniedCount +
                        " | allowed=" + snapshot.AllowedCount +
                        " | lastReason=" + (snapshot.LastReason ?? "NULL") +
                        " | lastBuildingId=" + snapshot.LastBuildingId +
                        " | lastPrefab=" + (snapshot.LastPrefab ?? "NULL") +
                        " | lastBuildingName=" + (snapshot.LastBuildingName ?? "NULL");
                    if (isAllDenied)
                        fields = fields + " | allCandidatesDenied=true";
                    if (isCandidateZero)
                    {
                        fields = fields +
                                 " | endPosX=" + endPos.x.ToString("F1") +
                                 " | endPosY=" + endPos.y.ToString("F1") +
                                 " | endPosZ=" + endPos.z.ToString("F1") +
                                 " | homeId=" + homeId +
                                 " | goingHome=" + goingHome +
                                 " | vehicleArg=" + vehicleId +
                                 " | allowTourists=" + allowTourists +
                                 FormatDriverInfo(ref driverInstance);
                    }
                    Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "FindParkingSpaceForCitizenFailed", fields);
                }
                else if (isAllowedButFailed)
                {
                    Log.Dev.Info(
                        DebugLogCategory.Tmpe,
                        LogPath.TMPE,
                        "FindParkingSpaceForCitizenFailedAfterAllowed",
                        "vehicleId=" + snapshot.VehicleId +
                        " | citizenId=" + snapshot.CitizenId +
                        " | isVisitor=" + snapshot.IsVisitor +
                        " | source=" + (snapshot.Source ?? "NULL") +
                        " | candidates=" + snapshot.CandidateChecks +
                        " | denied=" + snapshot.DeniedCount +
                        " | allowed=" + snapshot.AllowedCount +
                        " | lastReason=" + (snapshot.LastReason ?? "NULL") +
                        " | lastBuildingId=" + snapshot.LastBuildingId +
                        " | lastPrefab=" + (snapshot.LastPrefab ?? "NULL") +
                        " | lastBuildingName=" + (snapshot.LastBuildingName ?? "NULL"));
                }
                MaybeLogSummary();
                return;
            }

            MaybeLogSummary();
        }

        private static string FormatDriverInfo(ref CitizenInstance driverInstance)
        {
            try
            {
                uint citizenId = driverInstance.m_citizen;
                if (citizenId == 0u)
                    return " | driverCitizen=0";

                ref Citizen citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];
                bool isTourist = (citizen.m_flags & Citizen.Flags.Tourist) != 0;
                return " | driverCitizen=" + citizenId +
                       " | driverTourist=" + isTourist +
                       " | driverHome=" + citizen.m_homeBuilding +
                       " | driverWork=" + citizen.m_workBuilding +
                       " | instSrc=" + driverInstance.m_sourceBuilding +
                       " | instDst=" + driverInstance.m_targetBuilding +
                       " | instFlags=" + driverInstance.m_flags;
            }
            catch
            {
                return " | driverCitizen=ERR";
            }
        }

        private static bool TryGetDriverTourist(ref CitizenInstance driverInstance, out bool isTourist)
        {
            isTourist = false;
            try
            {
                uint citizenId = driverInstance.m_citizen;
                if (citizenId == 0u)
                    return false;

                ref Citizen citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];
                isTourist = (citizen.m_flags & Citizen.Flags.Tourist) != 0;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void AccumulateFailure(ParkingSearchContext.EpisodeSnapshot snapshot, bool driverIsTourist)
        {
            _failCount++;
            if (snapshot.CandidateChecks == 0)
            {
                _failCandidatesZero++;
                if (!driverIsTourist)
                    _failNonTouristCandidatesZero++;
            }

            if (snapshot.AllowedCount == 0 && snapshot.DeniedCount > 0)
            {
                _failAllDenied++;
                if (!driverIsTourist)
                    _failNonTouristAllDenied++;
            }

            if (snapshot.AllowedCount > 0)
                _failAllowedButFailed++;
        }

        private static void MaybeLogSummary()
        {
            float now = Time.realtimeSinceStartup;
            if (_nextSummaryTime <= 0f)
                _nextSummaryTime = now + SummaryIntervalSeconds;

            if (now < _nextSummaryTime)
                return;

            if (_failCount > 0 && Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
            {
                Log.Dev.Info(
                    DebugLogCategory.Tmpe,
                    LogPath.TMPE,
                    "FindParkingSpaceForCitizenFailureSummary",
                    "total=" + _failCount +
                    " | candidatesZero=" + _failCandidatesZero +
                    " | allDenied=" + _failAllDenied +
                    " | allowedPositive=" + _failAllowedButFailed +
                    " | nonTouristCandidatesZero=" + _failNonTouristCandidatesZero +
                    " | nonTouristAllDenied=" + _failNonTouristAllDenied);
            }

            _failCount = 0;
            _failCandidatesZero = 0;
            _failAllDenied = 0;
            _failAllowedButFailed = 0;
            _failNonTouristCandidatesZero = 0;
            _failNonTouristAllDenied = 0;
            _nextSummaryTime = now + SummaryIntervalSeconds;
        }

        private static void MaybeLogFailedAttempts(object extDriverInstanceObj, ref CitizenInstance driverInstance, ushort vehicleId)
        {
            if (!Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                return;

            if (extDriverInstanceObj == null)
                return;

            if (!TryGetExtInstanceFields(extDriverInstanceObj, out uint instanceId, out int failedAttempts, out object pathMode))
                return;

            int last;
            if (_lastFailedAttemptsByInstance.TryGetValue(instanceId, out last) && last == failedAttempts)
                return;

            _lastFailedAttemptsByInstance[instanceId] = failedAttempts;

            uint citizenId = driverInstance.m_citizen;
            string pathModeText = pathMode != null ? pathMode.ToString() : "NULL";
            Log.Dev.Info(
                DebugLogCategory.Tmpe,
                LogPath.TMPE,
                "ParkingAttemptsChanged",
                "instanceId=" + instanceId +
                " | citizenId=" + citizenId +
                " | vehicleId=" + vehicleId +
                " | failedAttempts=" + failedAttempts +
                " | pathMode=" + pathModeText);
        }

        private static bool TryGetExtInstanceFields(
            object extDriverInstanceObj,
            out uint instanceId,
            out int failedAttempts,
            out object pathMode)
        {
            instanceId = 0;
            failedAttempts = 0;
            pathMode = null;

            if (_extInstanceType == null || _extInstanceType != extDriverInstanceObj.GetType())
            {
                _extInstanceType = extDriverInstanceObj.GetType();
                _extInstanceIdField = _extInstanceType.GetField("instanceId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _extFailedAttemptsField = _extInstanceType.GetField("failedParkingAttempts", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _extPathModeField = _extInstanceType.GetField("pathMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (_extInstanceIdField == null || _extFailedAttemptsField == null)
            {
                LogMissingExtFields(extDriverInstanceObj.GetType());
                return false;
            }

            try
            {
                object instanceRaw = _extInstanceIdField.GetValue(extDriverInstanceObj);
                object attemptsRaw = _extFailedAttemptsField.GetValue(extDriverInstanceObj);
                instanceId = Convert.ToUInt32(instanceRaw);
                failedAttempts = Convert.ToInt32(attemptsRaw);
                if (_extPathModeField != null)
                    pathMode = _extPathModeField.GetValue(extDriverInstanceObj);
                return true;
            }
            catch
            {
                LogMissingExtFields(extDriverInstanceObj.GetType());
                return false;
            }
        }

        private static bool TryGetExtParkingLocation(
            object extDriverInstanceObj,
            out string location,
            out ushort locationId)
        {
            location = "UNKNOWN";
            locationId = 0;

            if (extDriverInstanceObj == null)
                return false;

            try
            {
                Type type = extDriverInstanceObj.GetType();
                FieldInfo locationField = type.GetField("parkingSpaceLocation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo locationIdField = type.GetField("parkingSpaceLocationId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (locationField == null || locationIdField == null)
                    return false;

                object locationValue = locationField.GetValue(extDriverInstanceObj);
                if (locationValue != null)
                    location = locationValue.ToString();

                object locationIdValue = locationIdField.GetValue(extDriverInstanceObj);
                if (locationIdValue is ushort id)
                    locationId = id;

                return locationId != 0;
            }
            catch
            {
                return false;
            }
        }

        private static void LogMissingExtFields(Type type)
        {
            if (!Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                return;

            if (!_extInstanceFieldWarned.Add(type))
                return;

            var sb = new StringBuilder();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(fields[i].Name);
                if (sb.Length > 400)
                {
                    sb.Append("...");
                    break;
                }
            }

            Log.Dev.Info(
                DebugLogCategory.Tmpe,
                LogPath.TMPE,
                "ExtCitizenInstanceFieldsMissing",
                "type=" + type.FullName + " | fields=[" + sb + "]");
        }
    }
}
