namespace PickyParking.Features.ParkingRules
{
    public sealed class ParkingRulePreviewState
    {
        private ushort _buildingId;
        private ParkingRulesConfigDefinition _rule;
        private bool _hasPreview;

        public void SetPreview(ushort buildingId, ParkingRulesConfigDefinition rule)
        {
            _buildingId = buildingId;
            _rule = rule;
            _hasPreview = true;
        }

        public bool TryGetPreview(ushort buildingId, out ParkingRulesConfigDefinition rule)
        {
            if (_hasPreview && _buildingId == buildingId)
            {
                rule = _rule;
                return true;
            }

            rule = default;
            return false;
        }

        public void Clear(ushort buildingId)
        {
            if (_hasPreview && _buildingId == buildingId)
                _hasPreview = false;
        }
    }
}
