using UnityEngine;
using ColossalFramework.UI;
using PickyParking.Domain;
using PickyParking.ModEntry;
using PickyParking.Infrastructure;
using PickyParking.Features.ParkingPermits;

namespace PickyParking.UI
{
    
    
    
    
    public sealed class ParkingRestrictionsConfigPanel : UIPanel
    {
        private const float SliderAllThreshold = 0.99f;
        private const ushort DefaultNewRuleRadiusMeters = 500;

        private ushort _buildingId;
        private bool _isDirty;
        private bool _isUpdatingUi;
        private bool _hasUnappliedChanges;
        private bool _restrictionsEnabled;
        private bool _hasStoredRule;
        private ParkingRestrictionsConfigDefinition _baselineRule;

        private PickyParkingPanelVisuals _visuals;
        private ParkingPermitsSliderRow _residentsRow;
        private ParkingPermitsSliderRow _workSchoolRow;
        private ParkingPermitsToggleRow _visitorsRow;
        private ParkingRestrictionsConfigEditor _editor;
        private ParkingRestrictionsConfigUiConfig _uiConfig;
        private ParkingPanelTheme _theme;

        public override void Start()
        {
            base.Start();

            ModRuntime runtime = ModRuntime.Current;
            _editor = runtime != null ? runtime.ParkingRestrictionsConfigEditor : null;
            _uiConfig = _editor != null ? _editor.UiConfig : ParkingRestrictionsConfigUiConfig.Default;
            _theme = new ParkingPanelTheme();

            _visuals = new PickyParkingPanelVisuals(
                this,
                _theme,
                _uiConfig.SliderMinValue,
                _uiConfig.SliderMaxValue,
                _uiConfig.SliderStep,
                GetDefaultSliderValue,
                _uiConfig.DistanceSliderMinValue,
                _uiConfig.DistanceSliderMaxValue,
                _uiConfig.MinDistanceMeters,
                _uiConfig.MidDistanceMeters,
                _uiConfig.MaxDistanceMeters,
                _uiConfig.DistanceMidpointT,
                ToggleRestrictions,
                ToggleSliderRow,
                HandleSliderValueChanged,
                ToggleVisitorsRow,
                ApplyChangesFromButton);

            _visuals.ConfigurePanel();
            _visuals.BuildUi();
            _residentsRow = _visuals.ResidentsRow;
            _workSchoolRow = _visuals.WorkSchoolRow;
            _visitorsRow = _visuals.VisitorsRow;
        }

        public void Bind(ushort buildingId)
        {
            if (_buildingId != 0 && _buildingId != buildingId)
            {
                DiscardUnappliedChanges();
            }

            _buildingId = buildingId;
            Refresh();
        }

        public void CommitPendingChanges()
        {
            if (!_isDirty)
                return;

            _isDirty = false;

            if (!CanOperateOnBuilding())
                return;

            if (!_restrictionsEnabled)
                return;

            _editor.CommitPendingChanges(_buildingId, BuildUiState());
        }

        public void DiscardUnappliedChangesIfAny()
        {
            DiscardUnappliedChanges();
        }

        public void ClearPreview()
        {
            if (_editor == null)
                return;

            _editor.ClearPreview(_buildingId);
        }

        public void RequestPendingReevaluationIfAny()
        {
            RequestPendingReevaluationIfAny(_buildingId);
        }

        private void ToggleSliderRow(ParkingPermitsSliderRow row)
        {
            if (!_restrictionsEnabled)
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
            if (!_restrictionsEnabled)
                return;

            _visitorsRow.IsEnabled = !_visitorsRow.IsEnabled;
            _visuals.UpdateToggleRowVisuals(_visitorsRow);
            MarkDirty();
            UpdatePreviewRule();
        }

