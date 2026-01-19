using System;
using System.Threading;
using PickyParking.Features.Debug;
using PickyParking.Features.ParkingPolicing.Runtime;
using PickyParking.Logging;
using PickyParking.ModLifecycle.BackendSelection;
using PickyParking.Settings;

namespace PickyParking.Features.ParkingPolicing
{
    internal static class VanillaParkingContextInjector
    {
        private const string ParkVehicleSource = "Vanilla.PassengerCarAI.ParkVehicle";
        private const string UpdateParkedVehicleSource = "Vanilla.PassengerCarAI.UpdateParkedVehicle";
        private static int _parkVehicleInjectedLogged;
        private static int _parkVehiclePoppedLogged;
        private static int _updateParkedInjectedLogged;
        private static int _updateParkedPoppedLogged;

        public static void BeginParkVehicle(ushort vehicleId, ref bool state)
        {
            state = false;
            try
            {
                if (!ShouldInject())
                    return;

                if (ParkingSearchContext.HasContext)
                    return;

                uint citizenId;
                string reason;
                if (!CitizenIdResolver.TryGetCitizenIdFromVehicle(vehicleId, out citizenId, out reason))
                {
                    string resolvedReason = reason ?? "unknown";
                    if (Log.Dev.IsEnabled(DebugLogCategory.Enforcement))
                    {
                        Log.Dev.Warn(
                            DebugLogCategory.Enforcement,
                            LogPath.Vanilla,
                            "VanillaContextMissingCitizen",
                            "src=ParkVehicle | vehicleId=" + vehicleId + " | reason=" + resolvedReason,
                            "VanillaNoCitizen." + vehicleId);
                    }
                    citizenId = 0u;
                }

                ParkingContextScope.Push(vehicleId, citizenId, ParkVehicleSource);
                state = true;

                if (Log.Dev.IsEnabled(DebugLogCategory.Enforcement) &&
                    Interlocked.Exchange(ref _parkVehicleInjectedLogged, 1) == 0)
                {
                    Log.Dev.Info(DebugLogCategory.Enforcement, LogPath.Vanilla, "ContextPush", "source=" + ParkVehicleSource);
                }
            }
            catch (Exception ex)
            {
                Log.Dev.Exception(DebugLogCategory.Enforcement, LogPath.Vanilla, "ContextPushPrefixException", ex);
            }
        }

        public static void BeginUpdateParkedVehicle(ref VehicleParked parkedData, ref bool state)
        {
            state = false;
            try
            {
                if (!ShouldInject())
                    return;

                if (ParkingSearchContext.HasContext)
                    return;

                uint citizenId = parkedData.m_ownerCitizen;
                ParkingContextScope.Push(0, citizenId, UpdateParkedVehicleSource);
                state = true;

                if (Log.Dev.IsEnabled(DebugLogCategory.Enforcement) &&
                    Interlocked.Exchange(ref _updateParkedInjectedLogged, 1) == 0)
                {
                    Log.Dev.Info(DebugLogCategory.Enforcement, LogPath.Vanilla, "ContextPush", "source=" + UpdateParkedVehicleSource);
                }
            }
            catch (Exception ex)
            {
                Log.Dev.Exception(DebugLogCategory.Enforcement, LogPath.Vanilla, "ContextPushPrefixException", ex);
            }
        }

        public static Exception EndParkVehicle(Exception exception, bool state)
        {
            return EndScope(exception, state, ParkVehicleSource, ref _parkVehiclePoppedLogged);
        }

        public static Exception EndUpdateParkedVehicle(Exception exception, bool state)
        {
            return EndScope(exception, state, UpdateParkedVehicleSource, ref _updateParkedPoppedLogged);
        }

        private static Exception EndScope(Exception exception, bool state, string source, ref int loggedFlag)
        {
            try
            {
                if (state)
                    ParkingContextScope.Pop();

                if (state && Log.Dev.IsEnabled(DebugLogCategory.Enforcement) &&
                    Interlocked.Exchange(ref loggedFlag, 1) == 0)
                {
                    Log.Dev.Info(DebugLogCategory.Enforcement, LogPath.Vanilla, "ContextPop", "source=" + source);
                }
            }
            catch (Exception ex)
            {
                Log.Dev.Exception(DebugLogCategory.Enforcement, LogPath.Vanilla, "ContextPopFinalizerException", ex);
            }

            return exception;
        }

        private static bool ShouldInject()
        {
            ParkingRuntimeContext context = ParkingRuntimeContext.Current;
            if (context == null) return false;
            if (!context.FeatureGate.IsActive) return false;
            if (ParkingDebugSettings.DisableParkingEnforcement) return false;

            ParkingBackendState backendState = context.ParkingBackendState;
            if (backendState == null) return false;

            if (backendState.ActiveBackend != ParkingBackendKind.Vanilla)
            {
                LogVanillaBypassIfTmpeActive(backendState);
                return false;
            }

            return true;
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
