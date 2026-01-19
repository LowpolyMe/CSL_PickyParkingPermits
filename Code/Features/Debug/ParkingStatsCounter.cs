using System;
using System.Text;
using System.Threading;
using PickyParking.Logging;

namespace PickyParking.Features.Debug
{
    public static class ParkingStatsCounter
    {
        private static long _contextPushTotal;
        private static long _contextPopTotal;
        private static long _contextVehicleOnly;
        private static long _contextCitizenOnly;
        private static long _contextVehicleAndCitizen;
        private static long _contextNoIds;

        private static long _tmpeFindParkingForCitizen;
        private static long _tmpeParkPassengerCar;
        private static long _tmpeTryMoveParkedVehicle;
        private static long _tmpeTrySpawnParkedPassengerCar;

        private static long _candidateChecks;
        private static long _candidateDenied;
        private static long _candidateAllowed;
        private static long _propSearchNoContextDenied;
        private static long _propSearchDenied;

        private static long _evalVehicleCalls;
        private static long _evalCitizenCalls;
        private static long _denyNoDriverContext;
        private static long _denyNoCitizenContext;

        private static long _createBlocked;
        private static long _createBypassTmpe;
        private static long _createCheckNoContext;
        private static long _createCheckNoOwner;
        private static long _createCheckNoRuleBuilding;

        private static long _vanillaFallbackFlipped;
        private static long _invisiblesFixed;
        private static long _reevalDeniedQueued;
        private static long _reevalMoved;
        private static long _reevalReleased;

        public static bool ShouldLog =>
            Log.IsVerboseEnabled;

        private static bool ShouldCount => Log.IsVerboseEnabled;

        public static void ResetAll()
        {
            Interlocked.Exchange(ref _contextPushTotal, 0);
            Interlocked.Exchange(ref _contextPopTotal, 0);
            Interlocked.Exchange(ref _contextVehicleOnly, 0);
            Interlocked.Exchange(ref _contextCitizenOnly, 0);
            Interlocked.Exchange(ref _contextVehicleAndCitizen, 0);
            Interlocked.Exchange(ref _contextNoIds, 0);
            Interlocked.Exchange(ref _tmpeFindParkingForCitizen, 0);
            Interlocked.Exchange(ref _tmpeParkPassengerCar, 0);
            Interlocked.Exchange(ref _tmpeTryMoveParkedVehicle, 0);
            Interlocked.Exchange(ref _tmpeTrySpawnParkedPassengerCar, 0);
            Interlocked.Exchange(ref _candidateChecks, 0);
            Interlocked.Exchange(ref _candidateDenied, 0);
            Interlocked.Exchange(ref _candidateAllowed, 0);
            Interlocked.Exchange(ref _propSearchNoContextDenied, 0);
            Interlocked.Exchange(ref _propSearchDenied, 0);
            Interlocked.Exchange(ref _evalVehicleCalls, 0);
            Interlocked.Exchange(ref _evalCitizenCalls, 0);
            Interlocked.Exchange(ref _denyNoDriverContext, 0);
            Interlocked.Exchange(ref _denyNoCitizenContext, 0);
            Interlocked.Exchange(ref _createBlocked, 0);
            Interlocked.Exchange(ref _createBypassTmpe, 0);
            Interlocked.Exchange(ref _createCheckNoContext, 0);
            Interlocked.Exchange(ref _createCheckNoOwner, 0);
            Interlocked.Exchange(ref _createCheckNoRuleBuilding, 0);
            Interlocked.Exchange(ref _vanillaFallbackFlipped, 0);
            Interlocked.Exchange(ref _invisiblesFixed, 0);
            Interlocked.Exchange(ref _reevalDeniedQueued, 0);
            Interlocked.Exchange(ref _reevalMoved, 0);
            Interlocked.Exchange(ref _reevalReleased, 0);
        }

        public static void IncrementContextPush(ushort vehicleId, uint citizenId, string source)
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _contextPushTotal);

            if (vehicleId != 0 && citizenId != 0)
                Interlocked.Increment(ref _contextVehicleAndCitizen);
            else if (vehicleId != 0)
                Interlocked.Increment(ref _contextVehicleOnly);
            else if (citizenId != 0)
                Interlocked.Increment(ref _contextCitizenOnly);
            else
                Interlocked.Increment(ref _contextNoIds);

            if (string.IsNullOrEmpty(source))
                return;

