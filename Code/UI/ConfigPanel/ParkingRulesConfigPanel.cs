using UnityEngine;
using ColossalFramework.UI;
using PickyParking.Features.ParkingRules;
using PickyParking.GameAdapters;
using PickyParking.ModEntry;
using PickyParking.Logging;

namespace PickyParking.UI
{
    
    
    
    
    public sealed class ParkingRulesConfigPanel : UIPanel
    {
        private const float SliderAllThreshold = 0.99f;
        private const ushort DefaultNewRuleRadiusMeters = 500;
        private const float ParkingStatsUpdateIntervalSeconds = 0.5f;

        private ushort _buildingId;
        private bool _isDirty;
        private bool _isUpdatingUi;
        private bool _hasUnappliedChanges;
        private bool _restrictionsEnabled;
        private bool _hasStoredRule;
        private ParkingRulesConfigDefinition _baselineRule;

        private PickyParkingPanelVisuals _visuals;
        private ParkingRulesSliderRow _residentsRow;
        private ParkingRulesSliderRow _workSchoolRow;
        private ParkingRulesToggleRow _visitorsRow;
        private ParkingRulesConfigEditor _editor;
        private ParkingRulesConfigUiConfig _uiConfig;
        private ParkingPanelTheme _theme;
        private GameAccess _game;
        private float _nextParkingStatsUpdateTime;
        private bool _isPrefabSupported;

