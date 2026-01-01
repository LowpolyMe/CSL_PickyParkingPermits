using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using PickyParking.Logging;
using PickyParking.Features.Debug;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.Features.ParkingPolicing.Runtime;

namespace PickyParking.GameAdapters
{
    
    
    
    public sealed class GameAccess
    {
        private const int ParkedGridSafetyLimit = 32768;
        private const float TmpeSpaceMatchEpsilon = 0.25f;
        private const string InvisibleParkingSpacePropName = "Invisible Parking Space";
        private readonly List<Vector3> _parkingSpacePositions = new List<Vector3>(64);
        private readonly HashSet<ushort> _foundParkedVehicleIds = new HashSet<ushort>();
        private readonly HashSet<long> _debugUniquePositions = new HashSet<long>();
        private static MethodInfo _terrainSampleMethod;
        private static bool _terrainSampleMethodTakesVector3;
        private static bool _terrainSampleMethodSearched;

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
            if (buildingInfo == null)
            {
                if (Log.IsVerboseEnabled && ParkingDebugSettings.EnableGameAccessLogs)
                    Log.Info($"[Parking] Parking spaces missing: building info null buildingId={buildingId}");
                return false;
            }
            if (buildingInfo.m_props == null)
            {
                if (Log.IsVerboseEnabled && ParkingDebugSettings.EnableGameAccessLogs)
                    Log.Info($"[Parking] Parking spaces missing: building props null buildingId={buildingId} prefab={buildingInfo.name}");
                return false;
            }

            
            if ((buildingInfo.m_hasParkingSpaces & VehicleInfo.VehicleType.Car) == VehicleInfo.VehicleType.None)
            {
                if (Log.IsVerboseEnabled && ParkingDebugSettings.EnableGameAccessLogs && ShouldLogForBuilding(buildingId, buildingInfo))
                    Log.Info($"[Parking] Parking spaces missing: m_hasParkingSpaces=0 buildingId={buildingId} prefab={buildingInfo.name}");
                return false;
            }

            bool transformMatrixCalculated = false;
            Matrix4x4 buildingMatrix = default;

            foreach (BuildingInfo.Prop prop in buildingInfo.m_props)
            {
                var randomizer = new Randomizer(buildingId << 6 | prop.m_index);

                PropInfo propInfo = prop.m_finalProp;
                if (propInfo == null) continue;

                propInfo = propInfo.GetVariation(ref randomizer);
                if (propInfo == null) continue;

                if (string.Equals(propInfo.name, InvisibleParkingSpacePropName, StringComparison.Ordinal))
                    continue;

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

            if (outPositions.Count == 0 && Log.IsVerboseEnabled && ParkingDebugSettings.EnableGameAccessLogs)
                Log.Info($"[Parking] Parking spaces missing: no parking space props buildingId={buildingId} prefab={buildingInfo.name}");

            return outPositions.Count > 0;
        }

        
        
        
        
        public bool TryGetParkingSpaceCount(ushort buildingId, out int totalSpaces)
        {
            totalSpaces = 0;
            if (!TryCollectParkingSpacePositions(buildingId, _parkingSpacePositions))
                return false;

            totalSpaces = _parkingSpacePositions.Count;
            return totalSpaces > 0;
        }

        public void CollectParkedVehiclesOnLot(
            ushort buildingId,
            List<ushort> results,
            float maxSnapDistance = 2f)
        {
            results.Clear();
            if (buildingId == 0) return;

            int totalSpaces;
            int occupiedSpaces;
            if (!TryCollectParkingSpaceUsage(buildingId, maxSnapDistance, _foundParkedVehicleIds, out totalSpaces, out occupiedSpaces))
                return;

            results.AddRange(_foundParkedVehicleIds);
            _foundParkedVehicleIds.Clear();
        }

        public bool TryGetParkingSpaceStats(
            ushort buildingId,
            out int totalSpaces,
            out int occupiedSpaces,
            float maxSnapDistance = 2f)
        {
            return TryCollectParkingSpaceUsage(buildingId, maxSnapDistance, null, out totalSpaces, out occupiedSpaces);
        }

