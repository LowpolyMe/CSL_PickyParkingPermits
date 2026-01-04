using UnityEngine;
using ColossalFramework.UI;
using PickyParking.Features.ParkingRules;
using PickyParking.GameAdapters;
using PickyParking.Logging;
using PickyParking.UI.BuildingOptionsPanel;

namespace PickyParking.UI.BuildingOptionsPanel.ParkingRulesPanel
{
    
    
    
    
    public sealed class ParkingRulesConfigPanel : UIPanel
    {
        private ParkingRulesConfigPanelState _state;
        private ParkingRulesConfigPanelUi _ui;
        private ParkingRulesConfigEditor _editor;
        private GameAccess _game;
        private ParkingRulesConfigUiConfig _uiConfig;
        private ParkingPanelTheme _theme;
        private UiServices _services;

        public void Initialize(UiServices services)
        {
            _services = services;
        }

        public override void Start()
        {
            base.Start();

            _editor = _services != null ? _services.ParkingRulesConfigEditor : null;
            _game = _services != null ? _services.GameAccess : null;
            _uiConfig = _editor != null ? _editor.UiConfig : ParkingRulesConfigUiConfig.Default;
            _theme = new ParkingPanelTheme(_services);
            _state = new ParkingRulesConfigPanelState();

            var uiArgs = new ParkingRulesConfigPanelUiArgs
            {
                Panel = this,
                Theme = _theme,
                UiConfig = _uiConfig,
                GetDefaultSliderValue = GetDefaultSliderValue,
                OnToggleRestrictions = ToggleRestrictions,
                OnToggleSlider = ToggleSliderRow,
                OnSliderValueChanged = HandleSliderValueChanged,
                OnToggleVisitors = ToggleVisitorsRow,
                OnApplyChanges = ApplyChangesFromButton
            };
            _ui = new ParkingRulesConfigPanelUi(uiArgs);
            _ui.ConfigurePanel();
            _ui.BuildUi();
        }

        public override void Update()
        {
            base.Update();
            if (_ui == null || !IsPanelVisibleForStats())
                return;

            if (!_state.IsReadyForStatsUpdate(Time.unscaledTime))
                return;

            _state.ScheduleNextStatsUpdate(Time.unscaledTime + BuildingOptionsPanelUiValues.RulesPanel.ParkingStatsUpdateIntervalSeconds);
            UpdateParkingSpaceStats();
        }

        public void Bind(ushort buildingId)
        {
            if (_state.BuildingId != 0 && _state.BuildingId != buildingId)
            {
                DiscardUnappliedChanges();
            }

            _state.BindBuilding(buildingId);
            Refresh();
        }

        public void SetPrefabSupported(bool supported)
        {
            _state.SetPrefabSupported(supported);
        }

        public void CommitPendingChanges()
        {
            if (!_state.IsDirty)
                return;

            _state.ClearDirty();

            if (!CanOperateOnBuilding())
                return;

            if (!_state.RestrictionsEnabled)
                return;

            _editor.CommitPendingChanges(_state.BuildingId, BuildInput());
        }

        public void DiscardUnappliedChangesIfAny()
        {
            DiscardUnappliedChanges();
        }

        public void ClearPreview()
        {
            if (_editor == null)
                return;

            _editor.ClearPreview(_state.BuildingId);
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

            _ui.VisitorsRow.IsEnabled = !_ui.VisitorsRow.IsEnabled;
            _ui.UpdateToggleRowVisuals(_ui.VisitorsRow);
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
                _state.BeginUiSync();
                row.Slider.value = snappedValue;
                _state.EndUiSync();
            }

            value = snappedValue;

            if (value <= 0f)
            {
                row.IsEnabled = false;
                _ui.UpdateSliderRowLabel(row);
                _ui.UpdateSliderRowVisuals(row);
            }
            else
            {
                row.IsEnabled = true;
                row.LastNonZeroValue = value;
                _ui.UpdateSliderRowLabel(row);
                _ui.UpdateSliderRowVisuals(row);
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
            _ui.UpdateSliderRowVisuals(row);
        }

