namespace PickyParking.UI
{
    public sealed class ParkingRestrictionsConfigUiState
    {
        public bool ResidentsEnabled { get; private set; }
        public float ResidentsSliderValue { get; private set; }
        public float ResidentsStoredValue { get; private set; }
        public bool WorkSchoolEnabled { get; private set; }
        public float WorkSchoolSliderValue { get; private set; }
        public float WorkSchoolStoredValue { get; private set; }
        public bool VisitorsAllowed { get; private set; }

        public ParkingRestrictionsConfigUiState(
            bool residentsEnabled,
            float residentsSliderValue,
            float residentsStoredValue,
            bool workSchoolEnabled,
            float workSchoolSliderValue,
            float workSchoolStoredValue,
            bool visitorsAllowed)
        {
            ResidentsEnabled = residentsEnabled;
            ResidentsSliderValue = residentsSliderValue;
            ResidentsStoredValue = residentsStoredValue;
            WorkSchoolEnabled = workSchoolEnabled;
            WorkSchoolSliderValue = workSchoolSliderValue;
            WorkSchoolStoredValue = workSchoolStoredValue;
            VisitorsAllowed = visitorsAllowed;
        }
    }
}
