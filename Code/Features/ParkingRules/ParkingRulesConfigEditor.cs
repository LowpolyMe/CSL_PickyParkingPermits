using PickyParking.Logging;
using PickyParking.ModLifecycle;
using PickyParking.Features.ParkingPolicing;
using PickyParking.UI.BuildingOptionsPanel.ParkingRulesPanel;

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

            if (Log.IsVerboseEnabled)
                Log.Info("[ParkingRules] RemoveRule (" + reason + ") building=" + buildingId);

            SimThread.Dispatch(() => _parkingRulesRepository.Remove(buildingId));
        }

        public void CommitPendingChanges(ushort buildingId, ParkingRulesConfigInput input)
        {
            if (buildingId == 0 || input == null || _parkingRulesRepository == null)
                return;

            ParkingRulesConfigDefinition rule = BuildRuleFromInput(input);
            if (Log.IsVerboseEnabled)
                Log.Info("[ParkingRules] CommitPendingChanges building=" + buildingId + " rule=" + FormatRule(rule));

            SimThread.Dispatch(() => _parkingRulesRepository.Set(buildingId, rule));
        }

        public void ClearPreview(ushort buildingId)
        {
            if (buildingId == 0 || _previewState == null)
                return;

            _previewState.Clear(buildingId);
        }

        public void UpdatePreview(ushort buildingId, ParkingRulesConfigInput input)
        {
            if (buildingId == 0 || input == null || _previewState == null)
                return;

            ParkingRulesConfigDefinition rule = BuildRuleFromInput(input);
            _previewState.SetPreview(buildingId, rule);
        }

        public void ApplyRuleNow(ushort buildingId, ParkingRulesConfigInput input, string reason)
        {
            if (buildingId == 0 || input == null || _parkingRulesRepository == null)
                return;

            ParkingRulesConfigDefinition rule = BuildRuleFromInput(input);
            _hasPendingReevaluation = true;
            _pendingReevaluationBuildingId = buildingId;

            if (Log.IsVerboseEnabled)
                Log.Info("[ParkingRules] ApplyNow (" + reason + ") building=" + buildingId + " rule=" + FormatRule(rule) + " reevaluate=deferred");

            SimThread.Dispatch(() => _parkingRulesRepository.Set(buildingId, rule));
        }

        public void RequestPendingReevaluationIfAny(ushort buildingId)
        {
            if (!_hasPendingReevaluation)
            {
                if (Log.IsVerboseEnabled)
                    Log.Info("[ParkingRules] Reevaluation skipped (none pending) for building " + buildingId);
                return;
            }

            if (_pendingReevaluationBuildingId != buildingId)
            {
                if (Log.IsVerboseEnabled)
                    Log.Info("[ParkingRules] Reevaluation skipped (pending for " + _pendingReevaluationBuildingId + ") for building " + buildingId);
                return;
            }

            if (_reevaluation == null)
            {
                if (Log.IsVerboseEnabled)
                    Log.Info("[ParkingRules] Reevaluation skipped (service missing) for building " + buildingId);
                return;
            }

            _hasPendingReevaluation = false;

            if (Log.IsVerboseEnabled)
                Log.Info("[ParkingRules] Reevaluation requested for building " + buildingId);

            SimThread.Dispatch(() => _reevaluation.RequestForBuilding(buildingId));
        }

        public string FormatRule(ParkingRulesConfigDefinition rule)
        {
            return "ResidentsOnly=" + rule.ResidentsWithinRadiusOnly + " (" + rule.ResidentsRadiusMeters + "m), "
                   + "WorkSchoolOnly=" + rule.WorkSchoolWithinRadiusOnly + " (" + rule.WorkSchoolRadiusMeters + "m), "
                   + "VisitorsAllowed=" + rule.VisitorsAllowed;
        }

        public ParkingRulesConfigDefinition BuildRuleFromInput(ParkingRulesConfigInput input)
        {
            return new ParkingRulesConfigDefinition(
                input.ResidentsEnabled,
                input.ResidentsRadiusMeters,
                input.WorkSchoolEnabled,
                input.WorkSchoolRadiusMeters,
                input.VisitorsAllowed);
        }
    }
}
