using PickyParking.Features.ParkingRules;
using PickyParking.Logging;
using PickyParking.Settings;

namespace PickyParking.Features.ParkingPolicing
{
    public sealed class ParkingCandidateDecisionPipeline
    {
        private readonly ParkingPermissionEvaluator _evaluator;

        public ParkingCandidateDecisionPipeline(ParkingPermissionEvaluator evaluator)
        {
            _evaluator = evaluator;
        }

        public bool TryDenyCandidateBuilding(ushort buildingId, out bool denied, out DecisionReason reason)
        {
            denied = false;
            reason = DecisionReason.Allowed_Unrestricted;

            if (_evaluator == null)
                return false;

            if (ParkingSearchContext.HasCitizenId)
            {
                ParkingPermissionEvaluator.Result result = _evaluator.EvaluateCitizen(ParkingSearchContext.CitizenId, buildingId);
                reason = result.Reason;
                denied = !result.Allowed;
                return true;
            }

            if (ParkingSearchContext.HasVehicleId)
            {
                ParkingPermissionEvaluator.Result result = _evaluator.Evaluate(ParkingSearchContext.VehicleId, buildingId);
                reason = result.Reason;
                denied = !result.Allowed;
                return true;
            }

            if (Log.IsVerboseEnabled && Log.IsDecisionDebugEnabled)
                Log.Info(DebugLogCategory.DecisionPipeline, "[Decision] No context candidateBuildingId=" + buildingId);

            denied = false;
            reason = DecisionReason.Allowed_Unrestricted;
            return true;
        }
    }
}
