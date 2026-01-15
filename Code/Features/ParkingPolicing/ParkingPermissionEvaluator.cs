using UnityEngine;
using PickyParking.ModLifecycle;
using PickyParking.GameAdapters;
using PickyParking.Features.ParkingRules;
using PickyParking.Features.Debug;
using PickyParking.Settings;

namespace PickyParking.Features.ParkingPolicing
{
    public sealed class ParkingPermissionEvaluator
    {
        private readonly FeatureGate _isFeatureActive;
        private readonly ParkingRulesConfigRegistry _rules;
        private readonly GameAccess _game;
        private readonly ParkingRuleEvaluator _ruleEvaluator;

        public ParkingPermissionEvaluator(
            FeatureGate featureGate,
            ParkingRulesConfigRegistry rules,
            GameAccess game,
            ParkingRuleEvaluator ruleEvaluator)
        {
            _isFeatureActive = featureGate;
            _rules = rules;
            _game = game;
            _ruleEvaluator = ruleEvaluator;
        }

        public struct Result
        {
            public readonly bool Allowed;
            public readonly DecisionReason Reason;

            public Result(bool allowed, DecisionReason reason)
            {
                Allowed = allowed;
                Reason = reason;
            }
        }

        public Result Evaluate(ushort vehicleId, ushort candidateBuildingId)
        {
            ParkingStatsCounter.IncrementEvaluateVehicle();

            if (!_isFeatureActive.IsActive)
                return new Result(true, DecisionReason.Allowed_FailOpen_NotActive);

            if (!_game.IsPrivatePassengerCar(vehicleId))
                return new Result(true, DecisionReason.Allowed_FailOpen_NotPassengerCar);

            if (!TryGetRuleAndLotPosition(candidateBuildingId, out ParkingRulesConfigDefinition rule, out Vector3 lotPos, out DecisionReason failOpenReason))
                return new Result(true, failOpenReason);

            if (!_game.TryGetDriverInfo(vehicleId, out var driverContext))
            {
                if (PickyParking.Logging.Log.IsVerboseEnabled && PickyParking.Logging.Log.IsDecisionDebugEnabled)
                    PickyParking.Logging.Log.Info(DebugLogCategory.DecisionPipeline, $"[Parking] Evaluate denied: no driver context vehicleId={vehicleId} buildingId={candidateBuildingId}");
                ParkingStatsCounter.IncrementDeniedNoDriverContext();
                return new Result(false, DecisionReason.Denied_NoDriverContext);
            }

            Vector3? homePos;
            Vector3? workPos;
            ResolveHomeAndWorkPositions(driverContext.HomeBuildingId, driverContext.WorkBuildingId, out homePos, out workPos);
            return EvaluateWithSearchContext(rule, driverContext.IsVisitor, lotPos, homePos, workPos);
        }

        public Result EvaluateCitizen(uint citizenId, ushort candidateBuildingId)
        {
            ParkingStatsCounter.IncrementEvaluateCitizen();

            if (!_isFeatureActive.IsActive)
                return new Result(true, DecisionReason.Allowed_FailOpen_NotActive);

            if (!TryGetRuleAndLotPosition(candidateBuildingId, out ParkingRulesConfigDefinition rule, out Vector3 lotPos, out DecisionReason failOpenReason))
                return new Result(true, failOpenReason);

            if (!_game.TryGetCitizenInfo(citizenId, out var citizenContext))
            {
                if (PickyParking.Logging.Log.IsVerboseEnabled && PickyParking.Logging.Log.IsDecisionDebugEnabled)
                    PickyParking.Logging.Log.Info(DebugLogCategory.DecisionPipeline, $"[Parking] EvaluateCitizen denied: no citizen context citizenId={citizenId} buildingId={candidateBuildingId}");
                ParkingStatsCounter.IncrementDeniedNoCitizenContext();
                return new Result(false, DecisionReason.Denied_NoCitizenContext);
            }

            Vector3? homePos;
            Vector3? workPos;
            ResolveHomeAndWorkPositions(citizenContext.HomeBuildingId, citizenContext.WorkBuildingId, out homePos, out workPos);
            return EvaluateWithSearchContext(rule, citizenContext.IsVisitor, lotPos, homePos, workPos);
        }

        private bool TryGetRuleAndLotPosition(
            ushort candidateBuildingId,
            out ParkingRulesConfigDefinition rule,
            out Vector3 lotPos,
            out DecisionReason failOpenReason)
        {
            rule = default;
            lotPos = default;

            if (!_rules.TryGet(candidateBuildingId, out rule))
            {
                failOpenReason = DecisionReason.Allowed_FailOpen_NoRuleConfigured;
                if (PickyParking.Logging.Log.IsVerboseEnabled && PickyParking.Logging.Log.IsDecisionDebugEnabled)
                    PickyParking.Logging.Log.Info(DebugLogCategory.DecisionPipeline, $"[Parking] Fail-open: no rule configured buildingId={candidateBuildingId}");
                return false;
            }

            if (!_game.TryGetBuildingPosition(candidateBuildingId, out lotPos))
            {
                failOpenReason = DecisionReason.Allowed_FailOpen_TryGetBuildingPosition;
                if (PickyParking.Logging.Log.IsVerboseEnabled && PickyParking.Logging.Log.IsDecisionDebugEnabled)
                    PickyParking.Logging.Log.Info(DebugLogCategory.DecisionPipeline, $"[Parking] Fail-open: TryGetBuildingPosition failed buildingId={candidateBuildingId}");
                return false;
            }

            failOpenReason = DecisionReason.Allowed_Unrestricted;
            return true;
        }

        private void ResolveHomeAndWorkPositions(
            ushort homeBuildingId,
            ushort workBuildingId,
            out Vector3? homePos,
            out Vector3? workPos)
        {
            homePos = _game.TryGetBuildingPosition(homeBuildingId, out var hp) ? hp : (Vector3?)null;
            workPos = _game.TryGetBuildingPosition(workBuildingId, out var wp) ? wp : (Vector3?)null;
        }

        private Result EvaluateWithSearchContext(
            ParkingRulesConfigDefinition rule,
            bool isVisitorFromCitizen,
            Vector3 lotPos,
            Vector3? homePos,
            Vector3? workPos)
        {
            ParkingSearchContext.SetEpisodeVisitorFlag(isVisitorFromCitizen);
            ParkingRuleEvaluator.Result r = _ruleEvaluator.Evaluate(
                rule,
                isVisitorFromCitizen,
                ToParkingPosition(lotPos),
                ToParkingPosition(homePos),
                ToParkingPosition(workPos));
            return new Result(r.Allowed, r.Reason);
        }

        private static ParkingPosition ToParkingPosition(Vector3 pos)
        {
            return new ParkingPosition(pos.x, pos.z);
        }

        private static ParkingPosition? ToParkingPosition(Vector3? pos)
        {
            if (!pos.HasValue)
                return null;
            Vector3 v = pos.Value;
            return new ParkingPosition(v.x, v.z);
        }
    }
}

