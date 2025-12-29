using UnityEngine;
using PickyParking.Infrastructure;
using PickyParking.Infrastructure.Persistence;
using PickyParking.Features.ParkingPolicing;
using PickyParking.UI;

namespace PickyParking.Features.ParkingRules
{
    public sealed class ParkingRulesConfigEditor
    {
        private const ushort AllRadiusMeters = ushort.MaxValue;

        private readonly ParkingRulesConfigRegistry _parkingRulesRepository;
        private readonly ParkingRulePreviewState _previewState;
        private readonly ParkedVehicleReevaluation _reevaluation;
        private readonly float _defaultSliderValue;

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
            _defaultSliderValue = ConvertRadiusToSliderValue(UiConfig.DefaultRadiusMeters);
        }

        public float GetDefaultSliderValue()
        {
            return _defaultSliderValue;
        }

        public ParkingRulesConfigDefinition GetRuleForBuilding(ushort buildingId)
        {
            if (_parkingRulesRepository == null || buildingId == 0)
                return new ParkingRulesConfigDefinition(false, UiConfig.DefaultRadiusMeters, false, UiConfig.DefaultRadiusMeters, false);

            if (_parkingRulesRepository.TryGet(buildingId, out var rule))
                return rule;

            return new ParkingRulesConfigDefinition(false, UiConfig.DefaultRadiusMeters, false, UiConfig.DefaultRadiusMeters, false);
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

        public void CommitPendingChanges(ushort buildingId, ParkingRulesConfigUiState state)
        {
            if (buildingId == 0 || state == null || _parkingRulesRepository == null)
                return;

            ParkingRulesConfigDefinition rule = BuildRuleFromUi(state);
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

        public void UpdatePreview(ushort buildingId, ParkingRulesConfigUiState state)
        {
            if (buildingId == 0 || state == null || _previewState == null)
                return;

            ParkingRulesConfigDefinition rule = BuildRuleFromUi(state);
            _previewState.SetPreview(buildingId, rule);
        }

        public void ApplyRuleNow(ushort buildingId, ParkingRulesConfigUiState state, string reason)
        {
            if (buildingId == 0 || state == null || _parkingRulesRepository == null)
                return;

            ParkingRulesConfigDefinition rule = BuildRuleFromUi(state);
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

        public float ConvertRadiusToSliderValue(ushort radiusMeters)
        {
            if (radiusMeters == 0)
                return 0f;

            if (radiusMeters >= AllRadiusMeters)
                return 1f;

            float clamped = Mathf.Clamp(radiusMeters, UiConfig.MinDistanceMeters, UiConfig.MaxDistanceMeters);
            if (clamped <= UiConfig.MinDistanceMeters)
                return UiConfig.DistanceSliderMinValue;
            if (clamped >= UiConfig.MaxDistanceMeters)
                return UiConfig.DistanceSliderMaxValue;

            return DistanceSliderMapping.DistanceMetersToSlider(
                clamped,
                UiConfig.DistanceSliderMinValue,
                UiConfig.DistanceSliderMaxValue,
                UiConfig.MinDistanceMeters,
                UiConfig.MidDistanceMeters,
                UiConfig.MaxDistanceMeters,
                UiConfig.DistanceMidpointT);
        }

        public string FormatRule(ParkingRulesConfigDefinition rule)
        {
            return "ResidentsOnly=" + rule.ResidentsWithinRadiusOnly + " (" + rule.ResidentsRadiusMeters + "m), "
                   + "WorkSchoolOnly=" + rule.WorkSchoolWithinRadiusOnly + " (" + rule.WorkSchoolRadiusMeters + "m), "
                   + "VisitorsAllowed=" + rule.VisitorsAllowed;
        }

        public ParkingRulesConfigDefinition BuildRuleFromUi(ParkingRulesConfigUiState state)
        {
            bool residentsEnabled = state.ResidentsEnabled;
            bool workEnabled = state.WorkSchoolEnabled;

            float resValue = residentsEnabled ? state.ResidentsSliderValue : GetStoredSliderValue(state.ResidentsStoredValue);
            float workValue = workEnabled ? state.WorkSchoolSliderValue : GetStoredSliderValue(state.WorkSchoolStoredValue);

            ushort residentsRadius = ConvertSliderValueToRadius(resValue);
            ushort workRadius = ConvertSliderValueToRadius(workValue);

            return new ParkingRulesConfigDefinition(
                residentsEnabled,
                residentsRadius,
                workEnabled,
                workRadius,
                state.VisitorsAllowed);
        }

        private float GetStoredSliderValue(float stored)
        {
            return stored > 0f ? stored : _defaultSliderValue;
        }

        private ushort ConvertSliderValueToRadius(float normalizedSliderValue)
        {
            if (normalizedSliderValue <= 0f)
                return 0;

            if (normalizedSliderValue >= 1f)
                return AllRadiusMeters;

            if (normalizedSliderValue <= UiConfig.DistanceSliderMinValue)
                return UiConfig.MinDistanceMeters;
            if (normalizedSliderValue >= UiConfig.DistanceSliderMaxValue)
                return UiConfig.MaxDistanceMeters;

            float meters = DistanceSliderMapping.SliderToDistanceMeters(
                normalizedSliderValue,
                UiConfig.DistanceSliderMinValue,
                UiConfig.DistanceSliderMaxValue,
                UiConfig.MinDistanceMeters,
                UiConfig.MidDistanceMeters,
                UiConfig.MaxDistanceMeters,
                UiConfig.DistanceMidpointT);
            int rounded = Mathf.RoundToInt(meters);
            if (rounded < UiConfig.MinDistanceMeters) rounded = UiConfig.MinDistanceMeters;
            if (rounded > UiConfig.MaxDistanceMeters) rounded = UiConfig.MaxDistanceMeters;
            return (ushort)rounded;
        }
    }
}