        private void DisableSliderRow(ParkingRulesSliderRow row)
        {
            row.IsEnabled = false;

            if (row.Slider.value > 0f)
                row.LastNonZeroValue = row.Slider.value;

            _ui.UpdateSliderRowVisuals(row);
        }

        private void SetSliderValue(ParkingRulesSliderRow row, float value)
        {
            value = SnapSliderValue(value);
            _state.BeginUiSync();
            row.Slider.value = value;
            if (value > 0f)
                row.LastNonZeroValue = value;
            _ui.UpdateSliderRowLabel(row);
            _state.EndUiSync();
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
            if (value >= BuildingOptionsPanelUiValues.RulesPanel.SliderAllThreshold)
                return 1f;
            return value;
        }

        private void DiscardUnappliedChanges()
        {
            if (!_state.HasUnappliedChanges)
                return;

            ClearPreview();
            ApplyRuleToUi(_state.BaselineRule);
            _state.ClearDirty();
        }

        private void MarkDirty()
        {
            _state.MarkDirty();
        }

        private bool CanOperateOnBuilding()
        {
            return _state.BuildingId != 0 && _editor != null && _state.IsPrefabSupported;
        }

        private bool CanOperateOnBuilding(ushort buildingId)
        {
            return buildingId != 0 && _editor != null && _state.IsPrefabSupported;
        }