        private void HandleSliderValueChanged(ParkingPermitsSliderRow row, float value)
        {
            if (!_restrictionsEnabled)
                return;

            if (_isUpdatingUi)
                return;

            float snappedValue = SnapSliderValue(value);
            if (!Mathf.Approximately(snappedValue, value))
            {
                _isUpdatingUi = true;
                row.Slider.value = snappedValue;
                _isUpdatingUi = false;
            }

            value = snappedValue;

            if (value <= 0f)
            {
                row.IsEnabled = false;
                _visuals.UpdateSliderRowLabel(row);
                _visuals.UpdateSliderRowVisuals(row);
            }
            else
            {
                row.IsEnabled = true;
                row.LastNonZeroValue = value;
                _visuals.UpdateSliderRowLabel(row);
                _visuals.UpdateSliderRowVisuals(row);
            }

            MarkDirty();
            UpdatePreviewRule();
        }

        private void EnableSliderRow(ParkingPermitsSliderRow row)
        {
            row.IsEnabled = true;
            if (row.Slider.value <= 0f)
            {
                float restoreValue = row.LastNonZeroValue > 0f ? row.LastNonZeroValue : GetDefaultSliderValue();
                SetSliderValue(row, restoreValue);
            }
            _visuals.UpdateSliderRowVisuals(row);
        }

        private void DisableSliderRow(ParkingPermitsSliderRow row)
        {
            row.IsEnabled = false;

            if (row.Slider.value > 0f)
                row.LastNonZeroValue = row.Slider.value;

            _visuals.UpdateSliderRowVisuals(row);
        }

        private void SetSliderValue(ParkingPermitsSliderRow row, float value)
        {
            value = SnapSliderValue(value);
            _isUpdatingUi = true;
            row.Slider.value = value;
            if (value > 0f)
                row.LastNonZeroValue = value;
            _visuals.UpdateSliderRowLabel(row);
            _isUpdatingUi = false;
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
            if (!_hasUnappliedChanges)
                return;

            ClearPreview();
            ApplyRuleToUi(_baselineRule);
            _isDirty = false;
            _hasUnappliedChanges = false;
        }

        private void MarkDirty()
        {
            _isDirty = true;
            _hasUnappliedChanges = true;
        }

        private bool CanOperateOnBuilding()
        {
            return _buildingId != 0 && _editor != null;
        }

        private bool CanOperateOnBuilding(ushort buildingId)
        {
            return buildingId != 0 && _editor != null;
        }

        private void Refresh()
        {
            if (_editor == null)
                return;

            ParkingRestrictionsConfigDefinition rule = _editor.GetRuleForBuilding(_buildingId);
            bool hasStoredRule = _editor.TryGetStoredRule(_buildingId, out var storedRule);
            if (hasStoredRule)
                rule = storedRule;

            _baselineRule = rule;
            _restrictionsEnabled = hasStoredRule;
            _hasStoredRule = hasStoredRule;
            ApplyRuleToUi(rule);
            _isDirty = false;
            _hasUnappliedChanges = false;
            UpdateRestrictionsVisibility();
            UpdatePreviewRule();

            if (Log.IsVerboseEnabled)
                Log.Info("[UI] Refreshed panel for building " + _buildingId + ": " + _editor.FormatRule(rule));
        }

        private void ApplyRuleToUi(ParkingRestrictionsConfigDefinition rule)
        {
            ApplySliderRowFromRule(_residentsRow, rule.ResidentsWithinRadiusOnly, rule.ResidentsRadiusMeters);
            ApplySliderRowFromRule(_workSchoolRow, rule.WorkSchoolWithinRadiusOnly, rule.WorkSchoolRadiusMeters);

            _visitorsRow.IsEnabled = rule.VisitorsAllowed;
            _visuals.UpdateToggleRowVisuals(_visitorsRow);
        }

