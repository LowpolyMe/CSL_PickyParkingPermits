using PickyParking.Features.ParkingRules;

namespace PickyParking.UI.BuildingOptionsPanel.ParkingRulesPanel
{
    internal sealed class ParkingRulesConfigPanelState
    {
        public ushort BuildingId;
        public bool IsDirty;
        public bool IsUpdatingUi;
        public bool HasUnappliedChanges;
        public bool RestrictionsEnabled;
        public bool HasStoredRule;
        public ParkingRulesConfigDefinition BaselineRule;
        public bool IsPrefabSupported;
        public float NextParkingStatsUpdateTime;

        public void MarkDirty()
        {
            IsDirty = true;
            HasUnappliedChanges = true;
        }

        public void ResetDirty()
        {
            IsDirty = false;
            HasUnappliedChanges = false;
        }
    }
}






