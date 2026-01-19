using System.Collections.Generic;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.Features.ParkingRules;
using PickyParking.Logging;
using PickyParking.ModLifecycle;
using PickyParking.Settings;
using PickyParking.Features.ParkingPolicing;
using PickyParking.GameAdapters;

namespace PickyParking.Features.Debug
{
    public sealed class DebugHotkeyController
    {
        private const ushort DefaultRadiusMeters = ParkingRulesLimits.DefaultRadiusMeters;

        private readonly GameAccess _gameAccess;
        private readonly SupportedParkingLotRegistry _supportedParkingLotRegistry;
        private readonly ModSettingsController _settingsController;
        private readonly ParkingRulesConfigRegistry _parkingRulesRepository;
        private readonly ParkedVehicleReevaluation _parkedVehicleReevaluation;

        public DebugHotkeyController(
            GameAccess gameAccess,
            SupportedParkingLotRegistry supportedParkingLotRegistry,
            ModSettingsController settingsController,
            ParkingRulesConfigRegistry parkingRulesRepository,
            ParkedVehicleReevaluation parkedVehicleReevaluation)
        {
            _gameAccess = gameAccess;
            _supportedParkingLotRegistry = supportedParkingLotRegistry;
            _settingsController = settingsController;
            _parkingRulesRepository = parkingRulesRepository;
            _parkedVehicleReevaluation = parkedVehicleReevaluation;
        }

        public bool TryToggleSupportedPrefab()
        {
            if (!TryGetSelectedBuilding(out _, out BuildingInfo info))
                return false;

            PrefabKey key = ParkingLotPrefabKeyFactory.CreateKey(info);
            bool enabled = _supportedParkingLotRegistry.Toggle(key);

            if (_settingsController == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                {
                    Log.Dev.Warn(DebugLogCategory.RuleUi, LogPath.Any, "DebugHotkeySettingsControllerMissing");
                }
                return true;
            }

            _settingsController.Current.SupportedParkingLotPrefabs =
                new List<PrefabKey>(_supportedParkingLotRegistry.EnumerateKeys());
            _settingsController.Save( "DebugHotkeyController.ToggleSupportedPrefabForSelection");

            if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
            {
                Log.Dev.Info(
                    DebugLogCategory.RuleUi,
                    LogPath.Any,
                    "DebugHotkeyPrefabToggled",
                    "prefab=" + key + " | enabled=" + enabled);
            }

            return true;
        }

        public bool TryRequestReevaluationForSelection()
        {
            if (!TryGetSelectedBuilding(out ushort buildingId, out _))
                return false;

            if (_parkedVehicleReevaluation == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                {
                    Log.Dev.Warn(DebugLogCategory.RuleUi, LogPath.Any, "DebugHotkeyReevaluationServiceMissing");
                }
                return false;
            }

            if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
            {
                Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "DebugHotkeyReevaluationRequested", "buildingId=" + buildingId);
            }

            SimThread.Dispatch(() => { _parkedVehicleReevaluation.RequestForBuilding(buildingId); });
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
                    if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                    {
                        Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "DebugHotkeyRuleRemoved", "buildingId=" + buildingId);
                    }
                    return;
                }

                ParkingRulesConfigDefinition rule = CreateDefaultRule();
                _parkingRulesRepository.Set(buildingId, rule);
                if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                {
                    Log.Dev.Info(
                        DebugLogCategory.RuleUi,
                        LogPath.Any,
                        "DebugHotkeyRuleCreated",
                        "buildingId=" + buildingId + " | rule=" + FormatRule(rule));
                }
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
                if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                {
                    Log.Dev.Info(
                        DebugLogCategory.RuleUi,
                        LogPath.Any,
                        "DebugHotkeyResidentsToggled",
                        "buildingId=" + buildingId + " | rule=" + FormatRule(updated));
                }
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
                if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                {
                    Log.Dev.Info(
                        DebugLogCategory.RuleUi,
                        LogPath.Any,
                        "DebugHotkeyWorkSchoolToggled",
                        "buildingId=" + buildingId + " | rule=" + FormatRule(updated));
                }
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
                if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                {
                    Log.Dev.Info(
                        DebugLogCategory.RuleUi,
                        LogPath.Any,
                        "DebugHotkeyVisitorsToggled",
                        "buildingId=" + buildingId + " | rule=" + FormatRule(updated));
                }
            });

            return true;
        }

        public bool TryDumpSelectionState()
        {
            if (!TryGetSelectedBuilding(out ushort buildingId, out BuildingInfo info))
                return false;

            PrefabKey key = ParkingLotPrefabKeyFactory.CreateKey(info);
            bool supported = _supportedParkingLotRegistry.Contains(key);

            string ruleText = _parkingRulesRepository.TryGet(buildingId, out var rule)
                ? FormatRule(rule)
                : "<none>";

            if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
            {
                Log.Dev.Info(
                    DebugLogCategory.RuleUi,
                    LogPath.Any,
                    "DebugHotkeySelectionDump",
                    "buildingId=" + buildingId +
                    " | prefab=" + key +
                    " | supportedPrefab=" + supported +
                    " | rule=" + ruleText);
            }

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
                if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                {
                    Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "DebugHotkeyNoSelectedBuilding");
                }
                return false;
            }

            if (info == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                {
                    Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "DebugHotkeySelectedBuildingMissingInfo");
                }
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