        private void ApplySliderRowFromRule(ParkingPermitsSliderRow row, bool enabled, ushort radiusMeters)
        {
            if (_editor == null)
                return;

            _visuals.ApplySliderRowFromRule(
                row,
                enabled,
                radiusMeters,
                _editor.ConvertRadiusToSliderValue,
                SetSliderValue);
        }

        private void UpdatePreviewRule()
        {
            if (!CanOperateOnBuilding())
                return;

            if (!_restrictionsEnabled)
                return;

            _editor.UpdatePreview(_buildingId, BuildUiState());
        }

        private void ApplyChangesInternal(string reason)
        {
            if (!CanOperateOnBuilding())
                return;

            _isDirty = false;
            _editor.ApplyRuleNow(_buildingId, BuildUiState(), reason);
        }

        private void ApplyChangesFromButton()
        {
            if (!CanOperateOnBuilding())
                return;

            if (!_restrictionsEnabled)
                return;

            _editor.ApplyRuleNow(_buildingId, BuildUiState(), "ApplyButton");
            _baselineRule = _editor.BuildRuleFromUi(BuildUiState());
            _hasStoredRule = true;
            _isDirty = false;
            _hasUnappliedChanges = false;
        }

        private void RequestPendingReevaluationIfAny(ushort buildingId)
        {
            if (!CanOperateOnBuilding(buildingId))
                return;

            _editor.RequestPendingReevaluationIfAny(buildingId);
        }

        private float GetDefaultSliderValue()
        {
            if (_editor == null)
                return ParkingRestrictionsConfigUiConfig.Default.DistanceSliderMaxValue * 0.2f;

            return _editor.GetDefaultSliderValue();
        }

        private void ToggleRestrictions()
        {
            if (!CanOperateOnBuilding())
                return;

            _restrictionsEnabled = !_restrictionsEnabled;
            UpdateRestrictionsVisibility();

            if (!_restrictionsEnabled)
            {
                ClearPreview();
                _editor.RemoveRule(_buildingId, "RestrictionsToggleOff");
                _hasStoredRule = false;
                _isDirty = false;
                _hasUnappliedChanges = false;
                return;
            }

            if (!_hasStoredRule)
            {
                _baselineRule = new ParkingRestrictionsConfigDefinition(
                    residentsWithinRadiusOnly: true,
                    residentsRadiusMeters: DefaultNewRuleRadiusMeters,
                    workSchoolWithinRadiusOnly: true,
                    workSchoolRadiusMeters: DefaultNewRuleRadiusMeters,
                    visitorsAllowed: true);
                ApplyRuleToUi(_baselineRule);
                _editor.ApplyRuleNow(_buildingId, BuildUiState(), "DefaultsOnEnable");
                _hasStoredRule = true;
                _isDirty = false;
                _hasUnappliedChanges = false;
            }
            else
            {
                ApplyRuleToUi(_baselineRule);
            }
            UpdatePreviewRule();
        }

        private void UpdateRestrictionsVisibility()
        {
            if (_visuals == null)
                return;

            _visuals.SetRestrictionsContentVisible(_restrictionsEnabled);
            _visuals.UpdateRestrictionsToggleVisuals(_restrictionsEnabled);
        }

        private ParkingRestrictionsConfigUiState BuildUiState()
        {
            float residentsValue = _residentsRow != null && _residentsRow.Slider != null ? _residentsRow.Slider.value : 0f;
            float workValue = _workSchoolRow != null && _workSchoolRow.Slider != null ? _workSchoolRow.Slider.value : 0f;

            return new ParkingRestrictionsConfigUiState(
                _residentsRow != null && _residentsRow.IsEnabled,
                residentsValue,
                _residentsRow != null ? _residentsRow.LastNonZeroValue : 0f,
                _workSchoolRow != null && _workSchoolRow.IsEnabled,
                workValue,
                _workSchoolRow != null ? _workSchoolRow.LastNonZeroValue : 0f,
                _visitorsRow != null && _visitorsRow.IsEnabled);
        }
    }
}
