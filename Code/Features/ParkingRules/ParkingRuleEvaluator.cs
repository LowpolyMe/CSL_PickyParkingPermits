namespace PickyParking.Features.ParkingRules
{
    public sealed class ParkingRuleEvaluator
    {
        public struct Result
        {
            public bool Allowed;
            public DecisionReason Reason;

            public Result(bool allowed, DecisionReason reason)
            {
                Allowed = allowed;
                Reason = reason;
            }
        }

        public Result Evaluate(
            ParkingRulesConfigDefinition rule,
            bool isVisitor,
            ParkingPosition parkingLotPosition,
            ParkingPosition? homePosition,
            ParkingPosition? workPosition)
        {  
            if (isVisitor)
            {
                if (rule.VisitorsAllowed)
                    return new Result(true, DecisionReason.Allowed_MatchedVisitor);

                return new Result(false, DecisionReason.Denied_VisitorsNotAllowed);
            }

            if (rule.ResidentsWithinRadiusOnly && homePosition.HasValue)
            {
                if (rule.ResidentsRadiusMeters == ushort.MaxValue)
                    return new Result(true, DecisionReason.Allowed_MatchedResidentRadius);

                float r2 = rule.ResidentsRadiusMeters * rule.ResidentsRadiusMeters;
                if (ParkingPosition.SqrDistance(homePosition.Value, parkingLotPosition) <= r2)
                    return new Result(true, DecisionReason.Allowed_MatchedResidentRadius);
            }

            if (rule.WorkSchoolWithinRadiusOnly)
            {
                if (rule.WorkSchoolRadiusMeters == ushort.MaxValue)
                    return new Result(true, DecisionReason.Allowed_MatchedWorkSchoolRadius);

                float r2 = rule.WorkSchoolRadiusMeters * rule.WorkSchoolRadiusMeters;

                if (workPosition.HasValue && ParkingPosition.SqrDistance(workPosition.Value, parkingLotPosition) <= r2)
                    return new Result(true, DecisionReason.Allowed_MatchedWorkSchoolRadius);
            }

            return new Result(false, DecisionReason.Denied_NoMatch);
        }
    }
}




