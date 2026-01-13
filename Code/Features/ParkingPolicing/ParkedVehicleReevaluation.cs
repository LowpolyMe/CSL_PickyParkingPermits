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
using PickyParking.Settings;

namespace PickyParking.Features.ParkingPolicing
{
    public sealed class ParkedVehicleReevaluation
    {
        private const int DefaultMaxEvaluationsPerTick = 128; //evals per building per tick
        private const int DefaultMaxRelocationsPerTick = 8; //resulting relocations (only violating vehicles) per tick per building
        private const int DefaultMaxFinalizationsPerTick = 4; //stuck parked vehicles finalized per tick per building

        private readonly FeatureGate _isFeatureActive;
        private readonly ParkingRulesConfigRegistry _rules;
        private readonly ParkingPermissionEvaluator _evaluator;
        private readonly GameAccess _game;
        private readonly SupportedParkingLotRegistry _supportedLots;
        private readonly TmpeIntegration _tmpe;
        private readonly ModSettingsController _settingsController;

        private readonly Queue<ushort> _pendingBuildings = new Queue<ushort>();
        private readonly HashSet<ushort> _pendingSet = new HashSet<ushort>();
        private readonly Queue<DeniedParkedVehicle> _deniedQueue = new Queue<DeniedParkedVehicle>();

        private readonly List<ushort> _parkedBuffer = new List<ushort>(512);
        private readonly List<ushort> _sweepBuildings = new List<ushort>(256);
        private readonly List<ushort> _unsupportedBuffer = new List<ushort>(32);
        private readonly Dictionary<ushort, uint> _stuckSeenToday = new Dictionary<ushort, uint>();
        private readonly Dictionary<ushort, uint> _stuckSeenYesterday = new Dictionary<ushort, uint>();

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
        private int _activeFixedCount;

        public ParkedVehicleReevaluation(
            FeatureGate featureGate,
            ParkingRulesConfigRegistry rules,
            ParkingPermissionEvaluator evaluator,
            GameAccess game,
            SupportedParkingLotRegistry supportedLots,
            TmpeIntegration tmpe,
            ModSettingsController settingsController)
        {
            _isFeatureActive = featureGate;
            _rules = rules;
            _evaluator = evaluator;
            _game = game;
            _supportedLots = supportedLots;
            _tmpe = tmpe;
            _settingsController = settingsController;
        }

        public bool HasPendingWork => _activeBuilding != 0 || _pendingBuildings.Count > 0;

        public bool RequestForBuilding(ushort buildingId)
        {
            if (buildingId == 0) return false;
            if (_disposed) return false;
            if (!_isFeatureActive.IsActive) return false;
            if (ParkingDebugSettings.DisableParkingEnforcement) return false;
            if (!IsBuildingSupported(buildingId))
            {
                CleanupRuleIfUnsupported(buildingId);
                return false;
            }

            if (Log.IsEnforcementDebugEnabled && ParkingDebugSettings.IsBuildingDebugEnabled(buildingId))
                Log.Info("[Parking] Reevaluation requested for buildingId=" +
                         buildingId + "\n" + Environment.StackTrace);

            if (!_pendingSet.Add(buildingId))
                return false;

            _pendingBuildings.Enqueue(buildingId);
            if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
                Log.Info("[Parking] Reevaluation queued buildingId=" + buildingId);
            Schedule();
            return true;
        }

        public bool TryRequestNextScheduledBuilding(bool resetSweep)
        {
            if (_disposed) return false;
            if (ParkingDebugSettings.DisableParkingEnforcement) return false;

            if (resetSweep || _resetSweepPending)
            {
                EnsureSweepList();
                _sweepIndex = 0;
                _resetSweepPending = false;
            }

            if (!_isFeatureActive.IsActive) return false;
            if (HasPendingWork) return false;

            EnsureSweepList();
            while (_sweepIndex < _sweepBuildings.Count)
            {
                ushort buildingId = _sweepBuildings[_sweepIndex++];
                if (_pendingSet.Contains(buildingId))
                    continue;

                bool queued = RequestForBuilding(buildingId);
                if (!queued)
                    continue;
                return true;
            }

            return false;
        }

