using ColossalFramework;

namespace PickyParking.Features.ParkingPolicing
{
    internal static class CitizenIdResolver
    {
        public static bool TryGetCitizenIdFromVehicle(ushort vehicleId, out uint citizenId, out string reason)
        {
            citizenId = 0u;
            reason = null;

            if (vehicleId == 0)
            {
                reason = "vehicleId==0 (TM:PE internal / parked-relocation style search)";
                return false;
            }

            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;
            ref Vehicle v = ref vehicleManager.m_vehicles.m_buffer[vehicleId];

            if ((v.m_flags & Vehicle.Flags.Created) == 0)
            {
                reason = "vehicle not Created (flags=" + v.m_flags + ")";
                return false;
            }

            uint citizenUnitId = v.m_citizenUnits;
            if (citizenUnitId == 0)
            {
                reason = "vehicle.m_citizenUnits==0 (no citizens attached)";
                return false;
            }

            CitizenManager citizenManager = Singleton<CitizenManager>.instance;
            CitizenUnit[] unitsBuf = citizenManager.m_units.m_buffer;
            Citizen[] citizensBuf = citizenManager.m_citizens.m_buffer;

            uint maxUnitCount = citizenManager.m_units.m_size;
            int iter = 0;

            while (citizenUnitId != 0)
            {
                ref CitizenUnit unit = ref unitsBuf[citizenUnitId];

                for (int i = 0; i < 5; i++)
                {
                    uint citizen = unit.GetCitizen(i);
                    if (citizen == 0) continue;

                    ushort instanceId = citizensBuf[citizen].m_instance;
                    if (instanceId != 0)
                    {
                        ref CitizenInstance inst = ref citizenManager.m_instances.m_buffer[instanceId];
                        citizenId = inst.m_citizen != 0 ? inst.m_citizen : citizen;
                        reason = null;
                        return true;
                    }

                    citizenId = citizen;
                    reason = "citizen has no instance; using CitizenId directly";
                    return true;
                }

                citizenUnitId = unit.m_nextUnit;
                if (++iter > maxUnitCount)
                {
                    reason = "invalid citizen unit list (loop protection hit)";
                    return false;
                }
            }

            reason = "no citizens found in citizen units";
            return false;
        }
    }
}
