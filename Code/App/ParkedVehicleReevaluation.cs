using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;
using PickyParking.Infrastructure;
using PickyParking.Infrastructure.Persistence;
using PickyParking.Infrastructure.Integration;

namespace PickyParking.App
{
    
    
    
    
    
    
    public sealed class ParkedVehicleReevaluation
    {
        private const int MaxMovesPerTick = 64;

        private readonly FeatureGate _isFeatureActive;
        private readonly ParkingRestrictionsConfigRegistry _rules;
        private readonly ParkingPermissionEvaluator _evaluator;
        private readonly GameAccess _game;
        private readonly TmpeIntegration _tmpe;

        private readonly Queue<ushort> _pendingBuildings = new Queue<ushort>();
        private readonly HashSet<ushort> _pendingSet = new HashSet<ushort>();

        private readonly List<ushort> _parkedBuffer = new List<ushort>(512);

        private ushort _activeBuilding;
        private int _activeIndex;
        private bool _scheduled;
        private bool _disposed;
        private int _activeParkedCount;
        private int _activeAllowedCount;
        private int _activeDeniedCount;
        private int _activeMovedCount;
        private int _activeReleasedCount;
        private int _activeLoggedDenied;

        public ParkedVehicleReevaluation(
            FeatureGate featureGate,
            ParkingRestrictionsConfigRegistry rules,
            ParkingPermissionEvaluator evaluator,
            GameAccess game,
            TmpeIntegration tmpe)
        {
            _isFeatureActive = featureGate;
            _rules = rules;
            _evaluator = evaluator;
            _game = game;
            _tmpe = tmpe;
        }

        
        
        
        
        
        public void RequestForBuilding(ushort buildingId)
        {
            if (buildingId == 0) return;
            if (_disposed) return;
            if (!_isFeatureActive.IsActive) return;

            if (!_pendingSet.Add(buildingId))
                return;

            _pendingBuildings.Enqueue(buildingId);
            if (Log.IsVerboseEnabled)
                Log.Info("[Parking] Reevaluation queued buildingId=" + buildingId);
            Schedule();
        }

        private void Schedule()
        {
            if (_disposed) return;
            if (_scheduled) return;
            _scheduled = true;
            if (Log.IsVerboseEnabled)
                Log.Info("[Parking] Reevaluation scheduled");
            SimThread.Dispatch(Step);
        }

        private void Step()
        {
            _scheduled = false;
            if (_disposed)
            {
                ClearAll();
                return;
            }

            if (!_isFeatureActive.IsActive)
            {
                ClearAll();
                return;
            }

            int movedThisTick = 0;

            while (movedThisTick < MaxMovesPerTick)
            {
                
                if (_activeBuilding == 0 || _activeIndex >= _parkedBuffer.Count)
                {
                    FinishActiveBuilding();

                    if (!TryBeginNextBuilding())
                        return; 
                }

                if (_activeIndex >= _parkedBuffer.Count)
                    continue;

                ushort parkedId = _parkedBuffer[_activeIndex++];
                if (!_game.TryGetParkedVehicleInfo(parkedId, out uint ownerCitizenId, out ushort homeId, out Vector3 parkedPos))
                    continue;

                
                var eval = _evaluator.EvaluateCitizen(ownerCitizenId, _activeBuilding);
                if (eval.Allowed)
                {
                    _activeAllowedCount++;
                    continue;
                }

                _activeDeniedCount++;
                if (Log.IsVerboseEnabled && _activeLoggedDenied < 5)
                {
                    _activeLoggedDenied++;
                    Log.Info("[Parking] Reevaluation denied parkedId=" + parkedId
                             + " citizenId=" + ownerCitizenId
                             + " reason=" + eval.Reason);
                }

                
                bool moved = _tmpe.TryMoveParkedVehicleWithConfigDistance(
                    parkedVehicleId: parkedId,
                    ownerCitizenId: ownerCitizenId,
                    homeId: homeId,
                    refPos: parkedPos
                );

                if (!moved)
                {
                    
                    Singleton<VehicleManager>.instance.ReleaseParkedVehicle(parkedId);
                    _activeReleasedCount++;
                }
                else
                {
                    _activeMovedCount++;
                }

                movedThisTick++;
            }

            
            if (Log.IsVerboseEnabled)
                Log.Info("[Parking] Reevaluation tick moved=" + movedThisTick + " buildingId=" + _activeBuilding);
            Schedule();
        }

        private bool TryBeginNextBuilding()
        {
            while (_pendingBuildings.Count > 0)
            {
                ushort next = _pendingBuildings.Dequeue();

                
                if (!_rules.TryGet(next, out _))
                {
                    if (Log.IsVerboseEnabled)
                        Log.Info("[Parking] Reevaluation skipped (rule missing) buildingId=" + next);
                    _pendingSet.Remove(next);
                    continue;
                }

                _activeBuilding = next;
                _activeIndex = 0;
                _parkedBuffer.Clear();
                _activeAllowedCount = 0;
                _activeDeniedCount = 0;
                _activeMovedCount = 0;
                _activeReleasedCount = 0;
                _activeLoggedDenied = 0;

                
                _game.CollectParkedVehiclesOnLot(next, _parkedBuffer);
                _activeParkedCount = _parkedBuffer.Count;

                
                if (_parkedBuffer.Count == 0)
                {
                    if (Log.IsVerboseEnabled)
                        Log.Info("[Parking] Reevaluation skipped (no parked vehicles) buildingId=" + next);
                    _pendingSet.Remove(next);
                    _activeBuilding = 0;
                    continue;
                }

                if (Log.IsVerboseEnabled)
                    Log.Info("[Parking] Reevaluation start buildingId=" + _activeBuilding + " parkedCount=" + _parkedBuffer.Count);

                return true;
            }

            return false;
        }

        private void FinishActiveBuilding()
        {
            if (_activeBuilding == 0)
                return;

            if (Log.IsVerboseEnabled)
            {
                Log.Info("[Parking] Reevaluation done buildingId=" + _activeBuilding
                         + " parkedCount=" + _activeParkedCount
                         + " allowed=" + _activeAllowedCount
                         + " denied=" + _activeDeniedCount
                         + " moved=" + _activeMovedCount
                         + " released=" + _activeReleasedCount);
            }

            _pendingSet.Remove(_activeBuilding);
            _activeBuilding = 0;
            _activeIndex = 0;
            _parkedBuffer.Clear();
            _activeParkedCount = 0;
            _activeAllowedCount = 0;
            _activeDeniedCount = 0;
            _activeMovedCount = 0;
            _activeReleasedCount = 0;
            _activeLoggedDenied = 0;
        }

        private void ClearAll()
        {
            _pendingBuildings.Clear();
            _pendingSet.Clear();
            _activeBuilding = 0;
            _activeIndex = 0;
            _parkedBuffer.Clear();
            _scheduled = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ClearAll();
        }
    }
}