        public void NotifyDayChanged()
        {
            SimThread.Dispatch(ApplyDayChanged);
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

            if (ParkingDebugSettings.DisableParkingEnforcement)
            {
                ClearAll();
                return;
            }

            if (_activeBuilding == 0 || (_activeIndex >= _parkedBuffer.Count && _deniedQueue.Count == 0))
            {
                FinishActiveBuilding();
                if (!TryBeginNextBuilding())
                    return;
            }

            ushort logBuildingId = _activeBuilding;
            int evaluationsThisTick = 0;
            int relocationsThisTick = 0;
            int finalizationsThisTick = 0;
            int maxEvaluations = GetMaxEvaluationsPerTick();
            int maxRelocations = GetMaxRelocationsPerTick();
            int maxFinalizations = GetMaxFinalizationsPerTick();
            bool stuckFixEnabled = IsStuckFixEnabled();

            while (evaluationsThisTick < maxEvaluations && _activeIndex < _parkedBuffer.Count)
            {
                ushort parkedId = _parkedBuffer[_activeIndex++];
                if (!_game.TryGetParkedVehicleReevaluationInfo(
                        parkedId,
                        out uint ownerCitizenId,
                        out ushort homeId,
                        out Vector3 parkedPos,
                        out ushort flags,
                        out bool ownerRoundTrip,
                        out bool isStuckCandidate))
                {
                    evaluationsThisTick++;
                    continue;
                }

                if (stuckFixEnabled && isStuckCandidate)
                {
                    _stuckSeenToday[parkedId] = ownerCitizenId;
                    bool sameOwnerPersisted =
                        _stuckSeenYesterday.TryGetValue(parkedId, out uint prevOwner) &&
                        prevOwner == ownerCitizenId;
                    if (finalizationsThisTick < maxFinalizations && sameOwnerPersisted)
                    {
                        if (_game.TryFinalizeStuckOwnedParkedVehicle(parkedId))
                        {
                            finalizationsThisTick++;
                            _activeFixedCount++;
                            ParkingStatsCounter.IncrementInvisiblesFixed();
                        }
                    }
                }

                var eval = _evaluator.EvaluateCitizen(ownerCitizenId, _activeBuilding);
                if (Log.IsVerboseEnabled && Log.IsDecisionDebugEnabled &&
                    ParkingDebugSettings.BuildingDebugId != 0 &&
                    ParkingDebugSettings.BuildingDebugId == _activeBuilding)
                {
                    Log.Info("[Parking] Reevaluation decision parkedId=" + parkedId
                             + " citizenId=" + ownerCitizenId
                             + " allowed=" + eval.Allowed
                             + " reason=" + eval.Reason
                             + " buildingId=" + _activeBuilding);
                }
                if (eval.Allowed)
                {
                    _activeAllowedCount++;
                    evaluationsThisTick++;
                    continue;
                }

                _activeDeniedCount++;
                if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
                {
                    Log.Info("[Parking] Reevaluation: vehicle denied (queue relocate/release) parkedId=" + parkedId
                             + " citizenId=" + ownerCitizenId
                             + " reason=" + eval.Reason);
                }

                _deniedQueue.Enqueue(new DeniedParkedVehicle(parkedId, ownerCitizenId, homeId, parkedPos));
                ParkingStatsCounter.IncrementReevalDeniedQueued();
                evaluationsThisTick++;
            }

            while (relocationsThisTick < maxRelocations && _deniedQueue.Count > 0)
            {
                DeniedParkedVehicle denied = _deniedQueue.Dequeue();
                bool moved = _tmpe.TryMoveParkedVehicleWithConfigDistance(
                    parkedVehicleId: denied.ParkedVehicleId,
                    ownerCitizenId: denied.OwnerCitizenId,
                    homeId: denied.HomeId,
                    refPos: denied.ParkedPos
                );

                if (!moved)
                {
                    ParkedVehicleRemovalLogger.LogIfMatchesLot(
                        parkedVehicleId: denied.ParkedVehicleId,
                        buildingId: _activeBuilding,
                        source: "Reevaluation.ReleaseParkedVehicle");
                    Singleton<VehicleManager>.instance.ReleaseParkedVehicle(denied.ParkedVehicleId);
                    _activeReleasedCount++;
                    ParkingStatsCounter.IncrementReevalReleased();
                }
                else
                {
                    _activeMovedCount++;
                    ParkingStatsCounter.IncrementReevalMoved();
                }

                relocationsThisTick++;
            }

                if (Log.IsVerboseEnabled && Log.IsEnforcementDebugEnabled)
                    Log.Info("[Parking] Reevaluation tick eval=" + evaluationsThisTick +
                             " relocate=" + relocationsThisTick +
                             " finalize=" + finalizationsThisTick +
                             " buildingId=" + logBuildingId);
            if (_activeIndex >= _parkedBuffer.Count && _deniedQueue.Count == 0)
                FinishActiveBuilding();
            if (HasPendingWork || _deniedQueue.Count > 0)
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
                _deniedQueue.Clear();
                _activeAllowedCount = 0;
                _activeDeniedCount = 0;
                _activeMovedCount = 0;
                _activeReleasedCount = 0;
                _activeFixedCount = 0;

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
                         + " released=" + _activeReleasedCount
                         + " fixed=" + _activeFixedCount);
            }

