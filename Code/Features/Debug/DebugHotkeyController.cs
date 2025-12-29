using System.Collections.Generic;
using UnityEngine;
using PickyParking.Domain;
using PickyParking.Infrastructure;
using PickyParking.Infrastructure.Integration;
using PickyParking.Infrastructure.Persistence;
using PickyParking.Settings;
using PickyParking.Features.ParkingPolicing;

namespace PickyParking.Features.Debug
{
    
    
    
    public sealed class DebugHotkeyController
    {
        private const ushort DefaultRadiusMeters = 200;

        private readonly GameAccess _gameAccess;
        private readonly PrefabIdentity _prefabIdentity;
        private readonly SupportedParkingLotRegistry _supportedParkingLotRegistry;
        private readonly ModSettingsController _settingsController;
        private readonly ParkingRulesConfigRegistry _parkingRulesRepository;
        private readonly ParkedVehicleReevaluation _parkedVehicleReevaluation;

        public DebugHotkeyController(
            GameAccess gameAccess,
            PrefabIdentity prefabIdentity,
            SupportedParkingLotRegistry supportedParkingLotRegistry,
            ModSettingsController settingsController,
            ParkingRulesConfigRegistry parkingRulesRepository,
            ParkedVehicleReevaluation parkedVehicleReevaluation)
        {
            _gameAccess = gameAccess;
            _prefabIdentity = prefabIdentity;
            _supportedParkingLotRegistry = supportedParkingLotRegistry;
            _settingsController = settingsController;
            _parkingRulesRepository = parkingRulesRepository;
            _parkedVehicleReevaluation = parkedVehicleReevaluation;
        }

        public bool TryToggleSupportedPrefab()
        {
            if (!TryGetSelectedBuilding(out _, out BuildingInfo info))
                return false;

            PrefabKey key = _prefabIdentity.CreateKey(info);
            bool enabled = _supportedParkingLotRegistry.Toggle(key);

            if (_settingsController == null)
            {
                Log.Warn("[DebugHotkeys] Settings controller missing; prefab toggle not persisted.");
                return true;
            }

            _settingsController.Current.SupportedParkingLotPrefabs =
                new List<PrefabKey>(_supportedParkingLotRegistry.EnumerateKeys());
            _settingsController.Save("DebugHotkeyController.ToggleSupportedPrefabForSelection");

            if (Log.IsVerboseEnabled)
                Log.Info(enabled ? $"[DebugHotkeys] Enabled for prefab {key}" : $"[DebugHotkeys] Disabled for prefab {key}");

            return true;
        }

        public bool TryRequestReevaluationForSelection()
        {
            if (!TryGetSelectedBuilding(out ushort buildingId, out _))
                return false;

            if (_parkedVehicleReevaluation == null)
            {
                Log.Warn("[DebugHotkeys] Reevaluation service missing.");
                return false;
            }

            if (Log.IsVerboseEnabled)
                Log.Info("[DebugHotkeys] Reevaluation requested for building " + buildingId);

            SimThread.Dispatch(() => _parkedVehicleReevaluation.RequestForBuilding(buildingId));
            return true;
        }

        public bool TryToggleDefaultRule()
        {
            if (!TryGetSelectedBuilding(out ushort buildingId, out _))
                return false;

            SimThread.Dispatch(() =>
            {
                if (_parkingRulesRepository.TryGet(buildingId, out _))
                {
                    _parkingRulesRepository.Remove(buildingId);
                    if (Log.IsVerboseEnabled)
                        Log.Info($"[DebugHotkeys] Removed rule for building {buildingId}");
                    return;
                }

                ParkingRulesConfigDefinition rule = CreateDefaultRule();
                _parkingRulesRepository.Set(buildingId, rule);
                if (Log.IsVerboseEnabled)
                    Log.Info($"[DebugHotkeys] Created default rule for building {buildingId}: {FormatRule(rule)}");
            });

            return true;
        }

        public bool TryToggleResidentsRule()
        {
            if (!TryGetSelectedBuilding(out ushort buildingId, out _))
                return false;

            SimThread.Dispatch(() =>
            {
                ParkingRulesConfigDefinition current = GetOrCreateRule(buildingId);
                ParkingRulesConfigDefinition updated = new ParkingRulesConfigDefinition(
                    !current.ResidentsWithinRadiusOnly,
                    current.ResidentsRadiusMeters,
                    current.WorkSchoolWithinRadiusOnly,
                    current.WorkSchoolRadiusMeters,
                    current.VisitorsAllowed);

                SetOrRemove(buildingId, updated);
                if (Log.IsVerboseEnabled)
                    Log.Info($"[DebugHotkeys] Residents restriction toggled for building {buildingId}: {FormatRule(updated)}");
            });

            return true;
        }

