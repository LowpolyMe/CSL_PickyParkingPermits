using System;
using UnityEngine;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Logging;

namespace PickyParking.Patching
{
    internal static class ParkingCandidateBlockerPatchHandler
    {
        public static bool HandleFindParkingSpacePropAtBuildingPrefix(ref bool result, object[] args)
        {
            try
            {
                if (args == null || args.Length < 12)
                    return true;

                if (!(args[3] is ushort))
                    return true;

                ushort buildingId = (ushort)args[3];
                bool denied;
                if (!ParkingCandidateBlocker.TryGetCandidateDecision(buildingId, out denied))
                    return true;

                if (!denied)
                    return true;

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
