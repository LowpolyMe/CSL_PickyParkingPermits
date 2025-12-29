using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.Features.ParkingRules;
using PickyParking.Logging;
using PickyParking.ModLifecycle;
using PickyParking.Features.ParkingPolicing.Runtime;

namespace PickyParking.Features.ParkingPolicing
{
    public static class ParkingCandidateBlocker
    {
        private const float MaxSnapDistanceSqr = 4f;
        [ThreadStatic] private static List<Vector3> _spacePositions;

        public static bool TryGetCandidateDecision(ushort buildingId, out bool denied)
        {
            denied = false;

            var context = ParkingRuntimeContext.GetCurrentOrLog("ParkingCandidateBlocker.TryGetCandidateDecision");
            if (context == null || context.TmpeIntegration == null || !context.FeatureGate.IsActive)
                return false;

            if (!IsInScope(context, buildingId))
                return false;

            string prefabName = GetBuildingPrefabName(buildingId);

            DecisionReason reason;
            denied = context.TmpeIntegration.TryDenyBuildingParkingCandidate(buildingId, out reason);

            if (Log.IsVerboseEnabled &&
                context.ParkingRulesConfigRegistry.TryGet(buildingId, out var rule) &&
                rule.WorkSchoolWithinRadiusOnly)
            {
                Log.Info(
                    "[Parking] WorkerOnlyCandidateDecision " +
                    $"buildingId={buildingId} denied={denied} reason={reason} " +
                    $"isVisitor={ParkingSearchContext.IsVisitor} " +
                    $"vehicleId={ParkingSearchContext.VehicleId} citizenId={ParkingSearchContext.CitizenId} " +
                    $"source={ParkingSearchContext.Source ?? "NULL"} " +
                    $"rule=ResidentsOnly={rule.ResidentsWithinRadiusOnly} ({rule.ResidentsRadiusMeters}m), " +
                    $"WorkSchoolOnly={rule.WorkSchoolWithinRadiusOnly} ({rule.WorkSchoolRadiusMeters}m), " +
                    $"VisitorsAllowed={rule.VisitorsAllowed}"
                );
            }

            ParkingSearchContext.RecordCandidate(denied, reason.ToString(), buildingId, prefabName);
            return true;
        }

        public static bool ShouldBlockCreateParkedVehicle(uint ownerCitizenId, Vector3 position)
        {
            var context = ParkingRuntimeContext.GetCurrentOrLog("ParkingCandidateBlocker.ShouldBlockCreateParkedVehicle");
            if (context == null || !context.FeatureGate.IsActive)
                return false;

            if (!ParkingSearchContext.HasContext)
                return false;

            if (ownerCitizenId == 0u)
                return false;

            if (!TryFindRuleBuildingAtPosition(context, position, out ushort buildingId, out ParkingRulesConfigDefinition rule))
                return false;

            var eval = context.ParkingPermissionEvaluator.EvaluateCitizen(ownerCitizenId, buildingId);
            if (eval.Allowed)
                return false;

            if (Log.IsVerboseEnabled && rule.WorkSchoolWithinRadiusOnly)
            {
                Log.Info(
                    "[Parking] WorkerOnlyCreateDenied " +
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
            buildingId = 0;
            rule = default;

            var spacePositions = GetSpacePositionsBuffer();

            foreach (var kvp in context.ParkingRulesConfigRegistry.Enumerate())
            {
                if (!IsSupportedParkingLot(context, kvp.Key))
                    continue;

                spacePositions.Clear();
                if (!context.GameAccess.TryCollectParkingSpacePositions(kvp.Key, spacePositions))
                    continue;

                for (int i = 0; i < spacePositions.Count; i++)
                {
                    Vector3 spacePos = spacePositions[i];
                    float dx = spacePos.x - position.x;
                    float dz = spacePos.z - position.z;

                    if (dx * dx + dz * dz <= MaxSnapDistanceSqr)
                    {
                        buildingId = kvp.Key;
                        rule = kvp.Value;
                        return true;
                    }
                }
            }

            return false;
        }

        private static List<Vector3> GetSpacePositionsBuffer()
        {
            if (_spacePositions == null)
                _spacePositions = new List<Vector3>(64);

            return _spacePositions;
        }

        public static void ClearThreadStatic()
        {
            _spacePositions = null;
        }

        private static bool IsSupportedParkingLot(ParkingRuntimeContext services, ushort buildingId)
        {
            if (services == null) return false;
            if (!services.GameAccess.TryGetBuildingInfo(buildingId, out var info)) return false;

            var key = ParkingLotPrefabKeyFactory.CreateKey(info);
            return services.SupportedParkingLotRegistry.Contains(key);
        }

        private static bool IsInScope(ParkingRuntimeContext services, ushort buildingId)
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
    }
}
