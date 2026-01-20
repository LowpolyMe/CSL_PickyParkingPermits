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
                if (Log.IsVerboseEnabled && Log.IsDecisionDebugEnabled)
                    Log.Info(DebugLogCategory.DecisionPipeline, $"[Runtime] Candidate decision skipped: runtime inactive buildingId={buildingId}");
                return false;
            }

            if (!IsInScope(context, buildingId))
                return false;



            DecisionReason reason;
            if (!context.CandidateDecisionPipeline.TryDenyCandidateBuilding(buildingId, out denied, out reason))
                return false;

            if (Log.IsVerboseEnabled &&
                Log.IsDecisionDebugEnabled &&
                context.ParkingRulesConfigRegistry.TryGet(buildingId, out var rule))
            {
                Log.Info(DebugLogCategory.DecisionPipeline,
                    "event=CandidateDecision " + 
                    $"buildingId={buildingId} denied={denied} reason={reason} " +
                    $"isVisitor={ParkingSearchContext.IsVisitor} " +
                    $"vehicleId={ParkingSearchContext.VehicleId} citizenId={ParkingSearchContext.CitizenId} " +
                    $"source={ParkingSearchContext.Source ?? "NULL"} " +
                    $"rule=ResidentsOnly={rule.ResidentsWithinRadiusOnly} ({rule.ResidentsRadiusMeters}m), " +
                    $"WorkSchoolOnly={rule.WorkSchoolWithinRadiusOnly} ({rule.WorkSchoolRadiusMeters}m), " +
                    $"VisitorsAllowed={rule.VisitorsAllowed}"
                );
            }
            
            string prefabName = GetBuildingPrefabName(buildingId);
            string buildingName = GetBuildingCustomName(buildingId);
            ParkingSearchContext.RecordCandidate(denied, reason.ToString(), buildingId, prefabName, buildingName);
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
                if (Log.IsVerboseEnabled && Log.IsDecisionDebugEnabled)
                    Log.Info(DebugLogCategory.DecisionPipeline, "[Decision] CreateParkedVehicle check skipped: no parking search context");
                ParkingStatsCounter.IncrementCreateCheckNoContext();
                return false;
            }

            if (ownerCitizenId == 0u)
            {
                if (Log.IsVerboseEnabled && Log.IsDecisionDebugEnabled)
                    Log.Info(DebugLogCategory.DecisionPipeline, "[Decision] CreateParkedVehicle check skipped: ownerCitizenId=0");
                ParkingStatsCounter.IncrementCreateCheckNoOwner();
                return false;
            }

            if (!TryFindRuleBuildingAtPosition(context, position, out ushort buildingId, out ParkingRulesConfigDefinition rule))
            {
                if (Log.IsVerboseEnabled && Log.IsDecisionDebugEnabled)
                    Log.Info(DebugLogCategory.DecisionPipeline, $"[Decision] CreateParkedVehicle check skipped: no rule building at position ({position.x:F1},{position.y:F1},{position.z:F1})");
                ParkingStatsCounter.IncrementCreateCheckNoRuleBuilding();
                return false;
            }

            var eval = context.ParkingPermissionEvaluator.EvaluateCitizen(ownerCitizenId, buildingId);
            if (eval.Allowed)
                return false;

            if (Log.IsVerboseEnabled &&
                Log.IsDecisionDebugEnabled)
            {
                Log.Info(DebugLogCategory.DecisionPipeline,
                    "event=CreateParkedVehicleBlock " +
                    $"buildingId={buildingId} reason={eval.Reason} " +
                    $"isVisitor={ParkingSearchContext.IsVisitor} " +
                    $"vehicleId={ParkingSearchContext.VehicleId} citizenId={ParkingSearchContext.CitizenId} " +
                    $"source={ParkingSearchContext.Source ?? "NULL"} " +
                    $"rule=ResidentsOnly={rule.ResidentsWithinRadiusOnly} ({rule.ResidentsRadiusMeters}m), " +
                    $"WorkSchoolOnly={rule.WorkSchoolWithinRadiusOnly} ({rule.WorkSchoolRadiusMeters}m), " +
                    $"VisitorsAllowed={rule.VisitorsAllowed}"
                );
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

            if (Interlocked.Exchange(ref _wrongThreadLogged, 1) == 0)
            {
                Log.AlwaysWarn("[Threading] ParkingCandidateBlocker accessed off simulation thread; caller=" + (caller ?? "UNKNOWN"));
            }

            return false;
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