            _pendingSet.Remove(_activeBuilding);
            _activeBuilding = 0;
            _activeIndex = 0;
            _parkedBuffer.Clear();
            _deniedQueue.Clear();
            _activeParkedCount = 0;
            _activeAllowedCount = 0;
            _activeDeniedCount = 0;
            _activeMovedCount = 0;
            _activeReleasedCount = 0;
            _activeFixedCount = 0;
        }

        private void ClearAll()
        {
            _pendingBuildings.Clear();
            _pendingSet.Clear();
            _activeBuilding = 0;
            _activeIndex = 0;
            _parkedBuffer.Clear();
            _deniedQueue.Clear();
            _sweepBuildings.Clear();
            _sweepIndex = 0;
            _rulesVersionSeen = -1;
            _scheduled = false;
            _resetSweepPending = false;
            _stuckSeenToday.Clear();
            _stuckSeenYesterday.Clear();
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

        private int GetMaxEvaluationsPerTick()
        {
            var settings = _settingsController?.Current;
            int value = settings != null ? settings.ReevaluationMaxEvaluationsPerTick : DefaultMaxEvaluationsPerTick;
            return Math.Max(1, value);
        }

        private int GetMaxRelocationsPerTick()
        {
            var settings = _settingsController?.Current;
            int value = settings != null ? settings.ReevaluationMaxRelocationsPerTick : DefaultMaxRelocationsPerTick;
            return Math.Max(1, value);
        }

        private int GetMaxFinalizationsPerTick()
        {
            return Math.Max(1, DefaultMaxFinalizationsPerTick);
        }

        private bool IsStuckFixEnabled()
        {
            var settings = _settingsController?.Current;
            return settings == null || settings.EnableStuckParkedVehicleFix;
        }

        private void RollStuckSeenForward()
        {
            if (_stuckSeenToday.Count == 0)
            {
                _stuckSeenYesterday.Clear();
                return;
            }

            _stuckSeenYesterday.Clear();
            foreach (var kvp in _stuckSeenToday)
                _stuckSeenYesterday[kvp.Key] = kvp.Value;
            _stuckSeenToday.Clear();
        }

        private void ApplyDayChanged()
        {
            if (_disposed)
                return;

            _resetSweepPending = IsSweepComplete();
            RollStuckSeenForward();
        }

        private bool IsSweepComplete()
        {
            EnsureSweepList();
            if (HasPendingWork)
                return false;
            return _sweepIndex >= _sweepBuildings.Count;
        }

        private readonly struct DeniedParkedVehicle
        {
            public readonly ushort ParkedVehicleId;
            public readonly uint OwnerCitizenId;
            public readonly ushort HomeId;
            public readonly Vector3 ParkedPos;

            public DeniedParkedVehicle(ushort parkedVehicleId, uint ownerCitizenId, ushort homeId, Vector3 parkedPos)
            {
                ParkedVehicleId = parkedVehicleId;
                OwnerCitizenId = ownerCitizenId;
                HomeId = homeId;
                ParkedPos = parkedPos;
            }
        }
    }
}
