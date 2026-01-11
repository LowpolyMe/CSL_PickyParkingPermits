using System;
using PickyParking.Features.Debug;
using UnityEngine;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Logging;

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
                if (!ParkingSearchContext.HasContext &&
                    !ParkingDebugSettings.DisableParkingEnforcement &&
                    IsPassengerCarInfo(vehicleInfo))
                {
                    if (!_noContextLogged && Log.IsVerboseEnabled && Log.IsDecisionDebugEnabled)
                    {
                        _noContextLogged = true;
                        Log.Warn("[Parking] FindParkingSpacePropAtBuilding missing context; passenger car candidates may be skipped.");
                    }
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
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("[Parking] Exception\n" + ex);
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
                if (!ParkingCandidateBlocker.ShouldBlockCreateParkedVehicle(ownerCitizen, position))
                    return true;

                parked = 0;
                result = false;
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("[Parking] Prefix exception\n" + ex);
                return true;
            }
        }
    }
}
