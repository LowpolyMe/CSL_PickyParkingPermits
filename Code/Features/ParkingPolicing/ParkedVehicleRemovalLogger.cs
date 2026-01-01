using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;
using PickyParking.Logging;
using PickyParking.Features.ParkingPolicing.Runtime;

namespace PickyParking.Features.ParkingPolicing
{
    internal static class ParkedVehicleRemovalLogger
    {
        private const ushort DebugLotBuildingId = 27392;
        private const float MaxSnapDistanceSqr = 4f;
        [ThreadStatic] private static List<Vector3> _spacePositions;

        public static void LogIfMatchesLot(ushort parkedVehicleId, ushort buildingId, string source)
        {
            if (buildingId != DebugLotBuildingId)
                return;

            float lotDistSqr = 0f;
            int lotSpaceCount = 0;
            bool lotMatched = TryGetParkedPosition(parkedVehicleId, out var pos) &&
                              TryMatchLot(pos, out lotDistSqr, out lotSpaceCount);

            LogRemoval(parkedVehicleId, buildingId, source, lotMatched, lotDistSqr, lotSpaceCount);
        }

        public static void LogIfNearDebugLot(ushort parkedVehicleId, string source)
        {
            if (!TryGetParkedPosition(parkedVehicleId, out var pos))
                return;

            float lotDistSqr;
            int lotSpaceCount;
            bool lotMatched = TryMatchLot(pos, out lotDistSqr, out lotSpaceCount);
            if (!lotMatched)
                return;

            LogRemoval(parkedVehicleId, DebugLotBuildingId, source, lotMatched, lotDistSqr, lotSpaceCount);
        }

        private static bool TryGetParkedPosition(ushort parkedVehicleId, out Vector3 position)
        {
            position = default;
            if (parkedVehicleId == 0) return false;

            ref VehicleParked pv =
                ref Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer[parkedVehicleId];

            position = pv.m_position;
            return true;
        }

        private static bool TryMatchLot(Vector3 position, out float distSqr, out int lotSpaceCount)
        {
            distSqr = 0f;
            lotSpaceCount = 0;

            var context = ParkingRuntimeContext.Current;
            if (context == null || context.GameAccess == null)
                return false;

            List<Vector3> spaces = GetSpacePositions();
            if (!context.GameAccess.TryCollectParkingSpacePositions(DebugLotBuildingId, spaces))
                return false;

            lotSpaceCount = spaces.Count;
            float best = float.MaxValue;
            for (int i = 0; i < spaces.Count; i++)
            {
                Vector3 p = spaces[i];
                float dx = p.x - position.x;
                float dz = p.z - position.z;
                float d = dx * dx + dz * dz;
                if (d < best)
                    best = d;
            }

            distSqr = best;
            return best <= MaxSnapDistanceSqr;
        }

        private static List<Vector3> GetSpacePositions()
        {
            if (_spacePositions == null)
                _spacePositions = new List<Vector3>(64);
            return _spacePositions;
        }

        private static void LogRemoval(
            ushort parkedVehicleId,
            ushort buildingId,
            string source,
            bool lotMatched,
            float lotDistSqr,
            int lotSpaceCount)
        {
            try
            {
                if (parkedVehicleId == 0) return;

                ref VehicleParked pv =
                    ref Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer[parkedVehicleId];

                string prefabName = pv.Info != null ? pv.Info.name : "NULL_INFO";
                Vector3 pos = pv.m_position;
                uint ownerCitizenId = pv.m_ownerCitizen;
                ushort ownerInstance = 0;
                ushort homeId = 0;
                ushort workId = 0;
                string citizenFlags = "NONE";

                if (ownerCitizenId != 0)
                {
                    ref Citizen citizen =
                        ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[ownerCitizenId];
                    ownerInstance = citizen.m_instance;
                    homeId = citizen.m_homeBuilding;
                    workId = citizen.m_workBuilding;
                    citizenFlags = citizen.m_flags.ToString();
                }

                Log.Warn(
                    "[Parking] Parked vehicle removed " +
                    $"source={source} buildingId={buildingId} parkedId={parkedVehicleId} " +
                    $"flags={pv.m_flags} prefab={prefabName} ownerCitizen={ownerCitizenId} " +
                    $"ownerInstance={ownerInstance} homeId={homeId} workId={workId} " +
                    $"citizenFlags={citizenFlags} " +
                    $"pos=({pos.x:F1},{pos.y:F1},{pos.z:F1}) " +
                    $"lotMatch={lotMatched} lotDistSqr={lotDistSqr:F2} lotSpaces={lotSpaceCount}"
                );
            }
            catch (Exception ex)
            {
                Log.Error("[Parking] Parked vehicle removal logging failed\n" + ex);
            }
        }
    }
}