        private bool TryCollectParkingSpaceUsage(
            ushort buildingId,
            float maxSnapDistance,
            HashSet<ushort> outParkedVehicleIds,
            out int totalSpaces,
            out int occupiedSpaces)
        {
            totalSpaces = 0;
            occupiedSpaces = 0;

            if (outParkedVehicleIds == null &&
                TryCollectParkingSpaceUsageUsingTmpe(buildingId, out totalSpaces, out occupiedSpaces))
                return true;

            HashSet<ushort> uniqueParked = outParkedVehicleIds ?? _foundParkedVehicleIds;
            uniqueParked.Clear();

            if (!TryCollectParkingSpacePositions(buildingId, _parkingSpacePositions))
                return false;

            totalSpaces = _parkingSpacePositions.Count;
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
                        {
                            uniqueParked.Add(parkedId);
                            break;
                        }
                    }

                    parkedId = next;

                    if (++safety > ParkedGridSafetyLimit)
                    {
                        if (Log.IsVerboseEnabled && ParkingDebugSettings.EnableGameAccessLogs)
                            Log.Info($"[Parking] Parked grid safety limit hit buildingId={buildingId} space=({spacePos.x:F1},{spacePos.y:F1},{spacePos.z:F1})");
                        break;
                    }
                }

            }

            // Count unique parked vehicles instead of per-space occupancy.
            // This avoids double-counting when space positions overlap.
            occupiedSpaces = uniqueParked.Count;
            if (outParkedVehicleIds == null)
                _foundParkedVehicleIds.Clear();

            if (Log.IsVerboseEnabled && ParkingDebugSettings.EnableGameAccessLogs && totalSpaces > 0 && occupiedSpaces == totalSpaces && ShouldLogForBuilding(buildingId, null))
            {
                Log.Info($"[Parking] Parking space stats show no free spaces buildingId={buildingId} total={totalSpaces} maxSnapDistance={maxSnapDistance}");
            }

            if (Log.IsVerboseEnabled && ParkingDebugSettings.EnableGameAccessLogs &&
                ParkingDebugSettings.IsBuildingDebugEnabled(buildingId))
            {
                LogIndustry1Stats(buildingId, maxSnapDistance, totalSpaces, occupiedSpaces);
            }

            return true;
        }

        private bool TryCollectParkingSpaceUsageUsingTmpe(
            ushort buildingId,
            out int totalSpaces,
            out int occupiedSpaces)
        {
            totalSpaces = 0;
            occupiedSpaces = 0;

            var context = ParkingRuntimeContext.Current;
            var tmpe = context?.TmpeIntegration;
            if (tmpe == null) return false;

            if (!tmpe.TryGetFindParkingSpacePropDelegate(out var findParkingSpacePropDelegate))
                return false;

            if (!tmpe.TryGetDefaultPassengerCarInfo(out var vehicleInfo))
                return false;

            if (buildingId == 0) return false;

            ref Building building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId];
            if ((building.m_flags & Building.Flags.Created) == 0) return false;

            BuildingInfo buildingInfo = building.Info;
            if (buildingInfo == null || buildingInfo.m_props == null)
                return false;

            if ((buildingInfo.m_hasParkingSpaces & VehicleInfo.VehicleType.Car) == VehicleInfo.VehicleType.None)
                return false;

            bool isElectric = vehicleInfo.m_class.m_subService != ItemClass.SubService.ResidentialLow;
            float vehicleWidth = vehicleInfo.m_generatedInfo.m_size.x;
            float vehicleLength = vehicleInfo.m_generatedInfo.m_size.z;

            bool transformMatrixCalculated = false;
            Matrix4x4 buildingMatrix = default;

            bool logTmpeSpaceDetails = Log.IsVerboseEnabled && ParkingDebugSettings.EnableGameAccessLogs &&
                                      ParkingDebugSettings.IsBuildingDebugEnabled(buildingId);
            List<string> blockedSamples = null;
            List<string> freeSamples = null;
            HashSet<ushort> loggedParkedVehicles = null;

            int tmpeFreeSpaces = 0;

            foreach (BuildingInfo.Prop prop in buildingInfo.m_props)
            {
                var randomizer = new Randomizer(buildingId << 6 | prop.m_index);
                if (randomizer.Int32(100u) >= prop.m_probability ||
                    building.Length < prop.m_requiredLength)
                    continue;

                PropInfo propInfo = prop.m_finalProp;
                if (propInfo == null) continue;

                propInfo = propInfo.GetVariation(ref randomizer);
                if (propInfo == null) continue;

                if (string.Equals(propInfo.name, InvisibleParkingSpacePropName, StringComparison.Ordinal))
                    continue;

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
                    Vector3 spaceWorldPos = propMatrix.MultiplyPoint(spaces[i].m_position);
                    float maxDistance = TmpeSpaceMatchEpsilon;
                    Vector3 parkPos = default;
                    Quaternion parkRot = default;

                    object[] args =
                    {
                        isElectric,
                        (ushort)0,
                        propInfo,
                        propWorldPos,
                        angle,
                        prop.m_fixedHeight,
                        spaceWorldPos,
                        vehicleWidth,
                        vehicleLength,
                        maxDistance,
                        parkPos,
                        parkRot
                    };

                    bool isFree;
                    try
                    {
                        isFree = (bool)findParkingSpacePropDelegate.DynamicInvoke(args);
                    }
                    catch (Exception e)
                    {
                        if (Log.IsVerboseEnabled && ParkingDebugSettings.EnableGameAccessLogs)
                            Log.Warn("[Parking] TMPE occupancy check failed: " + e);
                        return false;
                    }

                    totalSpaces++;
                    if (isFree)
                        tmpeFreeSpaces++;

                    if (logTmpeSpaceDetails &&
                        (blockedSamples == null || blockedSamples.Count < 8 ||
                         freeSamples == null || freeSamples.Count < 4))
                    {
                        Vector3 foundPos = default;
                        if (args[10] is Vector3 found)
                            foundPos = found;

                        float effectiveMaxDistance = args[9] is float maxDist ? maxDist : maxDistance;

                        float dx = foundPos.x - spaceWorldPos.x;
                        float dz = foundPos.z - spaceWorldPos.z;
                        float dy = foundPos.y - spaceWorldPos.y;
                        float distXZ = Mathf.Sqrt(dx * dx + dz * dz);
                        float dist3d = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);

                        ushort nearestParkedId;
                        float nearestParkedDist;
                        float nearestParkedDist3d;
                        Vector3 nearestParkedPos;
                        bool hasNearestParked = TryFindNearestParkedVehicle(
                            spaceWorldPos,
                            6f,
                            out nearestParkedId,
                            out nearestParkedDist,
                            out nearestParkedDist3d,
                            out nearestParkedPos);

                        string entry =
                            $"prop={propInfo.name ?? "UNKNOWN"} " +
                            $"pos=({spaceWorldPos.x:F1},{spaceWorldPos.y:F1},{spaceWorldPos.z:F1}) " +
                            $"matchDist={dist3d:F2} matchDistXZ={distXZ:F2} matchDY={dy:F2} " +
                            $"maxDist={effectiveMaxDistance:F2} " +
                            $"nearestParked={(hasNearestParked ? nearestParkedId.ToString() : "0")} " +
                            $"nearestParkedDist={(hasNearestParked ? nearestParkedDist.ToString("F2") : "n/a")} " +
                            $"nearestParkedDist3d={(hasNearestParked ? nearestParkedDist3d.ToString("F2") : "n/a")} " +
                            $"nearestParkedPos={(hasNearestParked ? $"({nearestParkedPos.x:F1},{nearestParkedPos.y:F1},{nearestParkedPos.z:F1})" : "n/a")}";
                        if (isFree)
                        {
                            if (freeSamples == null) freeSamples = new List<string>(4);
                            if (freeSamples.Count < 4) freeSamples.Add(entry);
                        }
                        else
                        {
                            if (logTmpeSpaceDetails && hasNearestParked)
                            {
                                if (loggedParkedVehicles == null)
                                    loggedParkedVehicles = new HashSet<ushort>();

                                if (loggedParkedVehicles.Count < 8 &&
                                    loggedParkedVehicles.Add(nearestParkedId))
                                {
                                    LogParkedVehicleDetails(nearestParkedId, buildingId);
                                }
                            }

                            if ((blockedSamples == null || blockedSamples.Count < 8) &&
                                logTmpeSpaceDetails)
                            {
                                float probeDistance = 5f;
                                object[] probeArgs =
                                {
                                    isElectric,
                                    (ushort)0,
                                    propInfo,
                                    propWorldPos,
                                    angle,
                                    prop.m_fixedHeight,
                                    spaceWorldPos,
                                    vehicleWidth,
                                    vehicleLength,
                                    probeDistance,
                                    default(Vector3),
                                    default(Quaternion)
                                };

                                bool probeFound;
                                try
                                {
                                    probeFound = (bool)findParkingSpacePropDelegate.DynamicInvoke(probeArgs);
                                }
                                catch
                                {
                                    probeFound = false;
                                }

                                float probeMaxDistance = probeArgs[9] is float pMax ? pMax : probeDistance;

                                if (probeFound && probeArgs[10] is Vector3 probePos)
                                {
                                    float pdx = probePos.x - spaceWorldPos.x;
                                    float pdz = probePos.z - spaceWorldPos.z;
                                    float pdy = probePos.y - spaceWorldPos.y;
                                    float pDist = Mathf.Sqrt(pdx * pdx + pdz * pdz);
                                    float pDist3d = Mathf.Sqrt(pdx * pdx + pdy * pdy + pdz * pdz);
                                    entry +=
                                        $" probeDist={pDist3d:F2} probeDistXZ={pDist:F2} probeDY={pdy:F2} " +
                                        $"probeMaxDist={probeMaxDistance:F2}";
                                }
                            }

                            if (blockedSamples == null) blockedSamples = new List<string>(8);
                            if (blockedSamples.Count < 8) blockedSamples.Add(entry);
                        }
                    }
                }
            }

            occupiedSpaces = totalSpaces - tmpeFreeSpaces;

            if (Log.IsVerboseEnabled && ParkingDebugSettings.EnableGameAccessLogs &&
                ParkingDebugSettings.IsBuildingDebugEnabled(buildingId))
            {
                Log.Info(
                    "[Parking] Building debug TMPE occupancy " +
                    $"buildingId={buildingId} spaces={totalSpaces} occupied={occupiedSpaces} " +
                    $"epsilon={TmpeSpaceMatchEpsilon:F2}"
                );
            }

            if (logTmpeSpaceDetails)
            {
                int freeSpaces = totalSpaces - occupiedSpaces;
                string blockedSample = blockedSamples != null && blockedSamples.Count > 0
                    ? string.Join(", ", blockedSamples.ToArray())
                    : "n/a";
                string freeSample = freeSamples != null && freeSamples.Count > 0
                    ? string.Join(", ", freeSamples.ToArray())
                    : "n/a";

                Log.Info(
                    "[Parking] TMPE space detail " +
                    $"buildingId={buildingId} total={totalSpaces} free={freeSpaces} occupied={occupiedSpaces} " +
                    $"epsilon={TmpeSpaceMatchEpsilon:F2} " +
                    $"blockedSample=[{blockedSample}] freeSample=[{freeSample}]"
                );
            }

            return totalSpaces > 0;
        }

        private void LogParkedVehicleDetails(ushort parkedVehicleId, ushort buildingId)
        {
            if (parkedVehicleId == 0) return;

            ref VehicleParked pv =
                ref Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer[parkedVehicleId];

            if (pv.m_flags == 0) return;

            string prefabName = pv.Info != null ? pv.Info.name : "UNKNOWN";
            Vector3 pos = pv.m_position;
            float terrainHeight;
            bool hasTerrainHeight = TryGetTerrainHeight(pos, out terrainHeight);
            float deltaToTerrain = hasTerrainHeight ? pos.y - terrainHeight : 0f;

            Log.Info(
                "[Parking] TMPE parked vehicle " +
                $"buildingId={buildingId} parkedId={parkedVehicleId} prefab={prefabName} " +
                $"flags={pv.m_flags} ownerCitizen={pv.m_ownerCitizen} " +
                $"pos=({pos.x:F1},{pos.y:F1},{pos.z:F1}) " +
                $"terrainY={(hasTerrainHeight ? terrainHeight.ToString("F2") : "n/a")} " +
                $"deltaY={(hasTerrainHeight ? deltaToTerrain.ToString("F2") : "n/a")}"
            );
        }

        private static bool TryGetTerrainHeight(Vector3 position, out float height)
        {
            height = 0f;

            if (!_terrainSampleMethodSearched)
            {
                _terrainSampleMethodSearched = true;
                var methods = typeof(TerrainManager).GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                string[] names =
                {
                    "SampleRawHeightSmooth",
                    "SampleRawHeight",
                    "SampleHeightSmooth",
                    "SampleHeight"
                };

                foreach (string name in names)
                {
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo method = methods[i];
                        if (!string.Equals(method.Name, name, StringComparison.Ordinal))
                            continue;

                        ParameterInfo[] ps = method.GetParameters();
                        if (ps.Length == 1 && ps[0].ParameterType == typeof(Vector3))
                        {
                            _terrainSampleMethod = method;
                            _terrainSampleMethodTakesVector3 = true;
                            break;
                        }
                        if (ps.Length == 2 &&
                            ps[0].ParameterType == typeof(float) &&
                            ps[1].ParameterType == typeof(float))
                        {
                            _terrainSampleMethod = method;
                            _terrainSampleMethodTakesVector3 = false;
                            break;
                        }
                    }

                    if (_terrainSampleMethod != null)
                        break;
                }
            }

            if (_terrainSampleMethod == null)
                return false;

            object result;
            var terrain = Singleton<TerrainManager>.instance;

            if (_terrainSampleMethodTakesVector3)
            {
                result = _terrainSampleMethod.Invoke(terrain, new object[] { position });
            }
            else
            {
                result = _terrainSampleMethod.Invoke(terrain, new object[] { position.x, position.z });
            }

            if (!(result is float)) return false;
            height = (float)result;
            return true;
        }

        private bool TryFindNearestParkedVehicle(
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

        
        
        
        
        public bool IsPrivatePassengerCar(ushort vehicleId) => true;

        private static bool ShouldLogForBuilding(ushort buildingId, BuildingInfo info)
        {
            var context = ParkingRuntimeContext.Current;
            if (context == null || context.SupportedParkingLotRegistry == null)
                return false;

            if (info == null)
            {
                if (!context.GameAccess.TryGetBuildingInfo(buildingId, out info))
                    return false;
            }

            var key = ParkingLotPrefabKeyFactory.CreateKey(info);
            return context.SupportedParkingLotRegistry.Contains(key);
        }

        private void LogIndustry1Stats(ushort buildingId, float maxSnapDistance, int totalSpaces, int occupiedSpaces)
        {
            _debugUniquePositions.Clear();

            for (int i = 0; i < _parkingSpacePositions.Count; i++)
            {
                Vector3 p = _parkingSpacePositions[i];
                int xi = Mathf.RoundToInt(p.x * 10f);
                int zi = Mathf.RoundToInt(p.z * 10f);
                long key = ((long)xi << 32) ^ (uint)zi;
                _debugUniquePositions.Add(key);
            }

            int uniqueCount = _debugUniquePositions.Count;
            string sample = "";
            int sampleCount = Mathf.Min(10, _parkingSpacePositions.Count);
            for (int i = 0; i < sampleCount; i++)
            {
                Vector3 p = _parkingSpacePositions[i];
                sample += $"({p.x:F1},{p.y:F1},{p.z:F1})";
                if (i + 1 < sampleCount) sample += ", ";
            }

            Log.Info(
                "[Parking] Building debug space dump " +
                $"buildingId={buildingId} spaces={totalSpaces} unique={uniqueCount} occupied={occupiedSpaces} " +
                $"maxSnapDistance={maxSnapDistance} sample=[{sample}]"
            );
        }
    }
}


