using System;
using System.Collections.Generic;
using ColossalFramework;

namespace PickyParking.Features.Debug
{
    internal static class VehicleDespawnReasonCache
    {
        private const uint MaxAgeFrames = 1024;
        private static readonly object _lock = new object();
        private static readonly Dictionary<ushort, ReasonEntry> _reasons = new Dictionary<ushort, ReasonEntry>();

        private struct ReasonEntry
        {
            public string Reason;
            public uint Frame;
        }

        public static void Record(ushort vehicleId, string reason)
        {
            if (vehicleId == 0 || string.IsNullOrEmpty(reason))
                return;

            uint frame = GetFrameIndex();
            lock (_lock)
            {
                _reasons[vehicleId] = new ReasonEntry
                {
                    Reason = reason,
                    Frame = frame
                };
            }
        }

        public static bool TryConsume(ushort vehicleId, out string reason)
        {
            reason = null;
            if (vehicleId == 0)
                return false;

            uint frame = GetFrameIndex();
            lock (_lock)
            {
                if (!_reasons.TryGetValue(vehicleId, out var entry))
                    return false;

                _reasons.Remove(vehicleId);
                if (IsStale(entry, frame))
                    return false;

                reason = entry.Reason;
                return !string.IsNullOrEmpty(reason);
            }
        }

        public static void Prune()
        {
            uint frame = GetFrameIndex();
            lock (_lock)
            {
                if (_reasons.Count == 0)
                    return;

                var stale = new List<ushort>();
                foreach (var kvp in _reasons)
                {
                    if (IsStale(kvp.Value, frame))
                        stale.Add(kvp.Key);
                }

                for (int i = 0; i < stale.Count; i++)
                    _reasons.Remove(stale[i]);
            }
        }

        private static bool IsStale(ReasonEntry entry, uint currentFrame)
        {
            if (currentFrame < entry.Frame)
                return true;
            return currentFrame - entry.Frame > MaxAgeFrames;
        }

        private static uint GetFrameIndex()
        {
            try
            {
                return Singleton<SimulationManager>.instance.m_currentFrameIndex;
            }
            catch
            {
                return 0u;
            }
        }
    }
}
