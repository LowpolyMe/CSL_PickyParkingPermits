using System;
using System.Collections.Generic;

namespace PickyParking.Logging
{
    internal sealed class LogRateLimiter
    {
        private readonly int _maxKeys;
        private readonly TimeSpan _ttl;

        private readonly object _lock = new object();
        private readonly Dictionary<string, DateTime> _lastLoggedByKey = new Dictionary<string, DateTime>();

        public LogRateLimiter(int maxKeys, TimeSpan ttl)
        {
            _maxKeys = maxKeys;
            _ttl = ttl;
        }

        public bool ShouldLog(string key, TimeSpan interval)
        {
            var now = DateTime.UtcNow;

            lock (_lock)
            {
                DateTime last;
                if (_lastLoggedByKey.TryGetValue(key, out last))
                {
                    if (now - last < interval)
                        return false;
                }

                EvictIfNeeded(now);

                _lastLoggedByKey[key] = now;
                return true;
            }
        }

        private void EvictIfNeeded(DateTime now)
        {
            if (_lastLoggedByKey.Count < _maxKeys)
                return;

            DateTime cutoff = now - _ttl;

            List<string> toRemove = null;
            foreach (var pair in _lastLoggedByKey)
            {
                if (pair.Value < cutoff)
                {
                    if (toRemove == null) toRemove = new List<string>();
                    toRemove.Add(pair.Key);
                }
            }

            if (toRemove != null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                    _lastLoggedByKey.Remove(toRemove[i]);
            }
            
            if (_lastLoggedByKey.Count >= _maxKeys)
                _lastLoggedByKey.Clear();
        }

        public void Clear()
        {
            lock (_lock)
            {
                _lastLoggedByKey.Clear();
            }
        }
    }
}