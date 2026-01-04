using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;
using PickyParking.Logging;
using PickyParking.Features.Debug;

namespace PickyParking.GameAdapters
{
    public sealed class GameAccess
    {
        private readonly ParkedVehicleQueries _parkedVehicleQueries;
        private readonly ParkingSpaceQueries _parkingSpaceQueries;

        public GameAccess()
        {
            _parkedVehicleQueries = new ParkedVehicleQueries();
            _parkingSpaceQueries = new ParkingSpaceQueries(_parkedVehicleQueries);
        }

        public struct DriverContext
        {
            public readonly uint CitizenId;
            public readonly ushort HomeBuildingId;
            public readonly ushort WorkBuildingId;
            public readonly bool IsVisitor;

            public DriverContext(uint citizenId, ushort homeBuildingId, ushort workBuildingId, bool isVisitor)
            {
                CitizenId = citizenId;
                HomeBuildingId = homeBuildingId;
                WorkBuildingId = workBuildingId;
                IsVisitor = isVisitor;
            }
        }

        public bool TryGetBuildingInfo(ushort buildingId, out BuildingInfo info)
        {
            info = null;
            if (buildingId == 0) return false;

            ref Building building =
                ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId];

            if ((building.m_flags & Building.Flags.Created) == 0) return false;
            if ((building.m_flags & Building.Flags.Deleted) != 0) return false;

            info = building.Info;
            return info != null;
        }

        public bool IsBuildingManagerReady()
        {
            return Singleton<BuildingManager>.exists;
        }

        public bool TryGetSelectedBuilding(out ushort buildingId, out BuildingInfo info)
        {
            buildingId = 0;
            info = null;

            InstanceID instanceId = WorldInfoPanel.GetCurrentInstanceID();
            if (instanceId.Building == 0) return false;

            buildingId = instanceId.Building;
            return TryGetBuildingInfo(buildingId, out info);
        }

        public bool TryGetBuildingPosition(ushort buildingId, out Vector3 position)
        {
            position = default;
            if (buildingId == 0) return false;

            ref Building building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId];
            if ((building.m_flags & Building.Flags.Created) == 0) return false;

            position = building.m_position;
            return true;
        }

        public bool TryGetDriverInfo(ushort vehicleId, out DriverContext context)
        {
            context = default;
            if (vehicleId == 0) return false;

            ref Vehicle vehicle =
                ref Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleId];

            if ((vehicle.m_flags & Vehicle.Flags.Created) == 0)
            {
                if (Log.IsVerboseEnabled && ParkingDebugSettings.EnableGameAccessLogs)
                    Log.Info($"[Parking] TryGetDriverInfo failed: vehicle not Created vehicleId={vehicleId} flags={vehicle.m_flags}");
                return false;
            }

            VehicleInfo info = vehicle.Info;
            VehicleAI ai = info?.m_vehicleAI;
            if (ai == null)
            {
                if (Log.IsVerboseEnabled && ParkingDebugSettings.EnableGameAccessLogs)
                    Log.Info($"[Parking] TryGetDriverInfo failed: vehicle AI missing vehicleId={vehicleId}");
                return false;
            }

            InstanceID owner = ai.GetOwnerID(vehicleId, ref vehicle);
            uint citizenId = owner.Citizen;
            if (citizenId == 0)
            {
                if (Log.IsVerboseEnabled && ParkingDebugSettings.EnableGameAccessLogs)
                    Log.Info(
                        "[Parking] TryGetDriverInfo failed: owner citizenId=0 " +
                        $"vehicleId={vehicleId} citizenUnits={vehicle.m_citizenUnits}"
                    );
                return false;
            }

            ref Citizen citizen =
                ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];

            bool isVisitor = (citizen.m_flags & Citizen.Flags.Tourist) != 0;

            context = new DriverContext(
                citizenId,
                citizen.m_homeBuilding,
                citizen.m_workBuilding,
                isVisitor
            );

            return true;
        }

        public bool TryGetCitizenInfo(uint citizenId, out DriverContext context)
        {
            context = default;
            if (citizenId == 0)
            {
                if (Log.IsVerboseEnabled && ParkingDebugSettings.EnableGameAccessLogs)
                    Log.Info("[Parking] TryGetCitizenInfo failed: citizenId=0");
                return false;
            }

            ref Citizen citizen =
                ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];

            bool isVisitor = (citizen.m_flags & Citizen.Flags.Tourist) != 0;

            context = new DriverContext(
                citizenId,
                citizen.m_homeBuilding,
                citizen.m_workBuilding,
                isVisitor
            );

            return true;
        }

        public bool TryGetParkedVehicleInfo(
            ushort parkedVehicleId,
            out uint ownerCitizenId,
            out ushort homeId,
            out Vector3 position)
        {
            return _parkedVehicleQueries.TryGetParkedVehicleInfo(
                parkedVehicleId,
                out ownerCitizenId,
                out homeId,
                out position);
        }

        public bool TryGetApproxParkingArea(ushort buildingId, out Vector3 center, out float radius)
        {
            return _parkingSpaceQueries.TryGetApproxParkingArea(buildingId, out center, out radius);
        }

        public bool TryCollectParkingSpacePositions(ushort buildingId, List<Vector3> outPositions)
        {
            return _parkingSpaceQueries.TryCollectParkingSpacePositions(buildingId, outPositions);
        }

        public bool TryGetParkingSpaceCount(ushort buildingId, out int totalSpaces)
        {
            return _parkingSpaceQueries.TryGetParkingSpaceCount(buildingId, out totalSpaces);
        }

        public void CollectParkedVehiclesOnLot(
            ushort buildingId,
            List<ushort> results,
            float maxSnapDistance = 2f)
        {
            _parkingSpaceQueries.CollectParkedVehiclesOnLot(buildingId, results, maxSnapDistance);
        }

        public bool TryGetParkingSpaceStats(
            ushort buildingId,
            out int totalSpaces,
            out int occupiedSpaces,
            float maxSnapDistance = 2f)
        {
            return _parkingSpaceQueries.TryGetParkingSpaceStats(
                buildingId,
                out totalSpaces,
                out occupiedSpaces,
                maxSnapDistance);
        }

        public bool IsPrivatePassengerCar(ushort vehicleId)
        {
            if (vehicleId == 0) return false;

            ref Vehicle vehicle =
                ref Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleId];

            if ((vehicle.m_flags & Vehicle.Flags.Created) == 0)
                return false;

            VehicleInfo info = vehicle.Info;
            if (info == null)
                return false;

            VehicleAI ai = info.m_vehicleAI;
            if (ai == null)
                return false;

            return ai is PassengerCarAI;
        }
    }
}
