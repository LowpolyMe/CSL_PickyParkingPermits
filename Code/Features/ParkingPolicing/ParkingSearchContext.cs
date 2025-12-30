using System;
using System.Collections.Generic;
using System.Threading;
using PickyParking.Logging;
using PickyParking.ModLifecycle;

namespace PickyParking.Features.ParkingPolicing
{
    
    
    
    
    public static class ParkingSearchContext
    {
        private static int _wrongThreadLogged;

        public static int Depth
        {
            get
            {
                var stack = GetStackOrNull(createIfMissing: false, requireSimulationThread: true, caller: "Depth");
                return stack != null ? stack.Count : 0;
            }
        }
        public static bool EnableEpisodeLogs = true;
        public static int LogMinCandidates = 10;
        public static int LogMinDurationMs = 50;

        internal struct Frame
        {
            public readonly ushort VehicleId;
            public readonly uint CitizenId;
            public readonly string Source;
            public readonly ParkingSearchEpisodeDebugHelper Episode;

            public Frame(ushort vehicleId, uint citizenId, string source, ParkingSearchEpisodeDebugHelper episode)
            {
                VehicleId = vehicleId;
                CitizenId = citizenId;
                Source = source;
                Episode = episode;
            }
        }

        [ThreadStatic] private static Stack<Frame> _stack;

        private static Stack<Frame> Stack => GetStackOrNull(createIfMissing: true, requireSimulationThread: true, caller: "Stack");

        public static bool HasContext
        {
            get
            {
                var stack = GetStackOrNull(createIfMissing: false, requireSimulationThread: true, caller: "HasContext");
                return stack != null && stack.Count > 0;
            }
        }

        public static bool HasVehicleId => HasContext && Stack.Peek().VehicleId != 0;
        public static ushort VehicleId => HasVehicleId ? Stack.Peek().VehicleId : (ushort)0;

        public static bool HasCitizenId => HasContext && Stack.Peek().CitizenId != 0;
        public static uint CitizenId => HasCitizenId ? Stack.Peek().CitizenId : 0u;

        
        public static bool IsVisitor => HasContext && (Stack.Peek().Episode?.IsVisitor ?? false);
        public static string Source => HasContext ? Stack.Peek().Source : null;

        public static void SetEpisodeVisitorFlag(bool isVisitor)
        {
            var stack = GetStackOrNull(createIfMissing: false, requireSimulationThread: true, caller: "SetEpisodeVisitorFlag");
            if (stack == null || stack.Count == 0) return;
            stack.Peek().Episode?.SetIsVisitor(isVisitor);
        }

        public static void Push(ushort vehicleId, uint citizenId, string source = null)
        {
            var stack = GetStackOrNull(createIfMissing: true, requireSimulationThread: true, caller: "Push");
            if (stack == null) return;
            int startDepth = stack.Count + 1;

            
            var episode = new ParkingSearchEpisodeDebugHelper(
                vehicleId,
                citizenId,
                isVisitor: false,
                source: source,
                startDepth: startDepth
            );

            stack.Push(new Frame(vehicleId, citizenId, source, episode));
        }

        public static void RecordCandidate(bool denied, string reason, ushort buildingId, string prefabName)
        {
            var stack = GetStackOrNull(createIfMissing: false, requireSimulationThread: true, caller: "RecordCandidate");
            if (stack == null || stack.Count == 0) return;
            var f = stack.Peek();
            f.Episode?.RecordCandidate(denied, reason, buildingId, prefabName);
        }

        public static void Pop()
        {
            var stack = GetStackOrNull(createIfMissing: false, requireSimulationThread: true, caller: "Pop");
            if (stack == null || stack.Count == 0) return;

            var frame = stack.Pop();
            frame.Episode?.EndAndMaybeLog(
                enabled: EnableEpisodeLogs,
                minCandidates: LogMinCandidates,
                minDurationMs: LogMinDurationMs
            );
        }

        
        
        
        public static void ClearAll()
        {
            var stack = GetStackOrNull(createIfMissing: false, requireSimulationThread: false, caller: "ClearAll");
            stack?.Clear();
        }

        private static Stack<Frame> GetStackOrNull(bool createIfMissing, bool requireSimulationThread, string caller)
        {
            if (requireSimulationThread && !EnsureSimulationThread(caller))
                return null;

            if (_stack == null && createIfMissing)
            {
                _stack = new Stack<Frame>(4);
            }

            return _stack;
        }

        private static bool EnsureSimulationThread(string caller)
        {
            if (SimThread.IsSimulationThread())
                return true;

            if (Interlocked.Exchange(ref _wrongThreadLogged, 1) == 0)
            {
                Log.Warn("[Runtime] ParkingSearchContext accessed off simulation thread; caller=" + (caller ?? "UNKNOWN"));
            }

            return false;
        }
    }
}


