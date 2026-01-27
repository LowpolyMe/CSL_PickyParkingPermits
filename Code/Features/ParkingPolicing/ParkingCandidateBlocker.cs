using System;
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
        private static readonly TimeSpan DecisionLogInterval = TimeSpan.FromSeconds(2);

        public static bool TryGetCandidateDecision(ushort buildingId, out bool denied)
        {
            denied = false;

            if (ParkingDebugSettings.DisableParkingEnforcement)
                return false;

            if (!EnsureSimulationThread("TryGetCandidateDecision"))
                return false;

            ParkingRuntimeContext context = ParkingRuntimeContext.GetCurrentOrLog("ParkingCandidateBlocker.TryGetCandidateDecision");
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

            ParkingRulesConfigDefinition rule;
            if (Log.Dev.IsEnabled(DebugLogCategory.DecisionPipeline) &&
                context.ParkingRulesConfigRegistry.TryGet(buildingId, out rule))
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
                    " | visitorsAllowed=" + rule.VisitorsAllowed,
                    "DecisionPipeline.CandidateDecision",
                    DecisionLogInterval);
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

            ParkingRuntimeContext context = ParkingRuntimeContext.GetCurrentOrLog("ParkingCandidateBlocker.TryGetRuleBuildingAtPosition");
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

            ParkingRuntimeContext context = ParkingRuntimeContext.GetCurrentOrLog("ParkingCandidateBlocker.ShouldBlockCreateParkedVehicle");
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

            ushort buildingId;
            ParkingRulesConfigDefinition rule;
            if (!TryFindRuleBuildingAtPosition(context, position, out buildingId, out rule))
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

            ParkingPermissionEvaluator.Result eval = context.ParkingPermissionEvaluator.EvaluateCitizen(ownerCitizenId, buildingId);
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
                    " | visitorsAllowed=" + rule.VisitorsAllowed,
                    "DecisionPipeline.CreateParkedVehicleBlocked",
                    DecisionLogInterval);
            }

            return true;
        }

        private static bool TryFindRuleBuildingAtPosition(
            ParkingRuntimeContext context,
            Vector3 position,
            out ushort buildingId,
            out ParkingRulesConfigDefinition rule)
        {
            RuleLotSpatialIndex.RuleLotQuery query = new RuleLotSpatialIndex.RuleLotQuery
            {
                Context = context,
                Position = position,
                MaxSnapDistanceSqr = MaxSnapDistanceSqr
            };

            RuleLotSpatialIndex.RuleLotQueryResult result;
            if (!_ruleLotSpatialIndex.TryFindBuilding(query, out result))
            {
                buildingId = 0;
                rule = default(ParkingRulesConfigDefinition);
                return false;
            }

            buildingId = result.BuildingId;
            rule = result.Rule;
            return true;
        }

        public static void ClearThreadStatic()
        {
            _ruleLotSpatialIndex.Clear();
        }

        private static bool EnsureSimulationThread(string caller)
        {
            if (SimThread.IsSimulationThread())
                return true;

            if (Log.Dev.IsEnabled(DebugLogCategory.DecisionPipeline))
            {
                Log.Dev.Warn(
                    DebugLogCategory.DecisionPipeline,
                    LogPath.Any,
                    "OffSimulationThread",
                    "caller=" + (caller ?? "UNKNOWN"),
                    "ParkingCandidateBlocker.OffSimThread");
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
            BuildingInfo info;
            if (!services.GameAccess.TryGetBuildingInfo(buildingId, out info)) return false;

            PrefabKey key = ParkingLotPrefabKeyFactory.CreateKey(info);
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
            catch (Exception ex)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.DecisionPipeline))
                {
                    Log.Dev.Warn(
                        DebugLogCategory.DecisionPipeline,
                        LogPath.Any,
                        "BuildingPrefabNameLookupFailed",
                        "error=" + ex,
                        "ParkingCandidateBlocker.GetBuildingPrefabName",
                        TimeSpan.FromMinutes(5));
                }
                return "NAME_LOOKUP_FAILED";
            }
        }

        private static string GetBuildingCustomName(ushort buildingId)
        {
            try
            {
                BuildingManager bm = Singleton<BuildingManager>.instance;
                string name = bm.GetBuildingName(buildingId, default(InstanceID));
                return !string.IsNullOrEmpty(name) ? name : "NONE";
            }
            catch (Exception ex)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.DecisionPipeline))
                {
                    Log.Dev.Warn(
                        DebugLogCategory.DecisionPipeline,
                        LogPath.Any,
                        "BuildingCustomNameLookupFailed",
                        "error=" + ex,
                        "ParkingCandidateBlocker.GetBuildingCustomName",
                        TimeSpan.FromMinutes(5));
                }
                return "NAME_LOOKUP_FAILED";
            }
        }
    }
}
