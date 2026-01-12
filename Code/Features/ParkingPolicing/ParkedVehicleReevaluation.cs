using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;
using PickyParking.Logging;
using PickyParking.Features.Debug;
using PickyParking.ModLifecycle;
using PickyParking.Features.ParkingRules;
using PickyParking.GameAdapters;
using PickyParking.Features.ParkingLotPrefabs;

namespace PickyParking.Features.ParkingPolicing
{
    public sealed class ParkedVehicleReevaluation
    {
        private const int MaxMovesPerTick = 64;

        private readonly FeatureGate _isFeatureActive;
        private readonly ParkingRulesConfigRegistry _rules;
        private readonly ParkingPermissionEvaluator _evaluator;
        private readonly GameAccess _game;
        private readonly SupportedParkingLotRegistry _supportedLots;
        private readonly TmpeIntegration _tmpe;

        private readonly Queue<ushort> _pendingBuildings = new Queue<ushort>();
        private readonly HashSet<ushort> _pendingSet = new HashSet<ushort>();

        private readonly List<ushort> _parkedBuffer = new List<ushort>(512);
        private readonly List<ushort> _sweepBuildings = new List<ushort>(256);
        private readonly List<ushort> _unsupportedBuffer = new List<ushort>(32);

        private ushort _activeBuilding;
        private int _activeIndex;
        private bool _scheduled;
        private bool _disposed;
        private int _rulesVersionSeen = -1;
        private int _sweepIndex;
        private bool _resetSweepPending;
        private int _activeParkedCount;
        private int _activeAllowedCount;
        private int _activeDeniedCount;
        private int _activeMovedCount;
        private int _activeReleasedCount;
        private int _activeLoggedDenied;

        public ParkedVehicleReevaluation(
            FeatureGate featureGate,
            ParkingRulesConfigRegistry rules,
            ParkingPermissionEvaluator evaluator,
            GameAccess game,
            SupportedParkingLotRegistry supportedLots,
            TmpeIntegration tmpe)
        {
            _isFeatureActive = featureGate;
            _rules = rules;
            _evaluator = evaluator;
            _game = game;
            _supportedLots = supportedLots;
            _tmpe = tmpe;
        }

        public bool HasPendingWork => _activeBuilding != 0 || _pendingBuildings.Count > 0;

        public void RequestForBuilding(ushort buildingId)
        {
            if (buildingId == 0) return;
            if (_disposed) return;
            if (!_isFeatureActive.IsActive) return;
            if (!IsBuildingSupported(buildingId))
            {
                CleanupRuleIfUnsupported(buildingId);
                return;
            }

            if (Log.IsEnforcementDebugEnabled && ParkingDebugSettings.IsBuildingDebugEnabled(buildingId))
                Log.Warn("[Parking] Reevaluation requested for buildingId=" +
                         buildingId + "\n" + Environment.StackTrace);

            if (!_pendingSet.Add(buildingId))
                return;

            _pendingBuildings.Enqueue(buildingId);
            if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
                Log.Info("[Parking] Reevaluation queued buildingId=" + buildingId);
            Schedule();
        }

        public bool TryRequestNextScheduledBuilding(bool resetSweep)
        {
            if (_disposed) return false;

            if (resetSweep || _resetSweepPending)
            {
                EnsureSweepList();
                _sweepIndex = 0;
                _resetSweepPending = false;
            }

            if (!_isFeatureActive.IsActive) return false;
            if (HasPendingWork) return false;

            EnsureSweepList();
            if (_sweepIndex >= _sweepBuildings.Count)
                return false;

            ushort buildingId = _sweepBuildings[_sweepIndex++];
            RequestForBuilding(buildingId);
            return true;
        }

        public void NotifyDayChanged()
        {
            _resetSweepPending = true;
        }

        private void Schedule()
        {
            if (_disposed) return;
            if (_scheduled) return;
            _scheduled = true;
            if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
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
                    ParkedVehicleRemovalLogger.LogIfMatchesLot(
                        parkedVehicleId: parkedId,
                        buildingId: _activeBuilding,
                        source: "Reevaluation.ReleaseParkedVehicle");
                    Singleton<VehicleManager>.instance.ReleaseParkedVehicle(parkedId);
                    _activeReleasedCount++;
                }
                else
                {
                    _activeMovedCount++;
                }

                movedThisTick++;
            }

            if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
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
                    if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
                        Log.Info("[Parking] Reevaluation skipped (rule missing) buildingId=" + next);
                    _pendingSet.Remove(next);
                    continue;
                }

                if (!IsBuildingSupported(next))
                {
                    if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
                        Log.Info("[Parking] Reevaluation skipped (prefab unsupported) buildingId=" + next);
                    CleanupRuleIfUnsupported(next);
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
                    if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
                        Log.Info("[Parking] Reevaluation skipped (no parked vehicles) buildingId=" + next);
                    _pendingSet.Remove(next);
                    _activeBuilding = 0;
                    continue;
                }

                if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
                    Log.Info("[Parking] Reevaluation start buildingId=" + _activeBuilding + " parkedCount=" + _parkedBuffer.Count);

                return true;
            }

            return false;
        }

        private void FinishActiveBuilding()
        {
            if (_activeBuilding == 0)
                return;

            if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
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
            _sweepBuildings.Clear();
            _sweepIndex = 0;
            _rulesVersionSeen = -1;
            _scheduled = false;
            _resetSweepPending = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ClearAll();
        }

        private void EnsureSweepList()
        {
            int currentVersion = _rules.Version;
            if (currentVersion == _rulesVersionSeen)
                return;

            _sweepBuildings.Clear();
            _unsupportedBuffer.Clear();
            foreach (var kvp in _rules.Enumerate())
            {
                if (kvp.Key == 0)
                    continue;

                if (!IsBuildingSupported(kvp.Key))
                {
                    _unsupportedBuffer.Add(kvp.Key);
                    continue;
                }

                _sweepBuildings.Add(kvp.Key);
            }

            for (int i = 0; i < _unsupportedBuffer.Count; i++)
                CleanupRuleIfUnsupported(_unsupportedBuffer[i]);

            _rulesVersionSeen = _rules.Version;
            if (_sweepIndex > _sweepBuildings.Count)
                _sweepIndex = 0;
        }

        private bool IsBuildingSupported(ushort buildingId)
        {
            if (_supportedLots == null)
                return false;

            if (!_game.TryGetBuildingInfo(buildingId, out var info))
                return false;

            var key = ParkingLotPrefabKeyFactory.CreateKey(info);
            return _supportedLots.Contains(key);
        }

        private void CleanupRuleIfUnsupported(ushort buildingId)
        {
            if (_rules == null || buildingId == 0)
                return;

            _rules.RemoveIf(kvp => kvp.Key == buildingId);
        }
    }
}

