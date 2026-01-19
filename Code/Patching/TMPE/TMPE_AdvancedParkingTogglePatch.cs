using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Features.Debug;
using PickyParking.Logging;
using PickyParking.ModEntry;
using PickyParking.ModLifecycle.BackendSelection;

namespace PickyParking.Patching.TMPE
{
    internal static class TMPE_AdvancedParkingTogglePatch
    {
        private const string TargetTypeName = "TrafficManager.Manager.AbstractFeatureManager, TrafficManager";
        private const string EnableMethodName = "OnEnableFeature";
        private const string DisableMethodName = "OnDisableFeature";

        public static void Apply(Harmony harmony)
        {
            Type type = Type.GetType(TargetTypeName, false);
            if (type == null)
            {
                Log.Info(DebugLogCategory.Tmpe, "[TMPE] AdvancedParkingManager not found; skipping toggle patch.");
                return;
            }

            MethodInfo enableMethod = FindToggleMethod(type, EnableMethodName);
            MethodInfo disableMethod = FindToggleMethod(type, DisableMethodName);

            if (enableMethod == null && disableMethod == null)
            {
                Log.Info(DebugLogCategory.Tmpe, "[TMPE] AdvancedParkingManager toggle methods not found; skipping patch.");
                return;
            }

            if (enableMethod != null)
            {
                harmony.Patch(
                    enableMethod,
                    postfix: new HarmonyMethod(typeof(TMPE_AdvancedParkingTogglePatch), nameof(EnablePostfix))
                );
            }
            else
            {
                Log.Info(DebugLogCategory.Tmpe, "[TMPE] AdvancedParkingManager.OnEnableFeature not found; skipping patch.");
            }

            if (disableMethod != null)
            {
                harmony.Patch(
                    disableMethod,
                    postfix: new HarmonyMethod(typeof(TMPE_AdvancedParkingTogglePatch), nameof(DisablePostfix))
                );
            }
            else
            {
                Log.Info(DebugLogCategory.Tmpe, "[TMPE] AdvancedParkingManager.OnDisableFeature not found; skipping patch.");
            }

            Log.Info(DebugLogCategory.Tmpe, "[TMPE] Patched AbstractFeatureManager toggles (backend refresh).");
        }

        private static MethodInfo FindToggleMethod(Type type, string methodName)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                    continue;
                if (method.ReturnType != typeof(void))
                    continue;
                if (method.GetParameters().Length != 0)
                    continue;
                return method;
            }

            return null;
        }

        private static void EnablePostfix()
        {
            RefreshBackendState();
        }

        private static void DisablePostfix()
        {
            RefreshBackendState();
        }

        private static void RefreshBackendState()
        {
            ModRuntime runtime = ModRuntime.Current;
            if (runtime == null)
                return;

            ParkingBackendState state = runtime.ParkingBackendState;
            if (state == null)
                return;

            state.Refresh();
        }
    }
}
