using UnityEngine;
using ColossalFramework.UI;
using PickyParking.Features.ParkingRules;
using PickyParking.ModEntry;
using PickyParking.Logging;

namespace PickyParking.UI
{
    
    
    
    
    public sealed class ParkingRulesConfigPanel : UIPanel
    {
        private const float SliderAllThreshold = 0.99f;
        private const ushort DefaultNewRuleRadiusMeters = 500;
        private const float ParkingStatsUpdateIntervalSeconds = 0.5f;

        private ParkingRulesConfigPanelState _state;
        private ParkingRulesConfigPanelView _view;
        private ParkingRulesConfigPanelWorkflow _workflow;
        private ParkingRulesConfigUiConfig _uiConfig;
        private ParkingPanelTheme _theme;

        public override void Start()
        {
            base.Start();

            ModRuntime runtime = ModRuntime.Current;
            _workflow = new ParkingRulesConfigPanelWorkflow(
                runtime != null ? runtime.ParkingRulesConfigEditor : null,
                runtime != null ? runtime.GameAccess : null);
            _uiConfig = _workflow.UiConfig;
            _theme = new ParkingPanelTheme();
            _state = new ParkingRulesConfigPanelState();

            _view = ParkingRulesConfigPanelView.Build(
                this,
                _theme,
                _uiConfig,
                GetDefaultSliderValue,
                ToggleRestrictions,
                ToggleSliderRow,
                HandleSliderValueChanged,
                ToggleVisitorsRow,
                ApplyChangesFromButton);
        }

        public override void Update()
        {
            base.Update();
            if (_view == null || !IsPanelVisibleForStats())
                return;

            if (_state.BuildingId == 0)
                return;

            if (Time.unscaledTime < _state.NextParkingStatsUpdateTime)
                return;

            _state.NextParkingStatsUpdateTime = Time.unscaledTime + ParkingStatsUpdateIntervalSeconds;
            UpdateParkingSpaceStats();
        }

        public void Bind(ushort buildingId)
        {
            if (_state.BuildingId != 0 && _state.BuildingId != buildingId)
            {
                DiscardUnappliedChanges();
            }

            _state.BuildingId = buildingId;
            Refresh();
        }

        public void SetPrefabSupported(bool supported)
        {
            _state.IsPrefabSupported = supported;
        }

        public void CommitPendingChanges()
        {
            if (!_state.IsDirty)
                return;

            _state.IsDirty = false;

            if (!CanOperateOnBuilding())
                return;

            if (!_state.RestrictionsEnabled)
                return;

            _workflow.CommitPendingChanges(_state.BuildingId, BuildInput());
        }

        public void DiscardUnappliedChangesIfAny()
        {
            DiscardUnappliedChanges();
        }

        public void ClearPreview()
        {
            if (_workflow == null)
                return;

            _workflow.ClearPreview(_state.BuildingId);
        }

        public void RequestPendingReevaluationIfAny()
        {
            RequestPendingReevaluationIfAny(_state.BuildingId);
        }

        private void ToggleSliderRow(ParkingRulesSliderRow row)
        {
            if (!_state.RestrictionsEnabled)
                return;

            if (row.IsEnabled)
            {
                DisableSliderRow(row);
            }
            else
            {
                EnableSliderRow(row);
            }

            MarkDirty();
            UpdatePreviewRule();
        }

        private void ToggleVisitorsRow()
        {
            if (!_state.RestrictionsEnabled)
                return;

            _view.VisitorsRow.IsEnabled = !_view.VisitorsRow.IsEnabled;
            _view.Visuals.UpdateToggleRowVisuals(_view.VisitorsRow);
            MarkDirty();
            UpdatePreviewRule();
        }

