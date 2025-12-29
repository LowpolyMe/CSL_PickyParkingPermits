using System;
using System.Reflection;
using ColossalFramework;
using PickyParking.Infrastructure;
using PickyParking.App;
using PickyParking.Domain;
using UnityEngine;

namespace PickyParking.Infrastructure.Integration
{
    
    
    
    public sealed class TmpeIntegration
    {
        private readonly FeatureGate _isFeatureActive;
        private readonly ParkingPermissionEvaluator _evaluator;

        private const string AdvancedParkingManagerType =
            "TrafficManager.Manager.Impl.AdvancedParkingManager, TrafficManager";

        private const string GlobalConfigType = "TrafficManager.State.GlobalConfig, TrafficManager";

        private Type _apmType;
        private object _apmInstance;
        private MethodInfo _tryMoveParkedVehicle;

        public TmpeIntegration(FeatureGate featureGate, ParkingPermissionEvaluator evaluator)
        {
            _isFeatureActive = featureGate;
            _evaluator = evaluator;
        }

        public void RefreshState()
        {
            try
            {
                var tmpeType = Type.GetType(AdvancedParkingManagerType, throwOnError: false);

                if (tmpeType != null)
                    _isFeatureActive.SetActive();
                else
                    _isFeatureActive.SetInactive("TM:PE not found");
            }
            catch (Exception e)
            {
                _isFeatureActive.SetInactive("An exception occurred: " + e);
            }
        }

        private static bool TryGetCitizenIdFromVehicle(ushort vehicleId, out uint citizenId, out string reason)
        {
            citizenId = 0u;
            reason = null;

            if (vehicleId == 0)
            {
                reason = "vehicleId==0 (TM:PE internal / parked-relocation style search)";
                return false;
            }

            var vehicleManager = Singleton<VehicleManager>.instance;
            ref Vehicle v = ref vehicleManager.m_vehicles.m_buffer[vehicleId];

            if ((v.m_flags & Vehicle.Flags.Created) == 0)
            {
                reason = $"vehicle not Created (flags={v.m_flags})";
                return false;
            }

            uint citizenUnitId = v.m_citizenUnits;
            if (citizenUnitId == 0)
            {
                reason = "vehicle.m_citizenUnits==0 (no citizens attached)";
                return false;
            }

            var citizenManager = Singleton<CitizenManager>.instance;
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

        public bool TryDenyBuildingParkingCandidate(ushort candidateBuildingId, out DecisionReason reason)
        {
            reason = DecisionReason.Allowed_Unrestricted;

            if (ParkingSearchContext.HasCitizenId)
            {
                var result = _evaluator.EvaluateCitizen(ParkingSearchContext.CitizenId, candidateBuildingId);
                reason = result.Reason;
                return !result.Allowed;
            }

            
            if (ParkingSearchContext.HasVehicleId)
            {
                var result = _evaluator.Evaluate(ParkingSearchContext.VehicleId, candidateBuildingId);
                reason = result.Reason;
                return !result.Allowed;
            }
            
            if (Log.IsVerboseEnabled)
            {
                Log.Info(
                    $"[Parking] No context candidateBuildingId={candidateBuildingId}"
                );
            }

            return false;
        }

        private bool EnsureRelocationReflection()
        {
            if (_tryMoveParkedVehicle != null && _apmInstance != null)
                return true;

            _apmType = Type.GetType(AdvancedParkingManagerType, throwOnError: false);
            if (_apmType == null) return false;

            
            FieldInfo instanceField = _apmType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instanceField == null) return false;

            _apmInstance = instanceField.GetValue(null);
            if (_apmInstance == null) return false;

            _tryMoveParkedVehicle = _apmType.GetMethod(
                "TryMoveParkedVehicle",
                BindingFlags.Public | BindingFlags.Instance);

            return _tryMoveParkedVehicle != null;
        }

        public bool TryMoveParkedVehicleWithConfigDistance(ushort parkedVehicleId, uint ownerCitizenId, ushort homeId, Vector3 refPos)
        {
            if (!_isFeatureActive.IsActive) return false;
            if (parkedVehicleId == 0 || ownerCitizenId == 0) return false;
            if (!EnsureRelocationReflection()) return false;

            float maxDistance = 500f;

            var vm = Singleton<VehicleManager>.instance;
            VehicleParked pv = vm.m_parkedVehicles.m_buffer[parkedVehicleId];

            

            using (var _ = new ParkingContextScope(vehicleId: 0, citizenId: ownerCitizenId,
                       source: "PickyParking.TryMoveParkedVehicle"))
            {
                object[] args =
                {
                    parkedVehicleId,
                    pv, 
                    refPos,
                    maxDistance,
                    homeId
                };

                object result = _tryMoveParkedVehicle.Invoke(_apmInstance, args);

                if (args[1] is VehicleParked updated)
                    vm.m_parkedVehicles.m_buffer[parkedVehicleId] = updated;

                return result is bool b && b;
            }
        }


    }
}


