using System;
using PickyParking.Features.Debug;
using UnityEngine;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Logging;
using PickyParking.Settings;

namespace PickyParking.Patching
{
    internal static class ParkingCandidateBlockerPatchHandler
    {
        private static bool _noContextLogged;

        public static bool HandleFindParkingSpacePropAtBuildingPrefix(
            VehicleInfo vehicleInfo,
            ushort buildingId,
            ref Vector3 parkPos,
            ref Quaternion parkRot,
            ref float parkOffset,
            ref bool result)
        {
            try
            {
                // Design choice: without a parking search context, we intentionally block passenger-car candidates.
                if (!ParkingSearchContext.HasContext &&
                    !ParkingDebugSettings.DisableParkingEnforcement &&
                    IsPassengerCarInfo(vehicleInfo))
                {
                    if (!_noContextLogged && Log.Dev.IsEnabled(DebugLogCategory.DecisionPipeline))
                    {
                        _noContextLogged = true;
                        Log.Dev.Warn(DebugLogCategory.DecisionPipeline, LogPath.TMPE, "PropSearchMissingContext");
                    }
                    ParkingStatsCounter.IncrementPropSearchNoContextDenied();
                    result = false;
                    return false;
                }

                bool denied;
                if (!ParkingCandidateBlocker.TryGetCandidateDecision(buildingId, out denied))
                    return true;

                if (!denied)
                    return true;

                parkPos = Vector3.zero;
                parkRot = Quaternion.identity;
                parkOffset = -1f;

                result = false;
                ParkingStatsCounter.IncrementPropSearchDenied();
                return false;
            }
            catch (Exception ex)
            {
                Log.Dev.Exception(DebugLogCategory.DecisionPipeline, LogPath.TMPE, "PropSearchPrefixException", ex);
                return true;
            }
        }

        private static bool IsPassengerCarInfo(VehicleInfo info)
        {
            if (info == null || info.m_vehicleAI == null)
                return false;

            if (!(info.m_vehicleAI is PassengerCarAI))
            {
                string aiName = info.m_vehicleAI.GetType().Name ?? string.Empty;
                if (!string.Equals(aiName, "CustomPassengerCarAI", StringComparison.Ordinal))
                    return false;
            }

            return true;
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
                if (ParkingSearchContext.HasContext)
                {
                    string source = ParkingSearchContext.Source;
                    if (!string.IsNullOrEmpty(source) &&
                        source.StartsWith("TMPE.", StringComparison.Ordinal))
                    {
                        ParkingStatsCounter.IncrementCreateBypassTmpe();
                        return true;
                    }
                }

                if (!ParkingCandidateBlocker.ShouldBlockCreateParkedVehicle(ownerCitizen, position))
                    return true;

                parked = 0;
                result = false;
                ParkingStatsCounter.IncrementCreateBlocked();
                return false;
            }
            catch (Exception ex)
            {
                Log.Dev.Exception(DebugLogCategory.Enforcement, LogPath.Vanilla, "CreateParkedVehiclePrefixException", ex);
                return true;
            }
        }
    }
}
