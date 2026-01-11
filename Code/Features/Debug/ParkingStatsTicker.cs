using System.Threading;
using UnityEngine;
using PickyParking.Logging;

namespace PickyParking.Features.Debug
{
    public sealed class ParkingStatsTicker : MonoBehaviour
    {
        private const string RuntimeObjectName = "PickyParking.Runtime";
        private static ParkingStatsTicker _instance;
        private static bool _desiredEnabled = true;
        private static int _missingInstanceLogged;

        private float _nextLogTime;
        private float _lastLogTime;
        private bool _wasLogging;

        public static void SetEnabled(bool enabled)
        {
            _desiredEnabled = enabled;
            if (enabled && _instance == null)
                TryEnsureInstance();
            if (_instance != null)
                _instance.enabled = enabled;
            if (!enabled)
                ParkingStatsCounter.ResetAll();
            else if (_instance == null && Interlocked.Exchange(ref _missingInstanceLogged, 1) == 0)
                Log.Warn("[Parking] Stats ticker enabled but no instance exists yet.");
        }

        private void OnEnable()
        {
            _instance = this;
            enabled = _desiredEnabled;
            if (!enabled)
                return;

            float now = Time.unscaledTime;
            _lastLogTime = now;
            _nextLogTime = now + ParkingStatsCounter.DefaultIntervalSeconds;
            _wasLogging = false;
        }

        private void OnDisable()
        {
            if (_instance == this)
                _instance = null;
            _wasLogging = false;
        }

        private void Update()
        {
            if (!ParkingStatsCounter.ShouldLog)
            {
                if (_wasLogging)
                {
                    ParkingStatsCounter.ResetAll();
                    _wasLogging = false;
                }
                return;
            }

            if (!_wasLogging)
            {
                ParkingStatsCounter.ResetAll();
                _lastLogTime = Time.unscaledTime;
                _nextLogTime = _lastLogTime + ParkingStatsCounter.DefaultIntervalSeconds;
                _wasLogging = true;
                Log.Info("[Parking] Stats ticker active (interval=60s).");
            }

            float now = Time.unscaledTime;
            if (now < _nextLogTime)
                return;

            float windowSeconds = Mathf.Max(1f, now - _lastLogTime);
            ParkingStatsCounter.LogAndReset(windowSeconds);
            _lastLogTime = now;
            _nextLogTime = now + ParkingStatsCounter.DefaultIntervalSeconds;
        }

        private static void TryEnsureInstance()
        {
            var runtimeObject = GameObject.Find(RuntimeObjectName);
            if (runtimeObject == null)
                return;

            var ticker = runtimeObject.GetComponent<ParkingStatsTicker>();
            if (ticker == null)
                ticker = runtimeObject.AddComponent<ParkingStatsTicker>();

            _instance = ticker;
        }
    }
}
