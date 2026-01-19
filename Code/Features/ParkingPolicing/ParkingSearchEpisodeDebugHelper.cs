using System;
using ColossalFramework;
using PickyParking.Features.Debug;
using UnityEngine;
using PickyParking.Logging;
using PickyParking.Settings;

namespace PickyParking.Features.ParkingPolicing
{
    internal sealed class ParkingSearchEpisodeDebugHelper
    {
        private readonly int _startMs;

        public readonly ushort VehicleId;
        public readonly uint CitizenId;
        public bool IsVisitor { get; private set; }
        public readonly string Source;
        public readonly int StartDepth;

        public int CandidateChecks { get; private set; }
        public int DeniedCount { get; private set; }
        public int AllowedCount { get; private set; }

        public string LastReason { get; private set; }
        public ushort LastBuildingId { get; private set; }
        public string LastPrefab { get; private set; }
        public string LastBuildingName { get; private set; }

        public ParkingSearchEpisodeDebugHelper(ushort vehicleId, uint citizenId, bool isVisitor, string source, int startDepth)
        {
            VehicleId = vehicleId;
            CitizenId = citizenId;
            IsVisitor = isVisitor;
            Source = source;
            StartDepth = startDepth;
            _startMs = NowMs();
        }

        public void SetIsVisitor(bool isVisitor)
        {
            
            if (IsVisitor == isVisitor) return;

            IsVisitor = isVisitor;
        }

        public void RecordCandidate(bool denied, string reason, ushort buildingId, string prefabName, string buildingName)
        {
            CandidateChecks++;

            if (denied) DeniedCount++;
            else AllowedCount++;

            LastReason = reason;
            LastBuildingId = buildingId;
            LastPrefab = prefabName;
            LastBuildingName = buildingName;
        }

        public void EndAndMaybeLog(bool enabled, int minCandidates, int minDurationMs)
        {
            if (!enabled) return;

            int duration = Math.Max(0, NowMs() - _startMs);
            if (CandidateChecks == 0)
            {
                if (Log.IsVerboseEnabled && Log.IsDecisionDebugEnabled && IsVanillaSource(Source))
                {
                    Log.Info(DebugLogCategory.DecisionPipeline,
                        $"[SearchEpisode] ParkingSearchEpisode " +
                        $"src={Source ?? "NULL"} depth={StartDepth} " +
                        $"vehicleId={VehicleId} citizenId={CitizenId} isVisitor={IsVisitor} " +
                        $"candidates=0 denied=0 allowed=0 " +
                        $"durationMs={duration} " +
                        $"last=(NULL bld=0 prefab=NULL name=NULL)"
                    );
                }
                return;
            }

            if (CandidateChecks < minCandidates && duration < minDurationMs)
                return;

            if (Log.IsVerboseEnabled && Log.IsDecisionDebugEnabled)
            {
                Log.Info(DebugLogCategory.DecisionPipeline,
                    $"[SearchEpisode] ParkingSearchEpisode " +
                    $"src={Source ?? "NULL"} depth={StartDepth} " +
                    $"vehicleId={VehicleId} citizenId={CitizenId} isVisitor={IsVisitor} " +
                    $"candidates={CandidateChecks} denied={DeniedCount} allowed={AllowedCount} " +
                    $"durationMs={duration} " +
                    $"last=({LastReason ?? "NULL"} bld={LastBuildingId} prefab={LastPrefab ?? "NULL"} name={LastBuildingName ?? "NULL"})"
                );
            }
        }

        private static int NowMs() => (int)(Time.realtimeSinceStartup * 1000f);

        private static bool IsVanillaSource(string source)
        {
            if (string.IsNullOrEmpty(source))
                return false;

            return source.StartsWith("Vanilla.", StringComparison.Ordinal);
        }
    }
}

