using System;
using System.Threading;
using ColossalFramework;
using UnityEngine;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.Features.ParkingRules;
using PickyParking.Features.Debug;
using PickyParking.Logging;
using PickyParking.ModLifecycle;
using PickyParking.Features.ParkingPolicing.Runtime;
using PickyParking.Settings;

namespace PickyParking.Features.ParkingPolicing
{
    public static class ParkingCandidateBlocker
    {
        private const float MaxSnapDistanceSqr = 4f;
        private static readonly RuleLotSpatialIndex _ruleLotSpatialIndex = new RuleLotSpatialIndex();
        private static int _wrongThreadLogged;

        public static bool TryGetCandidateDecision(ushort buildingId, out bool denied)
        {
            //TODO Only compute prefabName/buildingName inside the debug logging block(s). Also don’t swallow lookup exceptions—log once and degrade gracefully.
            denied = false;

            if (ParkingDebugSettings.DisableParkingEnforcement)
                return false;

            if (!EnsureSimulationThread("TryGetCandidateDecision"))
                return false;

            var context = ParkingRuntimeContext.GetCurrentOrLog("ParkingCandidateBlocker.TryGetCandidateDecision");
            if (context == null || !context.FeatureGate.IsActive || context.CandidateDecisionPipeline == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.DecisionPipeline))
                {
                    Log.Dev.Info(DebugLogCategory.DecisionPipeline, LogPath.Any, "CandidateDecisionSkippedRuntimeInactive", "buildingId=" + buildingId);
                }
                return false;
            }

            if (!IsInScope(context, buildingId))
                return false;



            DecisionReason reason;
            if (!context.CandidateDecisionPipeline.TryDenyCandidateBuilding(buildingId, out denied, out reason))
                return false;

            if (Log.Dev.IsEnabled(DebugLogCategory.DecisionPipeline) &&
                context.ParkingRulesConfigRegistry.TryGet(buildingId, out var rule))
            {
                Log.Dev.Info(
                    DebugLogCategory.DecisionPipeline,
                    GetPathForSource(ParkingSearchContext.Source),
                    "CandidateDecision",
                    "buildingId=" + buildingId +
                    " | denied=" + denied +
                    " | reason=" + reason +
                    " | isVisitor=" + ParkingSearchContext.IsVisitor +
                    " | vehicleId=" + ParkingSearchContext.VehicleId +
                    " | citizenId=" + ParkingSearchContext.CitizenId +
                    " | source=" + (ParkingSearchContext.Source ?? "NULL") +
                    " | residentsOnly=" + rule.ResidentsWithinRadiusOnly +
                    " | residentsRadiusMeters=" + rule.ResidentsRadiusMeters +
                    " | workSchoolOnly=" + rule.WorkSchoolWithinRadiusOnly +
                    " | workSchoolRadiusMeters=" + rule.WorkSchoolRadiusMeters +
                    " | visitorsAllowed=" + rule.VisitorsAllowed);
            }
            