        private void Refresh()
        {
            if (_editor == null)
                return;

            bool hasStoredRule = _editor.TryGetStoredRule(_state.BuildingId, out var storedRule);

            if (hasStoredRule)
            {
                _state.SetBaselineRule(storedRule, restrictionsEnabled: true, hasStoredRule: true);
                ApplyRuleToUi(storedRule);
            }
            else
            {
                var baselineRule = new ParkingRulesConfigDefinition(
                    residentsWithinRadiusOnly: false,
                    residentsRadiusMeters: ParkingRulesLimits.DefaultRadiusMeters,
                    workSchoolWithinRadiusOnly: false,
                    workSchoolRadiusMeters: ParkingRulesLimits.DefaultRadiusMeters,
                    visitorsAllowed: false);
                _state.SetBaselineRule(baselineRule, restrictionsEnabled: false, hasStoredRule: false);
            }

            UpdateRestrictionsVisibility();
            UpdatePreviewRule();
            UpdateParkingSpaceStats();

            if (hasStoredRule && Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                Log.Info("[UI] Refreshed panel for building " + _state.BuildingId + ": " + _editor.FormatRule(storedRule));
        }

        private void ApplyRuleToUi(ParkingRulesConfigDefinition rule)
        {
            ApplySliderRowFromRule(_ui.ResidentsRow, rule.ResidentsWithinRadiusOnly, rule.ResidentsRadiusMeters);
            ApplySliderRowFromRule(_ui.WorkSchoolRow, rule.WorkSchoolWithinRadiusOnly, rule.WorkSchoolRadiusMeters);

            _ui.VisitorsRow.IsEnabled = rule.VisitorsAllowed;
            _ui.UpdateToggleRowVisuals(_ui.VisitorsRow);
        }

        private void ApplySliderRowFromRule(ParkingRulesSliderRow row, bool enabled, ushort radiusMeters)
        {
            if (_ui == null)
                return;

            _ui.ApplySliderRowFromRule(new SliderRowRuleArgs
            {
                Row = row,
                Enabled = enabled,
                RadiusMeters = radiusMeters,
                ConvertRadiusToSliderValue = ConvertRadiusToSliderValue,
                SetSliderValue = SetSliderValue
            });
        }

        private void UpdatePreviewRule()
        {
            if (!CanOperateOnBuilding())
                return;

            if (!_state.RestrictionsEnabled)
                return;

            _editor.UpdatePreview(_state.BuildingId, BuildInput());
        }

        private void ApplyChangesInternal(string reason)
        {
            if (!CanOperateOnBuilding())
                return;

            _state.BeginApplying();
            _editor.ApplyRuleNow(_state.BuildingId, BuildInput(), reason);
            _state.ClearDirty();
        }

        private void ApplyChangesFromButton()
        {
            if (!CanOperateOnBuilding())
                return;

            if (!_state.RestrictionsEnabled)
                return;

            ParkingRulesConfigInput input = BuildInput();
            _state.BeginApplying();
            _editor.ApplyRuleNow(_state.BuildingId, input, "ApplyButton");
            _editor.RequestPendingReevaluationIfAny(_state.BuildingId);
            _state.CommitApplied(_editor.BuildRuleFromInput(input));
        }

        private void RequestPendingReevaluationIfAny(ushort buildingId)
        {
            if (!CanOperateOnBuilding(buildingId))
                return;

            _editor.RequestPendingReevaluationIfAny(buildingId);
        }

        private float GetDefaultSliderValue()
        {
            return ConvertRadiusToSliderValue(ParkingRulesLimits.DefaultRadiusMeters);
        }

        private void ToggleRestrictions()
        {
            if (!CanOperateOnBuilding())
                return;

            bool enableRestrictions = !_state.RestrictionsEnabled;
            if (enableRestrictions)
                _state.EnableRestrictions();
            else
                _state.DisableRestrictions();
            UpdateRestrictionsVisibility();

            if (!enableRestrictions)
            {
                ClearPreview();
                _editor.RemoveRule(_state.BuildingId, "RestrictionsToggleOff");
                return;
            }

            if (!_state.HasStoredRule)
            {
                var baselineRule = new ParkingRulesConfigDefinition(
                    residentsWithinRadiusOnly: true,
                    residentsRadiusMeters: BuildingOptionsPanelUiValues.RulesPanel.DefaultNewRuleRadiusMeters,
                    workSchoolWithinRadiusOnly: true,
                    workSchoolRadiusMeters: BuildingOptionsPanelUiValues.RulesPanel.DefaultNewRuleRadiusMeters,
                    visitorsAllowed: true);
                ApplyRuleToUi(baselineRule);
                _editor.ApplyRuleNow(_state.BuildingId, BuildInput(), "DefaultsOnEnable");
                _state.CommitApplied(baselineRule);
            }
            else
            {
                ApplyRuleToUi(_state.BaselineRule);
            }
            UpdatePreviewRule();
        }

        private void UpdateRestrictionsVisibility()
        {
            if (_ui == null)
                return;

            _ui.SetRestrictionsContentVisible(_state.RestrictionsEnabled);
            _ui.UpdateRestrictionsToggleVisuals(_state.RestrictionsEnabled);
        }

        private bool IsPanelVisibleForStats()
        {
            return isVisible;
        }

        private void UpdateParkingSpaceStats()
        {
            if (_ui == null || _game == null)
                return;

            int totalSpaces;
            int occupiedSpaces;
            if (!_game.TryGetParkingSpaceStats(_state.BuildingId, out totalSpaces, out occupiedSpaces))
            {
                _ui.UpdateParkingSpacesUnavailable();
                return;
            }

            int freeSpaces = totalSpaces - occupiedSpaces;
            if (freeSpaces < 0)
                freeSpaces = 0;

            _ui.UpdateParkingSpacesText(totalSpaces, freeSpaces);
        }

        private ParkingRulesConfigInput BuildInput()
        {
            ParkingRulesSliderRow residentsRow = _ui != null ? _ui.ResidentsRow : null;
            ParkingRulesSliderRow workSchoolRow = _ui != null ? _ui.WorkSchoolRow : null;
            ParkingRulesToggleRow visitorsRow = _ui != null ? _ui.VisitorsRow : null;

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

            return DistanceSliderMapping.DistanceMetersToSlider(clamped, _uiConfig);
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

            float meters = DistanceSliderMapping.SliderToDistanceMeters(normalizedSliderValue, _uiConfig);
            int rounded = Mathf.RoundToInt(meters);
            if (rounded < ParkingRulesLimits.MinRadiusMeters) rounded = ParkingRulesLimits.MinRadiusMeters;
            if (rounded > ParkingRulesLimits.MaxRadiusMeters) rounded = ParkingRulesLimits.MaxRadiusMeters;
            return (ushort)rounded;
        }
    }
}