        public override void Start()
        {
            base.Start();

            ModRuntime runtime = ModRuntime.Current;
            _editor = runtime != null ? runtime.ParkingRulesConfigEditor : null;
            _uiConfig = _editor != null ? _editor.UiConfig : ParkingRulesConfigUiConfig.Default;
            _theme = new ParkingPanelTheme();
            _game = runtime != null ? runtime.GameAccess : null;
            _isPrefabSupported = false;

            _visuals = new PickyParkingPanelVisuals(
                this,
                _theme,
                _uiConfig.SliderMinValue,
                _uiConfig.SliderMaxValue,
                _uiConfig.SliderStep,
                GetDefaultSliderValue,
                _uiConfig.DistanceSliderMinValue,
                _uiConfig.DistanceSliderMaxValue,
                ParkingRulesLimits.MinRadiusMeters,
                ParkingRulesLimits.MidRadiusMeters,
                ParkingRulesLimits.MaxRadiusMeters,
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

        public override void Update()
        {
            base.Update();
            if (_visuals == null || !IsPanelVisibleForStats())
                return;

            if (_buildingId == 0)
                return;

            if (Time.unscaledTime < _nextParkingStatsUpdateTime)
                return;

            _nextParkingStatsUpdateTime = Time.unscaledTime + ParkingStatsUpdateIntervalSeconds;
            UpdateParkingSpaceStats();
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

        public void SetPrefabSupported(bool supported)
        {
            _isPrefabSupported = supported;
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

            _editor.CommitPendingChanges(_buildingId, BuildInput());
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

        private void ToggleSliderRow(ParkingRulesSliderRow row)
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

        private void HandleSliderValueChanged(ParkingRulesSliderRow row, float value)
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

        private void EnableSliderRow(ParkingRulesSliderRow row)
        {
            row.IsEnabled = true;
            if (row.Slider.value <= 0f)
            {
                float restoreValue = row.LastNonZeroValue > 0f ? row.LastNonZeroValue : GetDefaultSliderValue();
                SetSliderValue(row, restoreValue);
            }
            _visuals.UpdateSliderRowVisuals(row);
        }

        private void DisableSliderRow(ParkingRulesSliderRow row)
        {
            row.IsEnabled = false;

            if (row.Slider.value > 0f)
                row.LastNonZeroValue = row.Slider.value;

            _visuals.UpdateSliderRowVisuals(row);
        }

        private void SetSliderValue(ParkingRulesSliderRow row, float value)
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
            return _buildingId != 0 && _editor != null && _isPrefabSupported;
        }

        private bool CanOperateOnBuilding(ushort buildingId)
        {
            return buildingId != 0 && _editor != null && _isPrefabSupported;
        }

        private void Refresh()
        {
            if (_editor == null)
                return;

            bool hasStoredRule = _editor.TryGetStoredRule(_buildingId, out var storedRule);

            _restrictionsEnabled = hasStoredRule;
            _hasStoredRule = hasStoredRule;

            if (hasStoredRule)
            {
                _baselineRule = storedRule;
                ApplyRuleToUi(storedRule);
            }
            else
            {
                _baselineRule = new ParkingRulesConfigDefinition(
                    residentsWithinRadiusOnly: false,
                    residentsRadiusMeters: ParkingRulesLimits.DefaultRadiusMeters,
                    workSchoolWithinRadiusOnly: false,
                    workSchoolRadiusMeters: ParkingRulesLimits.DefaultRadiusMeters,
                    visitorsAllowed: false);
            }

            _isDirty = false;
            _hasUnappliedChanges = false;
            UpdateRestrictionsVisibility();
            UpdatePreviewRule();
            UpdateParkingSpaceStats();

            if (hasStoredRule && Log.IsVerboseEnabled)
                Log.Info("[UI] Refreshed panel for building " + _buildingId + ": " + _editor.FormatRule(storedRule));
        }

        private void ApplyRuleToUi(ParkingRulesConfigDefinition rule)
        {
            ApplySliderRowFromRule(_residentsRow, rule.ResidentsWithinRadiusOnly, rule.ResidentsRadiusMeters);
            ApplySliderRowFromRule(_workSchoolRow, rule.WorkSchoolWithinRadiusOnly, rule.WorkSchoolRadiusMeters);

            _visitorsRow.IsEnabled = rule.VisitorsAllowed;
            _visuals.UpdateToggleRowVisuals(_visitorsRow);
        }

        private void ApplySliderRowFromRule(ParkingRulesSliderRow row, bool enabled, ushort radiusMeters)
        {
            if (_editor == null)
                return;

            _visuals.ApplySliderRowFromRule(
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

            if (!_restrictionsEnabled)
                return;

            _editor.UpdatePreview(_buildingId, BuildInput());
        }

        private void ApplyChangesInternal(string reason)
        {
            if (!CanOperateOnBuilding())
                return;

            _isDirty = false;
            _editor.ApplyRuleNow(_buildingId, BuildInput(), reason);
        }

        private void ApplyChangesFromButton()
        {
            if (!CanOperateOnBuilding())
                return;

            if (!_restrictionsEnabled)
                return;

            _editor.ApplyRuleNow(_buildingId, BuildInput(), "ApplyButton");
            _editor.RequestPendingReevaluationIfAny(_buildingId);
            _baselineRule = _editor.BuildRuleFromInput(BuildInput());
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
                return ParkingRulesConfigUiConfig.Default.DistanceSliderMaxValue * 0.2f;

            return ConvertRadiusToSliderValue(ParkingRulesLimits.DefaultRadiusMeters);
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
                _baselineRule = new ParkingRulesConfigDefinition(
                    residentsWithinRadiusOnly: true,
                    residentsRadiusMeters: DefaultNewRuleRadiusMeters,
                    workSchoolWithinRadiusOnly: true,
                    workSchoolRadiusMeters: DefaultNewRuleRadiusMeters,
                    visitorsAllowed: true);
                ApplyRuleToUi(_baselineRule);
            _editor.ApplyRuleNow(_buildingId, BuildInput(), "DefaultsOnEnable");
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

        private bool IsPanelVisibleForStats()
        {
            return isVisible;
        }

        private void UpdateParkingSpaceStats()
        {
            if (_visuals == null || _game == null)
                return;

            int totalSpaces;
            int occupiedSpaces;
            if (!_game.TryGetParkingSpaceStats(_buildingId, out totalSpaces, out occupiedSpaces))
            {
                _visuals.UpdateParkingSpacesUnavailable();
                return;
            }

            int freeSpaces = totalSpaces - occupiedSpaces;
            if (freeSpaces < 0)
                freeSpaces = 0;

            _visuals.UpdateParkingSpacesText(totalSpaces, freeSpaces);
        }

        private ParkingRulesConfigInput BuildInput()
        {
            float residentsValue = _residentsRow != null && _residentsRow.Slider != null ? _residentsRow.Slider.value : 0f;
            float workValue = _workSchoolRow != null && _workSchoolRow.Slider != null ? _workSchoolRow.Slider.value : 0f;

            bool residentsEnabled = _residentsRow != null && _residentsRow.IsEnabled;
            bool workEnabled = _workSchoolRow != null && _workSchoolRow.IsEnabled;

            float storedResidents = _residentsRow != null ? _residentsRow.LastNonZeroValue : 0f;
            float storedWork = _workSchoolRow != null ? _workSchoolRow.LastNonZeroValue : 0f;

            float residentsSliderValue = residentsEnabled ? residentsValue : GetStoredSliderValue(storedResidents);
            float workSliderValue = workEnabled ? workValue : GetStoredSliderValue(storedWork);

            return new ParkingRulesConfigInput(
                residentsEnabled,
                ConvertSliderValueToRadius(residentsSliderValue),
                workEnabled,
                ConvertSliderValueToRadius(workSliderValue),
                _visitorsRow != null && _visitorsRow.IsEnabled);
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
