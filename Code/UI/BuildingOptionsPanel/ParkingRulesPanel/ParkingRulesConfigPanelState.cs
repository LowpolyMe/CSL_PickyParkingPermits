using PickyParking.Features.ParkingRules;

namespace PickyParking.UI.BuildingOptionsPanel.ParkingRulesPanel
{
    internal sealed class ParkingRulesConfigPanelState
    {
        internal enum PanelMode
        {
            Disabled,
            Viewing,
            Editing,
            Applying
        }

        private ushort _buildingId;
        private bool _isDirty;
        private bool _isUpdatingUi;
        private bool _restrictionsEnabled;
        private bool _hasStoredRule;
        private ParkingRulesConfigDefinition _baselineRule;
        private bool _isPrefabSupported;
        private float _nextParkingStatsUpdateTime;
        private PanelMode _mode;

        public ushort BuildingId => _buildingId;
        public bool IsDirty => _isDirty;
        public bool IsUpdatingUi => _isUpdatingUi;
        public bool HasUnappliedChanges => _isDirty;
        public bool RestrictionsEnabled => _restrictionsEnabled;
        public bool HasStoredRule => _hasStoredRule;
        public ParkingRulesConfigDefinition BaselineRule => _baselineRule;
        public bool IsPrefabSupported => _isPrefabSupported;
        public float NextParkingStatsUpdateTime => _nextParkingStatsUpdateTime;
        public PanelMode Mode => _mode;

        public void BindBuilding(ushort buildingId)
        {
            _buildingId = buildingId;
            _isDirty = false;
            _isUpdatingUi = false;
            _restrictionsEnabled = false;
            _hasStoredRule = false;
            _baselineRule = default;
            _nextParkingStatsUpdateTime = 0f;
            _mode = buildingId == 0 ? PanelMode.Disabled : PanelMode.Viewing;
        }

        public void SetPrefabSupported(bool supported)
        {
            _isPrefabSupported = supported;
        }

        public void BeginUiSync()
        {
            _isUpdatingUi = true;
        }

        public void EndUiSync()
        {
            _isUpdatingUi = false;
        }

        public bool IsReadyForStatsUpdate(float currentTime)
        {
            return _buildingId != 0 && currentTime >= _nextParkingStatsUpdateTime;
        }

        public void ScheduleNextStatsUpdate(float nextTime)
        {
            _nextParkingStatsUpdateTime = nextTime;
        }

        public void SetBaselineRule(ParkingRulesConfigDefinition rule, bool restrictionsEnabled, bool hasStoredRule)
        {
            _baselineRule = rule;
            _restrictionsEnabled = restrictionsEnabled;
            _hasStoredRule = hasStoredRule;
            ClearDirty();
        }

        public void EnableRestrictions()
        {
            _restrictionsEnabled = true;
            if (_mode == PanelMode.Disabled)
                _mode = PanelMode.Viewing;
        }

        public void DisableRestrictions()
        {
            _restrictionsEnabled = false;
            _hasStoredRule = false;
            ClearDirty();
            _mode = PanelMode.Disabled;
        }

        public void MarkDirty()
        {
            if (!_restrictionsEnabled)
                return;

            _isDirty = true;
            _mode = PanelMode.Editing;
        }

        public void ClearDirty()
        {
            _isDirty = false;
            if (!_restrictionsEnabled)
                _mode = PanelMode.Disabled;
            else if (_mode != PanelMode.Applying)
                _mode = PanelMode.Viewing;
        }

        public void BeginApplying()
        {
            _mode = PanelMode.Applying;
            _isDirty = false;
        }

        public void CommitApplied(ParkingRulesConfigDefinition baselineRule)
        {
            _baselineRule = baselineRule;
            _hasStoredRule = true;
            _isDirty = false;
            _mode = _restrictionsEnabled ? PanelMode.Viewing : PanelMode.Disabled;
        }
    }
}






