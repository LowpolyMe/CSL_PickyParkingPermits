using System;
using System.Collections.Generic;

namespace PickyParking.Infrastructure.Integration
{
    
    
    
    
    public static class ParkingSearchContext
    {
        public static int Depth => _stack != null ? _stack.Count : 0;
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

        private static Stack<Frame> Stack
        {
            get
            {
                if (_stack == null)
                {
                    _stack = new Stack<Frame>(4);
                }

                return _stack;
            }
        }

        public static bool HasContext => Stack.Count > 0;

        public static bool HasVehicleId => HasContext && Stack.Peek().VehicleId != 0;
        public static ushort VehicleId => HasVehicleId ? Stack.Peek().VehicleId : (ushort)0;

        public static bool HasCitizenId => HasContext && Stack.Peek().CitizenId != 0;
        public static uint CitizenId => HasCitizenId ? Stack.Peek().CitizenId : 0u;

        
        public static bool IsVisitor => HasContext && (Stack.Peek().Episode?.IsVisitor ?? false);
        public static string Source => HasContext ? Stack.Peek().Source : null;

        public static void SetEpisodeVisitorFlag(bool isVisitor)
        {
            if (!HasContext) return;
            Stack.Peek().Episode?.SetIsVisitor(isVisitor);
        }

        public static void Push(ushort vehicleId, uint citizenId, string source = null)
        {
            int startDepth = Depth + 1;

            
            var episode = new ParkingSearchEpisodeDebugHelper(
                vehicleId,
                citizenId,
                isVisitor: false,
                source: source,
                startDepth: startDepth
            );

            Stack.Push(new Frame(vehicleId, citizenId, source, episode));
        }

        public static void RecordCandidate(bool denied, string reason, ushort buildingId, string prefabName)
        {
            if (!HasContext) return;
            var f = Stack.Peek();
            f.Episode?.RecordCandidate(denied, reason, buildingId, prefabName);
        }

        public static void Pop()
        {
            if (!HasContext) return;

            var frame = Stack.Pop();
            frame.Episode?.EndAndMaybeLog(
                enabled: EnableEpisodeLogs,
                minCandidates: LogMinCandidates,
                minDurationMs: LogMinDurationMs
            );
        }

        
        
        
        public static void ClearAll()
        {
            _stack?.Clear();
        }
    }
}


