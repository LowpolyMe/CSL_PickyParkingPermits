using PickyParking.Domain;

namespace PickyParking.App
{
    
    
    
    public sealed class ParkingRulePreviewState
    {
        private ushort _buildingId;
        private ParkingRestrictionsConfigDefinition _rule;
        private bool _hasPreview;

        public void SetPreview(ushort buildingId, ParkingRestrictionsConfigDefinition rule)
        {
            _buildingId = buildingId;
            _rule = rule;
            _hasPreview = true;
        }

        public bool TryGetPreview(ushort buildingId, out ParkingRestrictionsConfigDefinition rule)
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
