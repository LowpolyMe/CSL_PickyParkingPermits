
namespace PickyParking.Features.ParkingRules
{
    public enum DecisionReason
    {
        Allowed_Unrestricted = 0,
        Allowed_MatchedResidentRadius = 1,
        Allowed_MatchedWorkSchoolRadius = 2,
        Allowed_MatchedVisitor = 3,

        Allowed_FailOpen_NotActive = 10,
        Allowed_FailOpen_NotPassengerCar = 11,
        Allowed_FailOpen_NoRuleConfigured = 12,
        Allowed_FailOpen_TryGetBuildingPosition = 13,

        Denied_NotActive = 100,
        Denied_NotPassengerCar = 101,
        Denied_VisitorsNotAllowed = 103,
        Denied_NoMatch = 104,
        Denied_NoDriverContext = 105,
        Denied_NoCitizenContext = 106,
    }
}



