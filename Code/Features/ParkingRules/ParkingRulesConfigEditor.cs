using PickyParking.Features.Debug;
using PickyParking.Logging;
using PickyParking.ModLifecycle;
using PickyParking.Features.ParkingPolicing;
using PickyParking.UI.BuildingOptionsPanel.ParkingRulesPanel;
using PickyParking.Settings;

namespace PickyParking.Features.ParkingRules
{
    public sealed class ParkingRulesConfigEditor
    {
        private readonly ParkingRulesConfigRegistry _parkingRulesRepository;
        private readonly ParkingRulePreviewState _previewState;
        private readonly ParkedVehicleReevaluation _reevaluation;
        private bool _hasPendingReevaluation;
        private ushort _pendingReevaluationBuildingId;

        public ParkingRulesConfigUiConfig UiConfig { get; private set; }

        public ParkingRulesConfigEditor(
            ParkingRulesConfigRegistry parkingRulesRepository,
            ParkingRulePreviewState previewState,
            ParkedVehicleReevaluation reevaluation)
        {
            _parkingRulesRepository = parkingRulesRepository;
            _previewState = previewState;
            _reevaluation = reevaluation;
            UiConfig = ParkingRulesConfigUiConfig.Default;
        }

        public ParkingRulesConfigDefinition GetRuleForBuilding(ushort buildingId)
        {
            if (_parkingRulesRepository == null || buildingId == 0)
                return new ParkingRulesConfigDefinition(false, ParkingRulesLimits.DefaultRadiusMeters, false, ParkingRulesLimits.DefaultRadiusMeters, false);

            if (_parkingRulesRepository.TryGet(buildingId, out var rule))
                return rule;

            return new ParkingRulesConfigDefinition(false, ParkingRulesLimits.DefaultRadiusMeters, false, ParkingRulesLimits.DefaultRadiusMeters, false);
        }

        public bool TryGetStoredRule(ushort buildingId, out ParkingRulesConfigDefinition rule)
        {
            rule = default;
            if (_parkingRulesRepository == null || buildingId == 0)
                return false;

            return _parkingRulesRepository.TryGet(buildingId, out rule);
        }

        public void RemoveRule(ushort buildingId, string reason)
        {
            if (buildingId == 0 || _parkingRulesRepository == null)
                return;

            if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
            {
                string fields = "buildingId=" + buildingId;
                if (!string.IsNullOrEmpty(reason))
                    fields = fields + " | reason=" + reason;
                Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "RuleRemoved", fields);
            }

            SimThread.Dispatch(() => _parkingRulesRepository.Remove(buildingId));
        }

        public void CommitPendingChanges(ushort buildingId, ParkingRulesConfigDefinition rule)
        {
            if (buildingId == 0 || _parkingRulesRepository == null)
                return;

            if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
            {
                Log.Dev.Info(
                    DebugLogCategory.RuleUi,
                    LogPath.Any,
                    "RuleCommitted",
                    "buildingId=" + buildingId + " | rule=" + FormatRule(rule));
            }

            SimThread.Dispatch(() => _parkingRulesRepository.Set(buildingId, rule));
        }

        public void ClearPreview(ushort buildingId)
        {
            if (buildingId == 0 || _previewState == null)
                return;

            _previewState.Clear(buildingId);
        }

        public void UpdatePreview(ushort buildingId, ParkingRulesConfigDefinition rule)
        {
            if (buildingId == 0 || _previewState == null)
                return;

            _previewState.SetPreview(buildingId, rule);
        }

        public void ApplyRuleNow(ushort buildingId, ParkingRulesConfigDefinition rule, string reason)
        {
            if (buildingId == 0 || _parkingRulesRepository == null)
                return;

            _hasPendingReevaluation = true;
            _pendingReevaluationBuildingId = buildingId;

            if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
            {
                string fields = "buildingId=" + buildingId + " | rule=" + FormatRule(rule) + " | reevaluate=deferred";
                if (!string.IsNullOrEmpty(reason))
                    fields = fields + " | reason=" + reason;
                Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "RuleApplyNow", fields);
            }

            SimThread.Dispatch(() => _parkingRulesRepository.Set(buildingId, rule));
        }

        public void RequestPendingReevaluationIfAny(ushort buildingId)
        {
            if (!_hasPendingReevaluation)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                {
                    Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "ReevaluationSkippedNonePending", "buildingId=" + buildingId);
                }
                return;
            }

            if (_pendingReevaluationBuildingId != buildingId)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                {
                    Log.Dev.Info(
                        DebugLogCategory.RuleUi,
                        LogPath.Any,
                        "ReevaluationSkippedOtherPending",
                        "buildingId=" + buildingId + " | pendingBuildingId=" + _pendingReevaluationBuildingId);
                }
                return;
            }

            if (_reevaluation == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                {
                    Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "ReevaluationSkippedServiceMissing", "buildingId=" + buildingId);
                }
                return;
            }

            _hasPendingReevaluation = false;

            if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
            {
                Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "ReevaluationRequested", "buildingId=" + buildingId);
            }

            SimThread.Dispatch(() => { _reevaluation.RequestForBuilding(buildingId); });
        }

        public string FormatRule(ParkingRulesConfigDefinition rule)
        {
            return "ResidentsOnly=" + rule.ResidentsWithinRadiusOnly + " (" + rule.ResidentsRadiusMeters + "m), "
                   + "WorkSchoolOnly=" + rule.WorkSchoolWithinRadiusOnly + " (" + rule.WorkSchoolRadiusMeters + "m), "
                   + "VisitorsAllowed=" + rule.VisitorsAllowed;
        }
    }
}

