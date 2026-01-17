using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Features.Debug;
using PickyParking.Logging;
using PickyParking.Settings;

namespace PickyParking.Patching.TMPE
{
    internal static class TMPE_ParkPassengerCarPatch
    {
        private const string TargetTypeName = "TrafficManager.Manager.Impl.VehicleBehaviorManager, TrafficManager";
        private const string TargetMethodName = "ParkPassengerCar";

        public static void Apply(Harmony harmony)
        {
            Type type = Type.GetType(TargetTypeName, throwOnError: false);
            if (type == null)
            {
                Log.Info(DebugLogCategory.Tmpe, "[TMPE] VehicleBehaviorManager not found; skipping ParkPassengerCar patch.");
                return;
            }

            MethodInfo method = AccessTools.Method(type, TargetMethodName);
            if (method == null)
            {
                Log.Info(DebugLogCategory.Tmpe, "[TMPE] ParkPassengerCar not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(TMPE_ParkPassengerCarPatch), nameof(Prefix)),
                postfix: new HarmonyMethod(typeof(TMPE_ParkPassengerCarPatch), nameof(Postfix)),
                finalizer: new HarmonyMethod(typeof(TMPE_ParkPassengerCarPatch), nameof(Finalizer))
            );

            Log.Info(DebugLogCategory.Tmpe, "[TMPE] Patched ParkPassengerCar (context injection).");
        }

        private static void Prefix([HarmonyArgument(0)] ushort vehicleId, [HarmonyArgument(3)] uint driverCitizenId, ref bool __state)
        {
            ParkingSearchContextPatchHandler.BeginParkPassengerCar(vehicleId, driverCitizenId, ref __state);
        }

        private static Exception Finalizer(Exception __exception, bool __state)
        {
            return ParkingSearchContextPatchHandler.EndParkPassengerCar(__exception, __state);
        }

        private static void Postfix(bool __result, [HarmonyArgument(7)] object extDriverInstance)
        {
            if (__result)
                return;

            if (IsDeniedByRules()
                && !ParkingDebugSettings.DisableClearKnownParkingOnDenied)
            {
                TryClearKnownParkingLocation(extDriverInstance);
            }
        }

        private static bool IsDeniedByRules()
        {
            if (!ParkingSearchContext.HasContext)
                return false;

            if (!ParkingSearchContext.TryGetEpisodeSnapshot(out var snapshot))
                return false;

            return !string.IsNullOrEmpty(snapshot.LastReason) &&
                   snapshot.LastReason.StartsWith("Denied_", StringComparison.Ordinal);
        }

        private static void TryClearKnownParkingLocation(object extDriverInstance)
        {
            if (extDriverInstance == null)
                return;

            try
            {
                Type extType = extDriverInstance.GetType();

                FieldInfo locationField = AccessTools.Field(extType, "parkingSpaceLocation");
                FieldInfo locationIdField = AccessTools.Field(extType, "parkingSpaceLocationId");

                if (locationField != null)
                {
                    object noneValue = locationField.FieldType.IsEnum
                        ? Enum.ToObject(locationField.FieldType, 0)
                        : Activator.CreateInstance(locationField.FieldType);
                    locationField.SetValue(extDriverInstance, noneValue);
                }

                if (locationIdField != null)
                    locationIdField.SetValue(extDriverInstance, (ushort)0);
            }
            catch (ArgumentException ex)
            {
                Log.AlwaysWarnOnce("TMPE.ClearKnownParkingLocation", "[TMPE] Failed to clear known parking location: " + ex);
            }
            catch (FieldAccessException ex)
            {
                Log.AlwaysWarnOnce("TMPE.ClearKnownParkingLocation", "[TMPE] Failed to clear known parking location: " + ex);
            }
            catch (InvalidOperationException ex)
            {
                Log.AlwaysWarnOnce("TMPE.ClearKnownParkingLocation", "[TMPE] Failed to clear known parking location: " + ex);
            }
            catch (TargetException ex)
            {
                Log.AlwaysWarnOnce("TMPE.ClearKnownParkingLocation", "[TMPE] Failed to clear known parking location: " + ex);
            }
            catch (TargetInvocationException ex)
            {
                Log.AlwaysWarnOnce("TMPE.ClearKnownParkingLocation", "[TMPE] Failed to clear known parking location: " + ex);
            }
        }
    }
}
