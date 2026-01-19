using System;
using System.Reflection;
using System.Threading;
using ColossalFramework;
using PickyParking.Features.Debug;
using PickyParking.Logging;
using PickyParking.ModLifecycle;
using PickyParking.Features.ParkingRules;
using UnityEngine;
using PickyParking.Settings;

namespace PickyParking.Features.ParkingPolicing
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
        private FieldInfo _findParkingSpacePropDelegateField;
        private Delegate _findParkingSpacePropDelegate;
        private VehicleInfo _defaultPassengerCarInfo;
        private bool _defaultPassengerCarInfoSearched;
        private int _offThreadLogged;

        public TmpeIntegration(FeatureGate featureGate, ParkingPermissionEvaluator evaluator)
        {
            _isFeatureActive = featureGate;
            _evaluator = evaluator;
        }

        public void RefreshState()
        {
            try
            {
                Type.GetType(AdvancedParkingManagerType, throwOnError: false);
            }
            catch (Exception e)
            {
                Log.AlwaysWarn("[TMPE] RefreshState failed: " + e);
            }
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
            
            if (Log.IsVerboseEnabled && Log.IsDecisionDebugEnabled)
                Log.Info(DebugLogCategory.DecisionPipeline, $"[Decision] No context candidateBuildingId={candidateBuildingId}");

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

        private bool EnsureFindParkingSpacePropDelegate()
        {
            if (_findParkingSpacePropDelegate != null && _apmInstance != null)
                return true;

            _apmType = Type.GetType(AdvancedParkingManagerType, throwOnError: false);
            if (_apmType == null) return false;

            FieldInfo instanceField = _apmType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instanceField == null) return false;

            _apmInstance = instanceField.GetValue(null);
            if (_apmInstance == null) return false;

            _findParkingSpacePropDelegateField = _apmType.GetField(
                "_findParkingSpacePropDelegate",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (_findParkingSpacePropDelegateField == null) return false;

            _findParkingSpacePropDelegate =
                _findParkingSpacePropDelegateField.GetValue(_apmInstance) as Delegate;

            return _findParkingSpacePropDelegate != null;
        }

        public bool TryGetFindParkingSpacePropDelegate(out Delegate findDelegate)
        {
            findDelegate = null;
            if (!_isFeatureActive.IsActive) return false;
            if (!SimThread.IsSimulationThread())
            {
                LogOffThread("TryGetFindParkingSpacePropDelegate");
                return false;
            }

            if (!EnsureFindParkingSpacePropDelegate())
                return false;

            findDelegate = _findParkingSpacePropDelegate;
            return findDelegate != null;
        }

        public bool TryGetDefaultPassengerCarInfo(out VehicleInfo info)
        {
            info = null;
            if (_defaultPassengerCarInfoSearched)
            {
                info = _defaultPassengerCarInfo;
                return info != null;
            }

            _defaultPassengerCarInfoSearched = true;

            int loadedCount = PrefabCollection<VehicleInfo>.LoadedCount();
            for (uint i = 0; i < loadedCount; i++)
            {
                VehicleInfo candidate = PrefabCollection<VehicleInfo>.GetLoaded(i);
                if (candidate != null && candidate.m_vehicleAI is PassengerCarAI)
                {
                    _defaultPassengerCarInfo = candidate;
                    break;
                }
            }

            info = _defaultPassengerCarInfo;
            if (info == null && Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled)
                Log.Info(DebugLogCategory.Tmpe, "[Integration:TMPE] Default passenger car prefab not found; TMPE occupancy checks disabled.");

            return info != null;
        }

        public bool TryMoveParkedVehicleWithConfigDistance(ushort parkedVehicleId, uint ownerCitizenId, ushort homeId, Vector3 refPos)
        {
            if (!_isFeatureActive.IsActive) return false;
            if (!SimThread.IsSimulationThread())
            {
                LogOffThread("TryMoveParkedVehicleWithConfigDistance");
                return false;
            }
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

        private void LogOffThread(string caller)
        {
            if (Interlocked.Exchange(ref _offThreadLogged, 1) == 0)
                Log.AlwaysWarn("[Threading] Off-simulation-thread access blocked: " + (caller ?? "UNKNOWN"));
        }

    }
}


