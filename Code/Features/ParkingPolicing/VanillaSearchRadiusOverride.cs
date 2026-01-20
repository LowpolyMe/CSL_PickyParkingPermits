using System.Threading;
using PickyParking.Features.Debug;
using PickyParking.Features.ParkingPolicing.Runtime;
using PickyParking.Logging;
using PickyParking.ModEntry;
using PickyParking.ModLifecycle.BackendSelection;
using UnityEngine;

namespace PickyParking.Features.ParkingPolicing
{
    internal static class VanillaSearchRadiusOverride
    {
        private const string ParkVehicleSource = "Vanilla.PassengerCarAI.ParkVehicle";
        private const string TmpeParkVehicleSource = "TMPE.ParkPassengerCar";
        private static int _lastLoggedRadius = -1;
        private static int _prefixLogged;
        private static int _skipNonParkVehicleLogged;
        private static int _skipIgnoreParkedLogged;

        public static void ApplyPrefix(ref float maxDistance, ushort ignoreParked)
        {
            LogPrefixOnce(ignoreParked, maxDistance);
            TryOverride(ref maxDistance, ignoreParked);
        }

        public static bool TryOverride(ref float maxDistance, ushort ignoreParked)
        {
            ParkingRuntimeContext context = ParkingRuntimeContext.Current;
            if (context == null)
                return false;

            ParkingBackendState backendState = context.ParkingBackendState;
            if (backendState == null)
                return false;

            if (backendState.ActiveBackend != ParkingBackendKind.Vanilla)
            {
                LogVanillaBypassIfTmpeActive(backendState);
                return false;
            }

            if (ParkingDebugSettings.DisableParkingEnforcement)
                return false;

            string source = ParkingSearchContext.Source;
            if (!IsEligibleSource(source))
            {
                LogSkipNonParkVehicle(source, ignoreParked);
                return false;
            }

            if (ignoreParked != 0)
            {
                LogSkipIgnoreParked(source, ignoreParked);
                return false;
            }

            ModRuntime runtime = ModRuntime.Current;
            if (runtime == null || runtime.SettingsController == null || runtime.SettingsController.Current == null)
                return false;

            int meters = runtime.SettingsController.Current.VanillaBuildingSearchRadiusMeters;
            if (meters <= 0)
                meters = 16;

            int clamped = Mathf.Clamp(meters, 16, 256);
            maxDistance = clamped;
            LogAppliedRadius(clamped, source);
            return true;
        }

        private static void LogPrefixOnce(ushort ignoreParked, float maxDistance)
        {
            if (!Log.Dev.IsEnabled(DebugLogCategory.Enforcement))
                return;

            if (Interlocked.Exchange(ref _prefixLogged, 1) != 0)
                return;

            string source = ParkingSearchContext.Source ?? "NULL";
            Log.Dev.Info(
                DebugLogCategory.Enforcement,
                LogPath.Vanilla,
                "VanillaRadiusPrefixHit",
                "source=" + source +
                " | ignoreParked=" + ignoreParked +
                " | maxDistance=" + maxDistance.ToString("F2"));
        }

        private static void LogAppliedRadius(int meters, string source)
        {
            if (!Log.Dev.IsEnabled(DebugLogCategory.Enforcement))
                return;

            if (_lastLoggedRadius == meters)
                return;

            _lastLoggedRadius = meters;
            Log.Dev.Info(
                DebugLogCategory.Enforcement,
                LogPath.Vanilla,
                "VanillaRadiusApplied",
                "meters=" + meters + " | source=" + (source ?? "NULL"));
        }

        private static bool IsEligibleSource(string source)
        {
            if (string.Equals(source, ParkVehicleSource, System.StringComparison.Ordinal))
                return true;

            return string.Equals(source, TmpeParkVehicleSource, System.StringComparison.Ordinal);
        }

        private static void LogSkipNonParkVehicle(string source, ushort ignoreParked)
        {
            if (!Log.Dev.IsEnabled(DebugLogCategory.Enforcement))
                return;

            if (_skipNonParkVehicleLogged != 0)
                return;

            _skipNonParkVehicleLogged = 1;
            Log.Dev.Info(
                DebugLogCategory.Enforcement,
                LogPath.Vanilla,
                "VanillaRadiusSkippedNonArrival",
                "source=" + (source ?? "NULL") + " | ignoreParked=" + ignoreParked);
        }

        private static void LogSkipIgnoreParked(string source, ushort ignoreParked)
        {
            if (!Log.Dev.IsEnabled(DebugLogCategory.Enforcement))
                return;

            if (_skipIgnoreParkedLogged != 0)
                return;

            _skipIgnoreParkedLogged = 1;
            Log.Dev.Info(
                DebugLogCategory.Enforcement,
                LogPath.Vanilla,
                "VanillaRadiusSkippedRelocation",
                "source=" + (source ?? "NULL") + " | ignoreParked=" + ignoreParked);
        }

        private static void LogVanillaBypassIfTmpeActive(ParkingBackendState backendState)
        {
            if (backendState == null)
                return;

            if (backendState.ActiveBackend != ParkingBackendKind.TmpeAdvanced
                && backendState.ActiveBackend != ParkingBackendKind.TmpeBasic)
                return;

            if (Log.Dev.IsEnabled(DebugLogCategory.Enforcement))
            {
                Log.Dev.Warn(
                    DebugLogCategory.Enforcement,
                    LogPath.Any,
                    "VanillaBackendBypassed",
                    "reason=TmpeActive | activeBackend=" + backendState.ActiveBackend,
                    "VanillaBypass.TmpeActive");
            }
        }
    }
}