        public bool TryToggleWorkSchoolRule()
        {
            if (!TryGetSelectedBuilding(out ushort buildingId, out _))
                return false;

            SimThread.Dispatch(() =>
            {
                ParkingRulesConfigDefinition current = GetOrCreateRule(buildingId);
                ParkingRulesConfigDefinition updated = new ParkingRulesConfigDefinition(
                    current.ResidentsWithinRadiusOnly,
                    current.ResidentsRadiusMeters,
                    !current.WorkSchoolWithinRadiusOnly,
                    current.WorkSchoolRadiusMeters,
                    current.VisitorsAllowed);

                SetOrRemove(buildingId, updated);
                if (Log.IsVerboseEnabled)
                    Log.Info($"[DebugHotkeys] Work/School restriction toggled for building {buildingId}: {FormatRule(updated)}");
            });

            return true;
        }

        public bool TryToggleVisitorsRule()
        {
            if (!TryGetSelectedBuilding(out ushort buildingId, out _))
                return false;

            SimThread.Dispatch(() =>
            {
                ParkingRulesConfigDefinition current = GetOrCreateRule(buildingId);
                ParkingRulesConfigDefinition updated = new ParkingRulesConfigDefinition(
                    current.ResidentsWithinRadiusOnly,
                    current.ResidentsRadiusMeters,
                    current.WorkSchoolWithinRadiusOnly,
                    current.WorkSchoolRadiusMeters,
                    !current.VisitorsAllowed);

                SetOrRemove(buildingId, updated);
                if (Log.IsVerboseEnabled)
                    Log.Info($"[DebugHotkeys] Visitor restriction toggled for building {buildingId}: {FormatRule(updated)}");
            });

            return true;
        }

        public bool TryDumpSelectionState()
        {
            if (!TryGetSelectedBuilding(out ushort buildingId, out BuildingInfo info))
                return false;

            PrefabKey key = _prefabIdentity.CreateKey(info);
            bool supported = _supportedParkingLotRegistry.Contains(key);

            string ruleText = _parkingRulesRepository.TryGet(buildingId, out var rule)
                ? FormatRule(rule)
                : "<none>";

            if (Log.IsVerboseEnabled)
                Log.Info($"[DebugHotkeys] Selected building {buildingId} | Prefab={key} | SupportedPrefab={supported} | Rule={ruleText}");

            return true;
        }

        private bool TryGetSelectedBuilding(out ushort buildingId, out BuildingInfo info)
        {
            buildingId = 0;
            info = null;

            if (_gameAccess == null)
                return false;

            if (!_gameAccess.TryGetSelectedBuilding(out buildingId, out info))
            {
                if (Log.IsVerboseEnabled)
                    Log.Info("[DebugHotkeys] No selected building.");
                return false;
            }

            if (info == null)
            {
                if (Log.IsVerboseEnabled)
                    Log.Info("[DebugHotkeys] Selected building has no info.");
                return false;
            }

            return true;
        }

        private ParkingRulesConfigDefinition GetOrCreateRule(ushort buildingId)
        {
            if (_parkingRulesRepository.TryGet(buildingId, out var existing))
                return existing;

            return new ParkingRulesConfigDefinition(
                residentsWithinRadiusOnly: false,
                residentsRadiusMeters: DefaultRadiusMeters,
                workSchoolWithinRadiusOnly: false,
                workSchoolRadiusMeters: DefaultRadiusMeters,
                visitorsAllowed: false);
        }

        private void SetOrRemove(ushort buildingId, ParkingRulesConfigDefinition rule)
        {
            if (rule.IsUnrestricted)
                _parkingRulesRepository.Remove(buildingId);
            else
                _parkingRulesRepository.Set(buildingId, rule);
        }

        private static ParkingRulesConfigDefinition CreateDefaultRule()
            => new ParkingRulesConfigDefinition(
                residentsWithinRadiusOnly: true,
                residentsRadiusMeters: DefaultRadiusMeters,
                workSchoolWithinRadiusOnly: true,
                workSchoolRadiusMeters: DefaultRadiusMeters,
                false);

        private static string FormatRule(ParkingRulesConfigDefinition rule)
            => $"ResidentsOnly={rule.ResidentsWithinRadiusOnly} ({rule.ResidentsRadiusMeters}m), "
               + $"WorkSchoolOnly={rule.WorkSchoolWithinRadiusOnly} ({rule.WorkSchoolRadiusMeters}m), "
               + $"VisitorsAllowed={rule.VisitorsAllowed}";
    }
}
