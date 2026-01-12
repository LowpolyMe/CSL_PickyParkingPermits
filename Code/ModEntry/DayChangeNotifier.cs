using System;
using ColossalFramework;
using ICities;
using PickyParking.Features.Debug;

namespace PickyParking.ModEntry
{
    public sealed class DayChangeNotifier : ThreadingExtensionBase
    {
        private bool _hasDayStamp;
        private long _lastDayStamp;

        public override void OnReleased()
        {
            _hasDayStamp = false;
        }

        public override void OnAfterSimulationTick()
        {
            var runtime = ModRuntime.Current;
            if (runtime == null)
                return;

            bool sweepsEnabled = runtime.SettingsController?.Current?.EnableParkingRuleSweeps ?? true;
            long dayStamp = GetCurrentGameTime().Date.Ticks;
            bool dayChanged = !_hasDayStamp || dayStamp != _lastDayStamp;
            if (!_hasDayStamp)
                _hasDayStamp = true;
            if (!dayChanged)
            {
                if (sweepsEnabled)
                    runtime.ParkedVehicleReevaluation?.TryRequestNextScheduledBuilding(resetSweep: false);
                return;
            }

            _lastDayStamp = dayStamp;
            ParkingStatsTicker.NotifyDayChanged();
            if (sweepsEnabled)
            {
                runtime.ParkedVehicleReevaluation?.NotifyDayChanged();
                runtime.ParkedVehicleReevaluation?.TryRequestNextScheduledBuilding(resetSweep: false);
            }
        }

        private static DateTime GetCurrentGameTime()
        {
            var sim = Singleton<SimulationManager>.instance;
            if (sim == null)
                return DateTime.MinValue;

            return sim.m_currentGameTime;
        }
    }
}