            if (ParkingSearchContext.EnableEpisodeLogs && Log.Dev.IsEnabled(DebugLogCategory.DecisionPipeline))
            {
                string prefabName = GetBuildingPrefabName(buildingId);
                string buildingName = GetBuildingCustomName(buildingId);
                ParkingSearchContext.RecordCandidate(denied, reason.ToString(), buildingId, prefabName, buildingName);
            }
            ParkingStatsCounter.IncrementCandidateDecision(denied);
            return true;
        }

        public static bool TryGetRuleBuildingAtPosition(Vector3 position, out ushort buildingId)
        {
            buildingId = 0;

            if (!EnsureSimulationThread("TryGetRuleBuildingAtPosition"))
                return false;

            var context = ParkingRuntimeContext.GetCurrentOrLog("ParkingCandidateBlocker.TryGetRuleBuildingAtPosition");
            if (context == null || !context.FeatureGate.IsActive)
                return false;

            return TryFindRuleBuildingAtPosition(context, position, out buildingId, out _);
        }

        public static bool ShouldBlockCreateParkedVehicle(uint ownerCitizenId, Vector3 position)
        {
            if (!EnsureSimulationThread("ShouldBlockCreateParkedVehicle"))
                return false;

            if (ParkingDebugSettings.DisableParkingEnforcement)
                return false;

            var context = ParkingRuntimeContext.GetCurrentOrLog("ParkingCandidateBlocker.ShouldBlockCreateParkedVehicle");
            if (context == null || !context.FeatureGate.IsActive)
                return false;

            if (!ParkingSearchContext.HasContext)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.DecisionPipeline))
                {
                    Log.Dev.Info(DebugLogCategory.DecisionPipeline, LogPath.Any, "CreateParkedVehicleSkippedNoContext");
                }
                ParkingStatsCounter.IncrementCreateCheckNoContext();
                return false;
            }

            if (ownerCitizenId == 0u)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.DecisionPipeline))
                {
                    Log.Dev.Info(DebugLogCategory.DecisionPipeline, LogPath.Any, "CreateParkedVehicleSkippedNoOwner");
                }
                ParkingStatsCounter.IncrementCreateCheckNoOwner();
                return false;
            }

            if (!TryFindRuleBuildingAtPosition(context, position, out ushort buildingId, out ParkingRulesConfigDefinition rule))
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.DecisionPipeline))
                {
                    Log.Dev.Info(
                        DebugLogCategory.DecisionPipeline,
                        LogPath.Any,
                        "CreateParkedVehicleSkippedNoRuleBuilding",
                        "posX=" + position.x.ToString("F1") +
                        " | posY=" + position.y.ToString("F1") +
                        " | posZ=" + position.z.ToString("F1"));
                }
                ParkingStatsCounter.IncrementCreateCheckNoRuleBuilding();
                return false;
            }

            var eval = context.ParkingPermissionEvaluator.EvaluateCitizen(ownerCitizenId, buildingId);
            if (eval.Allowed)
                return false;

            if (Log.Dev.IsEnabled(DebugLogCategory.DecisionPipeline))
            {
                Log.Dev.Info(
                    DebugLogCategory.DecisionPipeline,
                    GetPathForSource(ParkingSearchContext.Source),
                    "CreateParkedVehicleBlocked",
                    "buildingId=" + buildingId +
                    " | reason=" + eval.Reason +
                    " | isVisitor=" + ParkingSearchContext.IsVisitor +
                    " | vehicleId=" + ParkingSearchContext.VehicleId +
                    " | citizenId=" + ParkingSearchContext.CitizenId +
                    " | source=" + (ParkingSearchContext.Source ?? "NULL") +
                    " | residentsOnly=" + rule.ResidentsWithinRadiusOnly +
                    " | residentsRadiusMeters=" + rule.ResidentsRadiusMeters +
                    " | workSchoolOnly=" + rule.WorkSchoolWithinRadiusOnly +
                    " | workSchoolRadiusMeters=" + rule.WorkSchoolRadiusMeters +
                    " | visitorsAllowed=" + rule.VisitorsAllowed);
            }

            return true;
        }

        private static bool TryFindRuleBuildingAtPosition(
            ParkingRuntimeContext context,
            Vector3 position,
            out ushort buildingId,
            out ParkingRulesConfigDefinition rule)
        {
            return _ruleLotSpatialIndex.TryFindBuilding(
                context,
                position,
                MaxSnapDistanceSqr,
                out buildingId,
                out rule);
        }

        public static void ClearThreadStatic()
        {
            _ruleLotSpatialIndex.Clear();
        }

        private static bool EnsureSimulationThread(string caller)
        {
            if (SimThread.IsSimulationThread())
                return true;

            if (Interlocked.Exchange(ref _wrongThreadLogged, 1) == 0)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.DecisionPipeline))
                {
                    Log.Dev.Warn(
                        DebugLogCategory.DecisionPipeline,
                        LogPath.Any,
                        "OffSimulationThread",
                        "caller=" + (caller ?? "UNKNOWN"),
                        "ParkingCandidateBlocker.OffSimThread");
                }
            }

            return false;
        }

        private static LogPath GetPathForSource(string source)
        {
            if (!string.IsNullOrEmpty(source))
            {
                if (source.StartsWith("Vanilla.", StringComparison.Ordinal))
                    return LogPath.Vanilla;
                if (source.StartsWith("TMPE.", StringComparison.Ordinal))
                    return LogPath.TMPE;
            }

            return LogPath.Any;
        }

        internal static bool IsInScope(ParkingRuntimeContext services, ushort buildingId)
        {
            if (services == null) return false;
            if (!services.GameAccess.TryGetBuildingInfo(buildingId, out var info)) return false;

            var key = ParkingLotPrefabKeyFactory.CreateKey(info);
            return services.SupportedParkingLotRegistry.Contains(key);
        }

        private static string GetBuildingPrefabName(ushort buildingId)
        {
            try
            {
                ref Building b = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId];
                BuildingInfo info = b.Info;
                if (info == null) return "NULL_INFO";
                return info.name != null ? info.name : "NULL_PREFAB_NAME";
            }
            catch
            {
                return "NAME_LOOKUP_FAILED";
            }
        }

        private static string GetBuildingCustomName(ushort buildingId)
        {
            try
            {
                var bm = Singleton<BuildingManager>.instance;
                string name = bm.GetBuildingName(buildingId, default(InstanceID));
                return !string.IsNullOrEmpty(name) ? name : "NONE";
            }
            catch
            {
                return "NAME_LOOKUP_FAILED";
            }
        }
    }
}
