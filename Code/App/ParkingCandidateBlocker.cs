using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;
using PickyParking.Domain;
using PickyParking.Infrastructure;
using PickyParking.Infrastructure.Integration;

namespace PickyParking.App
{
    
    
    
    public static class ParkingCandidateBlocker
    {
        private const float MaxSnapDistanceSqr = 4f; 
        [ThreadStatic] private static List<Vector3> _spacePositions;

        public static bool HandleFindParkingSpacePropAtBuildingPrefix(ref bool result, object[] args)
        {
            try
            {
                if (args == null || args.Length < 12)
                    return true;

                
                if (!(args[3] is ushort))
                    return true;

                ushort buildingId = (ushort)args[3];

                var context = ParkingRuntimeContext.GetCurrentOrLog("ParkingCandidateBlocker.HandleFindParkingSpacePropAtBuildingPrefix");
                if (context == null || context.TmpeIntegration == null || !context.FeatureGate.IsActive)
                    return true;

                
                if (!IsInScope(context, buildingId))
                    return true;

                string prefabName = GetBuildingPrefabName(buildingId);

                bool denied = context.TmpeIntegration.TryDenyBuildingParkingCandidate(buildingId, out DecisionReason reason);

                if (Log.IsVerboseEnabled &&
                    context.ParkingRestrictionsConfigRegistry.TryGet(buildingId, out var rule) &&
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
                if (!denied) return true;

                
                args[9] = Vector3.zero;
                args[10] = Quaternion.identity;
                args[11] = -1f;

                result = false;
                return false; 
            }
            catch (Exception ex)
            {
                Log.Error("[Parking] Exception\n" + ex);
                return true; 
            }
        }

        public static bool HandleCreateParkedVehiclePrefix(
            ref ushort parked,
            ref ColossalFramework.Math.Randomizer r,
            VehicleInfo info,
            Vector3 position,
            Quaternion rotation,
            uint ownerCitizen,
            ref bool result)
        {
            try
            {
                var context = ParkingRuntimeContext.GetCurrentOrLog("ParkingCandidateBlocker.HandleCreateParkedVehiclePrefix");
                if (context == null || !context.FeatureGate.IsActive)
                    return true;

                if (!ParkingSearchContext.HasContext)
                    return true;

                if (ownerCitizen == 0u)
                    return true;

                if (!TryFindRuleBuildingAtPosition(context, position, out ushort buildingId, out ParkingRestrictionsConfigDefinition rule))
                    return true;

                var eval = context.ParkingPermissionEvaluator.EvaluateCitizen(ownerCitizen, buildingId);
                if (eval.Allowed)
                    return true;

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

                parked = 0;
                result = false;
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("[Parking] Prefix exception\n" + ex);
            }

            return true;
        }

        private static bool TryFindRuleBuildingAtPosition(
            ParkingRuntimeContext context,
            Vector3 position,
            out ushort buildingId,
            out ParkingRestrictionsConfigDefinition rule)
        {
            buildingId = 0;
            rule = default;

            var spacePositions = GetSpacePositionsBuffer();

            foreach (var kvp in context.ParkingRestrictionsConfigRegistry.Enumerate())
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

            var key = services.PrefabIdentity.CreateKey(info);
            return services.SupportedParkingLotRegistry.Contains(key);
        }

        private static bool IsInScope(ParkingRuntimeContext services, ushort buildingId)
        {
            if (services == null) return false;
            if (!services.GameAccess.TryGetBuildingInfo(buildingId, out var info)) return false;

            var key = services.PrefabIdentity.CreateKey(info);
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
