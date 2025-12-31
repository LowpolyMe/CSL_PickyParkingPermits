using PickyParking.Features.ParkingRules;

namespace PickyParking.Features.ParkingRules
{
    public static class ParkingRulesLimits
    {
        public const ushort MinRadiusMeters = 25;
        public const ushort MidRadiusMeters = 500;
        public const ushort MaxRadiusMeters = 17000;
        public const ushort DefaultRadiusMeters = 500;
        public const ushort AllRadiusMeters = ushort.MaxValue;

        public static ParkingRulesConfigDefinition ClampRule(ParkingRulesConfigDefinition rule, out bool normalized)
        {
            ushort resRadius = ClampRadius(rule.ResidentsRadiusMeters);
            ushort workRadius = ClampRadius(rule.WorkSchoolRadiusMeters);

            normalized = resRadius != rule.ResidentsRadiusMeters
                         || workRadius != rule.WorkSchoolRadiusMeters;

            return new ParkingRulesConfigDefinition(
                rule.ResidentsWithinRadiusOnly,
                resRadius,
                rule.WorkSchoolWithinRadiusOnly,
                workRadius,
                rule.VisitorsAllowed);
        }

        private static ushort ClampRadius(ushort value)
        {
            if (value == 0)
                return 0;

            if (value == AllRadiusMeters)
                return value;

            if (value < MinRadiusMeters)
                return MinRadiusMeters;

            if (value > MaxRadiusMeters)
                return MaxRadiusMeters;

            return value;
        }
    }
}
