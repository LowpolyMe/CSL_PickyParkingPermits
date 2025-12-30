using PickyParking.Features.ParkingRules;

namespace PickyParking.Features.ParkingRules
{
    public sealed class ParkingRulesConfigInput
    {
        public bool ResidentsEnabled { get; private set; }
        public ushort ResidentsRadiusMeters { get; private set; }
        public bool WorkSchoolEnabled { get; private set; }
        public ushort WorkSchoolRadiusMeters { get; private set; }
        public bool VisitorsAllowed { get; private set; }

        public ParkingRulesConfigInput(
            bool residentsEnabled,
            ushort residentsRadiusMeters,
            bool workSchoolEnabled,
            ushort workSchoolRadiusMeters,
            bool visitorsAllowed)
        {
            ResidentsEnabled = residentsEnabled;
            ResidentsRadiusMeters = residentsRadiusMeters;
            WorkSchoolEnabled = workSchoolEnabled;
            WorkSchoolRadiusMeters = workSchoolRadiusMeters;
            VisitorsAllowed = visitorsAllowed;
        }
    }
}
