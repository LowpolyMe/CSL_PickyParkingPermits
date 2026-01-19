using System;
using System.Collections.Generic;

namespace PickyParking.Logging
{
    internal sealed class LogRateLimiter
    {
        private readonly int _maxKeys;
        private readonly TimeSpan _ttl;

        private readonly object _lock = new object();
        private readonly Dictionary<string, LogRateLimiterEntry> _entriesByKey = new Dictionary<string, LogRateLimiterEntry>();

        public LogRateLimiter(int maxKeys, TimeSpan ttl)
        {
            _maxKeys = maxKeys;
            _ttl = ttl;
        }

        public bool TryConsume(string key, TimeSpan interval, out int dropped)
        {
            DateTime now = DateTime.UtcNow;

            lock (_lock)
            {
                LogRateLimiterEntry entry;
                if (_entriesByKey.TryGetValue(key, out entry))
                {
                    if (now - entry.LastLoggedUtc < interval)
                    {
                        entry.DroppedCount++;
                        dropped = 0;
                        return false;
                    }

                    dropped = entry.DroppedCount;
                    entry.DroppedCount = 0;
                    entry.LastLoggedUtc = now;
                    return true;
                }

                EvictIfNeeded(now);

                dropped = 0;
                _entriesByKey[key] = new LogRateLimiterEntry(now);
                return true;
            }
        }

        private void EvictIfNeeded(DateTime now)
        {
            if (_entriesByKey.Count < _maxKeys)
                return;

            DateTime cutoff = now - _ttl;

            List<string> toRemove = null;
            foreach (KeyValuePair<string, LogRateLimiterEntry> pair in _entriesByKey)
            {
                if (pair.Value.LastLoggedUtc < cutoff)
                {
                    if (toRemove == null)
                    {
                        toRemove = new List<string>();
                    }

                    toRemove.Add(pair.Key);
                }
            }

            if (toRemove != null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                {
                    _entriesByKey.Remove(toRemove[i]);
                }
            }
            
            if (_entriesByKey.Count >= _maxKeys)
            {
                _entriesByKey.Clear();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _entriesByKey.Clear();
            }
        }

        private sealed class LogRateLimiterEntry
        {
            public LogRateLimiterEntry(DateTime lastLoggedUtc)
            {
                LastLoggedUtc = lastLoggedUtc;
                DroppedCount = 0;
            }

            public DateTime LastLoggedUtc { get; set; }
            public int DroppedCount { get; set; }
        }
    }
}