            if (source.StartsWith("TMPE.", StringComparison.Ordinal))
            {
                switch (source)
                {
                    case "TMPE.FindParkingSpaceForCitizen":
                        Interlocked.Increment(ref _tmpeFindParkingForCitizen);
                        break;
                    case "TMPE.ParkPassengerCar":
                        Interlocked.Increment(ref _tmpeParkPassengerCar);
                        break;
                    case "TMPE.TryMoveParkedVehicle":
                        Interlocked.Increment(ref _tmpeTryMoveParkedVehicle);
                        break;
                    case "TMPE.TrySpawnParkedPassengerCar":
                        Interlocked.Increment(ref _tmpeTrySpawnParkedPassengerCar);
                        break;
                }
            }
        }

        public static void IncrementContextPop()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _contextPopTotal);
        }

        public static void IncrementCandidateDecision(bool denied)
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _candidateChecks);
            if (denied)
                Interlocked.Increment(ref _candidateDenied);
            else
                Interlocked.Increment(ref _candidateAllowed);
        }

        public static void IncrementPropSearchNoContextDenied()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _propSearchNoContextDenied);
        }

        public static void IncrementPropSearchDenied()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _propSearchDenied);
        }

        public static void IncrementEvaluateVehicle()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _evalVehicleCalls);
        }

        public static void IncrementEvaluateCitizen()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _evalCitizenCalls);
        }

        public static void IncrementDeniedNoDriverContext()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _denyNoDriverContext);
        }

        public static void IncrementDeniedNoCitizenContext()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _denyNoCitizenContext);
        }

        public static void IncrementCreateBlocked()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _createBlocked);
        }

        public static void IncrementCreateBypassTmpe()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _createBypassTmpe);
        }

        public static void IncrementCreateCheckNoContext()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _createCheckNoContext);
        }

        public static void IncrementCreateCheckNoOwner()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _createCheckNoOwner);
        }

        public static void IncrementCreateCheckNoRuleBuilding()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _createCheckNoRuleBuilding);
        }

        public static void IncrementVanillaFallbackFlipped()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _vanillaFallbackFlipped);
        }

        public static void IncrementInvisiblesFixed()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _invisiblesFixed);
        }

        public static void IncrementReevalDeniedQueued()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _reevalDeniedQueued);
        }

        public static void IncrementReevalMoved()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _reevalMoved);
        }

        public static void IncrementReevalReleased()
        {
            if (!ShouldCount)
                return;

            Interlocked.Increment(ref _reevalReleased);
        }

        public static void LogAndReset(float windowSeconds)
        {
            var snapshot = SnapshotAndReset();
            var sb = new StringBuilder(256);

            sb.Append("[Daily Statistics]");
            //TODO add in-game date DD/MM/YY
            sb.Append(" ctxPush=");
            sb.Append(snapshot.ContextPushTotal);
            sb.Append(" ctxPop=");
            sb.Append(snapshot.ContextPopTotal);
            sb.Append(" ctxVehicleOnly=");
            sb.Append(snapshot.ContextVehicleOnly);
            sb.Append(" ctxCitizenOnly=");
            sb.Append(snapshot.ContextCitizenOnly);
            sb.Append(" ctxBoth=");
            sb.Append(snapshot.ContextVehicleAndCitizen);
            sb.Append(" ctxNoIds=");
            sb.Append(snapshot.ContextNoIds);

            sb.Append(" tmpeFind=");
            sb.Append(snapshot.TmpeFindParkingForCitizen);
            sb.Append(" tmpePark=");
            sb.Append(snapshot.TmpeParkPassengerCar);
            sb.Append(" tmpeMove=");
            sb.Append(snapshot.TmpeTryMoveParkedVehicle);
            sb.Append(" tmpeSpawn=");
            sb.Append(snapshot.TmpeTrySpawnParkedPassengerCar);

            sb.Append(" candChecks=");
            sb.Append(snapshot.CandidateChecks);
            sb.Append(" candDenied=");
            sb.Append(snapshot.CandidateDenied);
            sb.Append(" candAllowed=");
            sb.Append(snapshot.CandidateAllowed);
            sb.Append(" propNoCtxDenied=");
            sb.Append(snapshot.PropSearchNoContextDenied);
            sb.Append(" propDenied=");
            sb.Append(snapshot.PropSearchDenied);

            sb.Append(" evalCitizen=");
            sb.Append(snapshot.EvalCitizenCalls);
            sb.Append(" evalVehicle=");
            sb.Append(snapshot.EvalVehicleCalls);
            sb.Append(" denyNoCitizenCtx=");
            sb.Append(snapshot.DenyNoCitizenContext);
            sb.Append(" denyNoDriverCtx=");
            sb.Append(snapshot.DenyNoDriverContext);

            sb.Append(" createBlocked=");
            sb.Append(snapshot.CreateBlocked);
            sb.Append(" createBypassTmpe=");
            sb.Append(snapshot.CreateBypassTmpe);
            sb.Append(" createNoCtx=");
            sb.Append(snapshot.CreateCheckNoContext);
            sb.Append(" createNoOwner=");
            sb.Append(snapshot.CreateCheckNoOwner);
            sb.Append(" createNoRule=");
            sb.Append(snapshot.CreateCheckNoRuleBuilding);
            sb.Append(" vanillaFlip=");
            sb.Append(snapshot.VanillaFallbackFlipped);
            sb.Append(" invisiblesFixed=");
            sb.Append(snapshot.InvisiblesFixed);
            sb.Append(" reevalQueued=");
            sb.Append(snapshot.ReevalDeniedQueued);
            sb.Append(" reevalMoved=");
            sb.Append(snapshot.ReevalMoved);
            sb.Append(" reevalReleased=");
            sb.Append(snapshot.ReevalReleased);

            Log.Info(DebugLogCategory.None, sb.ToString());
        }

        private static Snapshot SnapshotAndReset()
        {
            return new Snapshot
            {
                ContextPushTotal = Interlocked.Exchange(ref _contextPushTotal, 0),
                ContextPopTotal = Interlocked.Exchange(ref _contextPopTotal, 0),
                ContextVehicleOnly = Interlocked.Exchange(ref _contextVehicleOnly, 0),
                ContextCitizenOnly = Interlocked.Exchange(ref _contextCitizenOnly, 0),
                ContextVehicleAndCitizen = Interlocked.Exchange(ref _contextVehicleAndCitizen, 0),
                ContextNoIds = Interlocked.Exchange(ref _contextNoIds, 0),
                TmpeFindParkingForCitizen = Interlocked.Exchange(ref _tmpeFindParkingForCitizen, 0),
                TmpeParkPassengerCar = Interlocked.Exchange(ref _tmpeParkPassengerCar, 0),
                TmpeTryMoveParkedVehicle = Interlocked.Exchange(ref _tmpeTryMoveParkedVehicle, 0),
                TmpeTrySpawnParkedPassengerCar = Interlocked.Exchange(ref _tmpeTrySpawnParkedPassengerCar, 0),
                CandidateChecks = Interlocked.Exchange(ref _candidateChecks, 0),
                CandidateDenied = Interlocked.Exchange(ref _candidateDenied, 0),
                CandidateAllowed = Interlocked.Exchange(ref _candidateAllowed, 0),
                PropSearchNoContextDenied = Interlocked.Exchange(ref _propSearchNoContextDenied, 0),
                PropSearchDenied = Interlocked.Exchange(ref _propSearchDenied, 0),
                EvalVehicleCalls = Interlocked.Exchange(ref _evalVehicleCalls, 0),
                EvalCitizenCalls = Interlocked.Exchange(ref _evalCitizenCalls, 0),
                DenyNoDriverContext = Interlocked.Exchange(ref _denyNoDriverContext, 0),
                DenyNoCitizenContext = Interlocked.Exchange(ref _denyNoCitizenContext, 0),
                CreateBlocked = Interlocked.Exchange(ref _createBlocked, 0),
                CreateBypassTmpe = Interlocked.Exchange(ref _createBypassTmpe, 0),
                CreateCheckNoContext = Interlocked.Exchange(ref _createCheckNoContext, 0),
                CreateCheckNoOwner = Interlocked.Exchange(ref _createCheckNoOwner, 0),
                CreateCheckNoRuleBuilding = Interlocked.Exchange(ref _createCheckNoRuleBuilding, 0),
                VanillaFallbackFlipped = Interlocked.Exchange(ref _vanillaFallbackFlipped, 0),
                InvisiblesFixed = Interlocked.Exchange(ref _invisiblesFixed, 0),
                ReevalDeniedQueued = Interlocked.Exchange(ref _reevalDeniedQueued, 0),
                ReevalMoved = Interlocked.Exchange(ref _reevalMoved, 0),
                ReevalReleased = Interlocked.Exchange(ref _reevalReleased, 0)
            };
        }

        private struct Snapshot
        {
            public long ContextPushTotal;
            public long ContextPopTotal;
            public long ContextVehicleOnly;
            public long ContextCitizenOnly;
            public long ContextVehicleAndCitizen;
            public long ContextNoIds;
            public long TmpeFindParkingForCitizen;
            public long TmpeParkPassengerCar;
            public long TmpeTryMoveParkedVehicle;
            public long TmpeTrySpawnParkedPassengerCar;
            public long CandidateChecks;
            public long CandidateDenied;
            public long CandidateAllowed;
            public long PropSearchNoContextDenied;
            public long PropSearchDenied;
            public long EvalVehicleCalls;
            public long EvalCitizenCalls;
            public long DenyNoDriverContext;
            public long DenyNoCitizenContext;
            public long CreateBlocked;
            public long CreateBypassTmpe;
            public long CreateCheckNoContext;
            public long CreateCheckNoOwner;
            public long CreateCheckNoRuleBuilding;
            public long VanillaFallbackFlipped;
            public long InvisiblesFixed;
            public long ReevalDeniedQueued;
            public long ReevalMoved;
            public long ReevalReleased;
        }
    }
}