        private void HandleSliderValueChanged(ParkingRulesSliderRow row, float value)
        {
            if (!_state.RestrictionsEnabled)
                return;

            if (_state.IsUpdatingUi)
                return;

            float snappedValue = SnapSliderValue(value);
            if (!Mathf.Approximately(snappedValue, value))
            {
                _state.IsUpdatingUi = true;
                row.Slider.value = snappedValue;
                _state.IsUpdatingUi = false;
            }

            value = snappedValue;

            if (value <= 0f)
            {
                row.IsEnabled = false;
                _view.Visuals.UpdateSliderRowLabel(row);
                _view.Visuals.UpdateSliderRowVisuals(row);
            }
            else
            {
                row.IsEnabled = true;
                row.LastNonZeroValue = value;
                _view.Visuals.UpdateSliderRowLabel(row);
                _view.Visuals.UpdateSliderRowVisuals(row);
            }

            MarkDirty();
            UpdatePreviewRule();
        }

        private void EnableSliderRow(ParkingRulesSliderRow row)
        {
            row.IsEnabled = true;
            if (row.Slider.value <= 0f)
            {
                float restoreValue = row.LastNonZeroValue > 0f ? row.LastNonZeroValue : GetDefaultSliderValue();
                SetSliderValue(row, restoreValue);
            }
            _view.Visuals.UpdateSliderRowVisuals(row);
        }

        private void DisableSliderRow(ParkingRulesSliderRow row)
        {
            row.IsEnabled = false;

            if (row.Slider.value > 0f)
                row.LastNonZeroValue = row.Slider.value;

            _view.Visuals.UpdateSliderRowVisuals(row);
        }

        private void SetSliderValue(ParkingRulesSliderRow row, float value)
        {
            value = SnapSliderValue(value);
            _state.IsUpdatingUi = true;
            row.Slider.value = value;
            if (value > 0f)
                row.LastNonZeroValue = value;
            _view.Visuals.UpdateSliderRowLabel(row);
            _state.IsUpdatingUi = false;
        }

        private float SnapSliderValue(float value)
        {
            if (_uiConfig != null)
            {
                float offThreshold = Mathf.Max(0f, _uiConfig.DistanceSliderMinValue - _uiConfig.SliderStep * 0.5f);
                if (value <= offThreshold)
                    return 0f;
            }
            else if (value <= 0f)
                return 0f;
            if (value >= SliderAllThreshold)
                return 1f;
            return value;
        }

        private void DiscardUnappliedChanges()
        {
            if (!_state.HasUnappliedChanges)
                return;

            ClearPreview();
            ApplyRuleToUi(_state.BaselineRule);
            _state.ResetDirty();
        }

        private void MarkDirty()
        {
            _state.MarkDirty();
        }

        private bool CanOperateOnBuilding()
        {
            return _state.BuildingId != 0 && _workflow != null && _workflow.CanEditRules && _state.IsPrefabSupported;
        }

        private bool CanOperateOnBuilding(ushort buildingId)
        {
            return buildingId != 0 && _workflow != null && _workflow.CanEditRules && _state.IsPrefabSupported;
        }

        private void Refresh()
        {
            if (_workflow == null || !_workflow.CanEditRules)
                return;

            bool hasStoredRule = _workflow.TryGetStoredRule(_state.BuildingId, out var storedRule);

            _state.RestrictionsEnabled = hasStoredRule;
            _state.HasStoredRule = hasStoredRule;

            if (hasStoredRule)
            {
                _state.BaselineRule = storedRule;
                ApplyRuleToUi(storedRule);
            }
            else
            {
                _state.BaselineRule = new ParkingRulesConfigDefinition(
                    residentsWithinRadiusOnly: false,
                    residentsRadiusMeters: ParkingRulesLimits.DefaultRadiusMeters,
                    workSchoolWithinRadiusOnly: false,
                    workSchoolRadiusMeters: ParkingRulesLimits.DefaultRadiusMeters,
                    visitorsAllowed: false);
            }

            _state.ResetDirty();
            UpdateRestrictionsVisibility();
            UpdatePreviewRule();
            UpdateParkingSpaceStats();

            if (hasStoredRule && Log.IsVerboseEnabled)
                Log.Info("[UI] Refreshed panel for building " + _state.BuildingId + ": " + _workflow.FormatRule(storedRule));
        }

