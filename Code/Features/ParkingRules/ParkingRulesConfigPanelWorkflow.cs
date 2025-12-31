using PickyParking.GameAdapters;
using PickyParking.UI;

namespace PickyParking.Features.ParkingRules
{
    public sealed class ParkingRulesConfigPanelWorkflow
    {
        private readonly ParkingRulesConfigEditor _editor;
        private readonly GameAccess _game;

        public ParkingRulesConfigPanelWorkflow(ParkingRulesConfigEditor editor, GameAccess game)
        {
            _editor = editor;
            _game = game;
        }

        public ParkingRulesConfigUiConfig UiConfig
        {
            get { return _editor != null ? _editor.UiConfig : ParkingRulesConfigUiConfig.Default; }
        }

        public bool CanEditRules
        {
            get { return _editor != null; }
        }

        public bool TryGetStoredRule(ushort buildingId, out ParkingRulesConfigDefinition rule)
        {
            rule = default;
            if (_editor == null)
                return false;

            return _editor.TryGetStoredRule(buildingId, out rule);
        }

        public void CommitPendingChanges(ushort buildingId, ParkingRulesConfigInput input)
        {
            if (_editor == null)
                return;

            _editor.CommitPendingChanges(buildingId, input);
        }

        public void ClearPreview(ushort buildingId)
        {
            if (_editor == null)
                return;

            _editor.ClearPreview(buildingId);
        }

        public void UpdatePreview(ushort buildingId, ParkingRulesConfigInput input)
        {
            if (_editor == null)
                return;

            _editor.UpdatePreview(buildingId, input);
        }

        public void ApplyRuleNow(ushort buildingId, ParkingRulesConfigInput input, string reason)
        {
            if (_editor == null)
                return;

            _editor.ApplyRuleNow(buildingId, input, reason);
        }

        public void RemoveRule(ushort buildingId, string reason)
        {
            if (_editor == null)
                return;

            _editor.RemoveRule(buildingId, reason);
        }

        public void RequestPendingReevaluationIfAny(ushort buildingId)
        {
            if (_editor == null)
                return;

            _editor.RequestPendingReevaluationIfAny(buildingId);
        }

        public ParkingRulesConfigDefinition BuildRuleFromInput(ParkingRulesConfigInput input)
        {
            if (_editor == null)
                return default;

            return _editor.BuildRuleFromInput(input);
        }

        public string FormatRule(ParkingRulesConfigDefinition rule)
        {
            if (_editor == null)
                return string.Empty;

            return _editor.FormatRule(rule);
        }

        public bool TryGetParkingSpaceStats(ushort buildingId, out int totalSpaces, out int occupiedSpaces)
        {
            totalSpaces = 0;
            occupiedSpaces = 0;
            if (_game == null)
                return false;

            return _game.TryGetParkingSpaceStats(buildingId, out totalSpaces, out occupiedSpaces);
        }
    }
}
