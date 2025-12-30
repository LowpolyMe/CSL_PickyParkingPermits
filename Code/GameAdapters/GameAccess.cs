using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace PickyParking.GameAdapters
{
    
    
    
    public sealed class GameAccess
    {
        private readonly List<Vector3> _parkingSpacePositions = new List<Vector3>(64);
        private readonly HashSet<ushort> _foundParkedVehicleIds = new HashSet<ushort>();

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

            if ((vehicle.m_flags & Vehicle.Flags.Created) == 0) return false;

            VehicleInfo info = vehicle.Info;
            VehicleAI ai = info?.m_vehicleAI;
            if (ai == null) return false;

            
            InstanceID owner = ai.GetOwnerID(vehicleId, ref vehicle);
            uint citizenId = owner.Citizen;
            if (citizenId == 0) return false;

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
            if (citizenId == 0) return false;

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

        
        
        
        
        
        public bool TryGetApproxParkingArea(ushort buildingId, out Vector3 center, out float radius)
        {
            center = default;
            radius = 0f;

            if (!TryGetBuildingPosition(buildingId, out center))
                return false;

            ref Building b =
                ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId];

            BuildingInfo info = b.Info;
            if (info == null)
                return false;

            
            float w = info.m_cellWidth * 8f;
            float l = info.m_cellLength * 8f;
            float halfDiag = 0.5f * Mathf.Sqrt(w * w + l * l);

            radius = Mathf.Max(halfDiag + 24f, 48f);
            return true;
        }

        
        
        
        
        public bool TryCollectParkingSpacePositions(
            ushort buildingId,
            List<Vector3> outPositions)
        {
            outPositions.Clear();
            if (buildingId == 0) return false;

            var bm = Singleton<BuildingManager>.instance;
            ref Building building = ref bm.m_buildings.m_buffer[buildingId];

            BuildingInfo buildingInfo = building.Info;
            if (buildingInfo == null) return false;
            if (buildingInfo.m_props == null) return false;

            
            if ((buildingInfo.m_hasParkingSpaces & VehicleInfo.VehicleType.Car) == VehicleInfo.VehicleType.None)
                return false;

            bool transformMatrixCalculated = false;
            Matrix4x4 buildingMatrix = default;

            foreach (BuildingInfo.Prop prop in buildingInfo.m_props)
            {
                var randomizer = new Randomizer(buildingId << 6 | prop.m_index);

                PropInfo propInfo = prop.m_finalProp;
                if (propInfo == null) continue;

                propInfo = propInfo.GetVariation(ref randomizer);
                if (propInfo == null) continue;

                var spaces = propInfo.m_parkingSpaces;
                if (spaces == null || spaces.Length == 0) continue;

                if (!transformMatrixCalculated)
                {
                    transformMatrixCalculated = true;

                    Vector3 meshPos = Building.CalculateMeshPosition(
                        buildingInfo,
                        building.m_position,
                        building.m_angle,
                        building.Length);

                    Quaternion q = Quaternion.AngleAxis(
                        building.m_angle * Mathf.Rad2Deg,
                        Vector3.down);

                    buildingMatrix.SetTRS(meshPos, q, Vector3.one);
                }

                Vector3 propWorldPos = buildingMatrix.MultiplyPoint(prop.m_position);

                float angle = building.m_angle + prop.m_radAngle;
                Quaternion propRot = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.down);
                Matrix4x4 propMatrix = Matrix4x4.TRS(propWorldPos, propRot, Vector3.one);

                for (int i = 0; i < spaces.Length; i++)
                {
                    
                    Vector3 local = spaces[i].m_position;
                    outPositions.Add(propMatrix.MultiplyPoint(local));
                }
            }

            return outPositions.Count > 0;
        }

        
        
        
        
        public void CollectParkedVehiclesOnLot(
            ushort buildingId,
            List<ushort> results,
            float maxSnapDistance = 2f)
        {
            results.Clear();
            if (buildingId == 0) return;

            
            _foundParkedVehicleIds.Clear();
            if (!TryCollectParkingSpacePositions(buildingId, _parkingSpacePositions))
                return;

            float maxSnapDistSqr = maxSnapDistance * maxSnapDistance;

            var vm = Singleton<VehicleManager>.instance;

            for (int s = 0; s < _parkingSpacePositions.Count; s++)
            {
                Vector3 spacePos = _parkingSpacePositions[s];

                int gx = Mathf.Clamp((int)(spacePos.x / 32f + 270f), 0, 539);
                int gz = Mathf.Clamp((int)(spacePos.z / 32f + 270f), 0, 539);

                ushort parkedId = vm.m_parkedGrid[gz * 540 + gx];

                int safety = 0;
                while (parkedId != 0)
                {
                    ref VehicleParked pv = ref vm.m_parkedVehicles.m_buffer[parkedId];
                    ushort next = pv.m_nextGridParked;

                    if (pv.m_flags != 0)
                    {
                        float dx = pv.m_position.x - spacePos.x;
                        float dz = pv.m_position.z - spacePos.z;

                        if (dx * dx + dz * dz <= maxSnapDistSqr)
                            _foundParkedVehicleIds.Add(parkedId);
                    }

                    parkedId = next;

                    if (++safety > 32768)
                        break;
                }
            }

            results.AddRange(_foundParkedVehicleIds);
            _foundParkedVehicleIds.Clear();
        }

        
        
        
        
        public bool IsPrivatePassengerCar(ushort vehicleId) => true;
    }
}