        private void ApplyRuleToUi(ParkingRulesConfigDefinition rule)
        {
            ApplySliderRowFromRule(_view.ResidentsRow, rule.ResidentsWithinRadiusOnly, rule.ResidentsRadiusMeters);
            ApplySliderRowFromRule(_view.WorkSchoolRow, rule.WorkSchoolWithinRadiusOnly, rule.WorkSchoolRadiusMeters);

            _view.VisitorsRow.IsEnabled = rule.VisitorsAllowed;
            _view.Visuals.UpdateToggleRowVisuals(_view.VisitorsRow);
        }

        private void ApplySliderRowFromRule(ParkingRulesSliderRow row, bool enabled, ushort radiusMeters)
        {
            if (_view == null)
                return;

            _view.Visuals.ApplySliderRowFromRule(
                row,
                enabled,
                radiusMeters,
                ConvertRadiusToSliderValue,
                SetSliderValue);
        }

        private void UpdatePreviewRule()
        {
            if (!CanOperateOnBuilding())
                return;

            if (!_state.RestrictionsEnabled)
                return;

            _workflow.UpdatePreview(_state.BuildingId, BuildInput());
        }

        private void ApplyChangesInternal(string reason)
        {
            if (!CanOperateOnBuilding())
                return;

            _state.IsDirty = false;
            _workflow.ApplyRuleNow(_state.BuildingId, BuildInput(), reason);
        }

        private void ApplyChangesFromButton()
        {
            if (!CanOperateOnBuilding())
                return;

            if (!_state.RestrictionsEnabled)
                return;

            ParkingRulesConfigInput input = BuildInput();
            _workflow.ApplyRuleNow(_state.BuildingId, input, "ApplyButton");
            _workflow.RequestPendingReevaluationIfAny(_state.BuildingId);
            _state.BaselineRule = _workflow.BuildRuleFromInput(input);
            _state.HasStoredRule = true;
            _state.ResetDirty();
        }

        private void RequestPendingReevaluationIfAny(ushort buildingId)
        {
            if (!CanOperateOnBuilding(buildingId))
                return;

            _workflow.RequestPendingReevaluationIfAny(buildingId);
        }

        private float GetDefaultSliderValue()
        {
            return ConvertRadiusToSliderValue(ParkingRulesLimits.DefaultRadiusMeters);
        }

        private void ToggleRestrictions()
        {
            if (!CanOperateOnBuilding())
                return;

            _state.RestrictionsEnabled = !_state.RestrictionsEnabled;
            UpdateRestrictionsVisibility();

            if (!_state.RestrictionsEnabled)
            {
                ClearPreview();
                _workflow.RemoveRule(_state.BuildingId, "RestrictionsToggleOff");
                _state.HasStoredRule = false;
                _state.ResetDirty();
                return;
            }

            if (!_state.HasStoredRule)
            {
                _state.BaselineRule = new ParkingRulesConfigDefinition(
                    residentsWithinRadiusOnly: true,
                    residentsRadiusMeters: DefaultNewRuleRadiusMeters,
                    workSchoolWithinRadiusOnly: true,
                    workSchoolRadiusMeters: DefaultNewRuleRadiusMeters,
                    visitorsAllowed: true);
                ApplyRuleToUi(_state.BaselineRule);
                _workflow.ApplyRuleNow(_state.BuildingId, BuildInput(), "DefaultsOnEnable");
                _state.HasStoredRule = true;
                _state.ResetDirty();
            }
            else
            {
                ApplyRuleToUi(_state.BaselineRule);
            }
            UpdatePreviewRule();
        }

        private void UpdateRestrictionsVisibility()
        {
            if (_view == null)
                return;

            _view.Visuals.SetRestrictionsContentVisible(_state.RestrictionsEnabled);
            _view.Visuals.UpdateRestrictionsToggleVisuals(_state.RestrictionsEnabled);
        }

        private bool IsPanelVisibleForStats()
        {
            return isVisible;
        }

        private void UpdateParkingSpaceStats()
        {
            if (_view == null || _workflow == null)
                return;

            int totalSpaces;
            int occupiedSpaces;
            if (!_workflow.TryGetParkingSpaceStats(_state.BuildingId, out totalSpaces, out occupiedSpaces))
            {
                _view.Visuals.UpdateParkingSpacesUnavailable();
                return;
            }

            int freeSpaces = totalSpaces - occupiedSpaces;
            if (freeSpaces < 0)
                freeSpaces = 0;

            _view.Visuals.UpdateParkingSpacesText(totalSpaces, freeSpaces);
        }

