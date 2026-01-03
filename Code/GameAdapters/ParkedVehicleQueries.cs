using ColossalFramework;
using UnityEngine;

namespace PickyParking.GameAdapters
{
    internal sealed class ParkedVehicleQueries
    {
        private const int ParkedGridSafetyLimit = 32768;

        public bool TryGetParkedVehicleInfo(
            ushort parkedVehicleId,
            out uint ownerCitizenId,
            out ushort homeId,
            out Vector3 position)
        {
            ownerCitizenId = 0;
            homeId = 0;
            position = default;

            if (parkedVehicleId == 0) return false;

            ref VehicleParked pv =
                ref Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer[parkedVehicleId];

            if (pv.m_flags == 0) return false;

            ownerCitizenId = pv.m_ownerCitizen;
            position = pv.m_position;

            if (ownerCitizenId == 0)
                return false;

            ref Citizen citizen =
                ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[ownerCitizenId];

            homeId = citizen.m_homeBuilding;
            return true;
        }

        public bool TryFindNearestParkedVehicle(
            Vector3 position,
            float searchRadius,
            out ushort parkedVehicleId,
            out float distance,
            out float distance3d,
            out Vector3 parkedPos)
        {
            parkedVehicleId = 0;
            distance = 0f;
            distance3d = 0f;
            parkedPos = default;

            if (searchRadius <= 0f) return false;

            float searchRadiusSqr = searchRadius * searchRadius;
            int baseX = Mathf.Clamp((int)(position.x / 32f + 270f), 0, 539);
            int baseZ = Mathf.Clamp((int)(position.z / 32f + 270f), 0, 539);

            var vm = Singleton<VehicleManager>.instance;
            float bestDistSqr = float.MaxValue;
            ushort bestId = 0;

            for (int dz = -1; dz <= 1; dz++)
            {
                int gz = baseZ + dz;
                if (gz < 0 || gz > 539) continue;

                for (int dx = -1; dx <= 1; dx++)
                {
                    int gx = baseX + dx;
                    if (gx < 0 || gx > 539) continue;

                    ushort parkedId = vm.m_parkedGrid[gz * 540 + gx];
                    int safety = 0;

                    while (parkedId != 0)
                    {
                        ref VehicleParked pv = ref vm.m_parkedVehicles.m_buffer[parkedId];
                        ushort next = pv.m_nextGridParked;

                        if (pv.m_flags != 0)
                        {
                            float dxp = pv.m_position.x - position.x;
                            float dzp = pv.m_position.z - position.z;
                            float distSqr = dxp * dxp + dzp * dzp;
                            if (distSqr <= searchRadiusSqr && distSqr < bestDistSqr)
                            {
                                bestDistSqr = distSqr;
                                bestId = parkedId;
                            }
                        }

                        parkedId = next;

                        if (++safety > ParkedGridSafetyLimit)
                            break;
                    }
                }
            }

            if (bestId == 0)
                return false;

            parkedVehicleId = bestId;
            distance = Mathf.Sqrt(bestDistSqr);
            ref VehicleParked best = ref Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer[bestId];
            parkedPos = best.m_position;
            float dy = best.m_position.y - position.y;
            distance3d = Mathf.Sqrt(bestDistSqr + dy * dy);
            return true;
        }
    }
}
