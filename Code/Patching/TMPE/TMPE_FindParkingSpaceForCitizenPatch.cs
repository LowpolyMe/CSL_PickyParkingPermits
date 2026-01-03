using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using ColossalFramework;
using HarmonyLib;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Features.ParkingPolicing.Runtime;
using PickyParking.Features.ParkingRules;
using UnityEngine;
using PickyParking.Logging;

namespace PickyParking.Patching.TMPE
{
    internal static class TMPE_FindParkingSpaceForCitizenPatch
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
        private static int _overrideTouristAllow;
        private static float _nextSummaryTime;
        private static bool _touristOverrideWarned;
        private static readonly Dictionary<uint, int> _lastFailedAttemptsByInstance = new Dictionary<uint, int>();
        private static FieldInfo _extInstanceIdField;
        private static FieldInfo _extFailedAttemptsField;
        private static FieldInfo _extPathModeField;
        private static Type _extInstanceType;
        private static readonly HashSet<Type> _extInstanceFieldWarned = new HashSet<Type>();

        public static void Apply(Harmony harmony)
        {
            var type = Type.GetType(TargetTypeName, throwOnError: false);
            if (type == null)
            {
                Log.Error("[TMPE] AdvancedParkingManager not found; skipping FindParkingSpaceForCitizen patch.");
                return;
            }

            MethodInfo method = FindTargetMethod(type);
            if (method == null)
            {
                Log.Error("[TMPE] FindParkingSpaceForCitizen overload not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(TMPE_FindParkingSpaceForCitizenPatch), nameof(Prefix)),
                postfix: new HarmonyMethod(typeof(TMPE_FindParkingSpaceForCitizenPatch), nameof(Postfix)),
                finalizer: new HarmonyMethod(typeof(TMPE_FindParkingSpaceForCitizenPatch), nameof(Finalizer))
            );

            Log.Info("[TMPE] Patched FindParkingSpaceForCitizen.");
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
            [HarmonyArgument(6)] ushort vehicleId,
            [HarmonyArgument(7)] ref bool allowTourists,
            ref bool __state)
        {
            MaybeLogFailedAttempts(extDriverInstanceObj, ref driverInstance, vehicleId);

            if (TryGetDriverTourist(ref driverInstance, out bool isTourist) && isTourist)
            {
                if (!allowTourists)
                {
                    allowTourists = true;
                    _overrideTouristAllow++;
                    if (!_touristOverrideWarned)
                    {
                        _touristOverrideWarned = true;
                        Log.Warn("[TMPE] Tourist allow override active (debug). Remove after investigation to restore default TM:PE behavior.");
                    }
                }
            }

            ParkingSearchContextPatchHandler.BeginFindParkingForCitizen(ref driverInstance, vehicleId, ref __state);
        }

        private static Exception Finalizer(Exception __exception, bool __state)
        {
            return ParkingSearchContextPatchHandler.EndFindParkingForCitizen(__exception, __state);
        }

        private static void Postfix(
            bool __result,
            [HarmonyArgument(0)] Vector3 endPos,
            [HarmonyArgument(2)] ref CitizenInstance driverInstance,
            [HarmonyArgument(4)] ushort homeId,
            [HarmonyArgument(5)] bool goingHome,
            [HarmonyArgument(6)] ushort vehicleId,
            [HarmonyArgument(7)] bool allowTourists)
        {
            if (__result)
                return;

            if (!Log.IsVerboseEnabled || !Log.IsTmpeDebugEnabled)
                return;

            TryGetDriverTourist(ref driverInstance, out bool driverIsTourist);

            if (ParkingSearchContext.TryGetEpisodeSnapshot(out var snapshot))
            {
                AccumulateFailure(snapshot, driverIsTourist);
                bool isAllDenied = snapshot.AllowedCount == 0 && snapshot.DeniedCount > 0;
                bool isCandidateZero = snapshot.CandidateChecks == 0;
                bool shouldLogDetail = !driverIsTourist && (isAllDenied || isCandidateZero);
                if (shouldLogDetail)
                {
                    string allDeniedNote = isAllDenied ? " allCandidatesDenied=true" : string.Empty;
                    Log.Info(
                        "[TMPE] FindParkingSpaceForCitizen failed (no parking found). " +
                        $"vehicleId={snapshot.VehicleId} citizenId={snapshot.CitizenId} isVisitor={snapshot.IsVisitor} " +
                        $"source={snapshot.Source ?? "NULL"} candidates={snapshot.CandidateChecks} " +
                        $"denied={snapshot.DeniedCount} allowed={snapshot.AllowedCount} " +
                        $"last=({snapshot.LastReason ?? "NULL"} bld={snapshot.LastBuildingId} prefab={snapshot.LastPrefab ?? "NULL"} name={snapshot.LastBuildingName ?? "NULL"})" +
                        allDeniedNote +
                        (isCandidateZero
                            ? $" endPos=({endPos.x:F1},{endPos.y:F1},{endPos.z:F1}) homeId={homeId} goingHome={goingHome} vehicleArg={vehicleId} allowTourists={allowTourists}{FormatDriverInfo(ref driverInstance)}"
                        : string.Empty));
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
                    return " driverCitizen=0";

                ref Citizen citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];
                bool isTourist = (citizen.m_flags & Citizen.Flags.Tourist) != 0;
                return $" driverCitizen={citizenId} driverTourist={isTourist} driverHome={citizen.m_homeBuilding} driverWork={citizen.m_workBuilding} instSrc={driverInstance.m_sourceBuilding} instDst={driverInstance.m_targetBuilding} instFlags={driverInstance.m_flags}";
            }
            catch
            {
                return " driverCitizen=ERR";
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

            if (_failCount > 0 && Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled)
            {
                Log.Info(
                    "[TMPE] Parking search failures (summary) " +
                    $"total={_failCount} cand0={_failCandidatesZero} allDenied={_failAllDenied} allowed>0={_failAllowedButFailed} " +
                    $"nonTouristCand0={_failNonTouristCandidatesZero} nonTouristAllDenied={_failNonTouristAllDenied} " +
                    $"touristOverride={_overrideTouristAllow}");
            }
            
            _failCount = 0; 
            _failCandidatesZero = 0;
            _failAllDenied = 0;
            _failAllowedButFailed = 0;
            _failNonTouristCandidatesZero = 0;
            _failNonTouristAllDenied = 0;
            _overrideTouristAllow = 0;
            _nextSummaryTime = now + SummaryIntervalSeconds;
        }

        private static void MaybeLogFailedAttempts(object extDriverInstanceObj, ref CitizenInstance driverInstance, ushort vehicleId)
        {
            if (!Log.IsVerboseEnabled || !Log.IsTmpeDebugEnabled)
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
            Log.Info(
                "[TMPE] Parking attempts changed " +
                $"instanceId={instanceId} citizenId={citizenId} vehicleId={vehicleId} " +
                $"failedAttempts={failedAttempts} pathMode={pathModeText}"
            );
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

        private static void LogMissingExtFields(Type type)
        {
            if (!Log.IsVerboseEnabled || !Log.IsTmpeDebugEnabled)
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

            Log.Info($"[TMPE] ExtCitizenInstance fields missing or unreadable. type={type.FullName} fields=[{sb}]");
        }

        private static string FormatRuleInfo(ushort buildingId)
        {
            if (buildingId == 0)
                return string.Empty;

            var context = ParkingRuntimeContext.GetCurrentOrLog("TMPE_FindParkingSpaceForCitizenPatch.FormatRuleInfo");
            if (context == null || context.ParkingRulesConfigRegistry == null)
                return "rule=<missing> ";

            if (!context.ParkingRulesConfigRegistry.TryGet(buildingId, out var rule))
                return "rule=<none> ";

            return $"rule=ResidentsOnly={rule.ResidentsWithinRadiusOnly} ({rule.ResidentsRadiusMeters}m), " +
                   $"WorkSchoolOnly={rule.WorkSchoolWithinRadiusOnly} ({rule.WorkSchoolRadiusMeters}m), " +
                   $"VisitorsAllowed={rule.VisitorsAllowed} ";
        }
    }
}