        private ParkingRulesConfigInput BuildInput()
        {
            ParkingRulesSliderRow residentsRow = _view != null ? _view.ResidentsRow : null;
            ParkingRulesSliderRow workSchoolRow = _view != null ? _view.WorkSchoolRow : null;
            ParkingRulesToggleRow visitorsRow = _view != null ? _view.VisitorsRow : null;

            float residentsValue = residentsRow != null && residentsRow.Slider != null ? residentsRow.Slider.value : 0f;
            float workValue = workSchoolRow != null && workSchoolRow.Slider != null ? workSchoolRow.Slider.value : 0f;

            bool residentsEnabled = residentsRow != null && residentsRow.IsEnabled;
            bool workEnabled = workSchoolRow != null && workSchoolRow.IsEnabled;

            float storedResidents = residentsRow != null ? residentsRow.LastNonZeroValue : 0f;
            float storedWork = workSchoolRow != null ? workSchoolRow.LastNonZeroValue : 0f;

            float residentsSliderValue = residentsEnabled ? residentsValue : GetStoredSliderValue(storedResidents);
            float workSliderValue = workEnabled ? workValue : GetStoredSliderValue(storedWork);

            return new ParkingRulesConfigInput(
                residentsEnabled,
                ConvertSliderValueToRadius(residentsSliderValue),
                workEnabled,
                ConvertSliderValueToRadius(workSliderValue),
                visitorsRow != null && visitorsRow.IsEnabled);
        }

        private float GetStoredSliderValue(float stored)
        {
            return stored > 0f ? stored : ConvertRadiusToSliderValue(ParkingRulesLimits.DefaultRadiusMeters);
        }

        private float ConvertRadiusToSliderValue(ushort radiusMeters)
        {
            if (radiusMeters == 0)
                return 0f;

            if (radiusMeters >= ParkingRulesLimits.AllRadiusMeters)
                return 1f;

            float clamped = Mathf.Clamp(radiusMeters, ParkingRulesLimits.MinRadiusMeters, ParkingRulesLimits.MaxRadiusMeters);
            if (clamped <= ParkingRulesLimits.MinRadiusMeters)
                return _uiConfig.DistanceSliderMinValue;
            if (clamped >= ParkingRulesLimits.MaxRadiusMeters)
                return _uiConfig.DistanceSliderMaxValue;

            return DistanceSliderMapping.DistanceMetersToSlider(
                clamped,
                _uiConfig.DistanceSliderMinValue,
                _uiConfig.DistanceSliderMaxValue,
                ParkingRulesLimits.MinRadiusMeters,
                ParkingRulesLimits.MidRadiusMeters,
                ParkingRulesLimits.MaxRadiusMeters);
        }

        private ushort ConvertSliderValueToRadius(float normalizedSliderValue)
        {
            if (normalizedSliderValue <= 0f)
                return 0;

            if (normalizedSliderValue >= 1f)
                return ParkingRulesLimits.AllRadiusMeters;

            if (normalizedSliderValue <= _uiConfig.DistanceSliderMinValue)
                return ParkingRulesLimits.MinRadiusMeters;
            if (normalizedSliderValue >= _uiConfig.DistanceSliderMaxValue)
                return ParkingRulesLimits.MaxRadiusMeters;

            float meters = DistanceSliderMapping.SliderToDistanceMeters(
                normalizedSliderValue,
                _uiConfig.DistanceSliderMinValue,
                _uiConfig.DistanceSliderMaxValue,
                ParkingRulesLimits.MinRadiusMeters,
                ParkingRulesLimits.MidRadiusMeters,
                ParkingRulesLimits.MaxRadiusMeters);
            int rounded = Mathf.RoundToInt(meters);
            if (rounded < ParkingRulesLimits.MinRadiusMeters) rounded = ParkingRulesLimits.MinRadiusMeters;
            if (rounded > ParkingRulesLimits.MaxRadiusMeters) rounded = ParkingRulesLimits.MaxRadiusMeters;
            return (ushort)rounded;
        }
    }
}
