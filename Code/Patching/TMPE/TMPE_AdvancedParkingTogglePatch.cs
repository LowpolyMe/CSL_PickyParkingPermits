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
        private const string AdvancedParkingManagerTypeName = "TrafficManager.Manager.Impl.AdvancedParkingManager";
        private const string EnableMethodName = "OnEnableFeature";
        private const string DisableMethodName = "OnDisableFeature";

        public static void Apply(Harmony harmony)
        {
            Type type = Type.GetType(TargetTypeName, false);
            if (type == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                {
                    Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "PatchSkippedMissingType", "type=AbstractFeatureManager");
                }
                return;
            }

            MethodInfo enableMethod = FindToggleMethod(type, EnableMethodName);
            MethodInfo disableMethod = FindToggleMethod(type, DisableMethodName);

            if (enableMethod == null && disableMethod == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                {
                    Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "PatchSkippedMissingMethod", "type=AbstractFeatureManager | method=Toggle");
                }
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
                if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                {
                    Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "PatchSkippedMissingMethod", "type=AbstractFeatureManager | method=" + EnableMethodName);
                }
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
                if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
                {
                    Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "PatchSkippedMissingMethod", "type=AbstractFeatureManager | method=" + DisableMethodName);
                }
            }

            if (Log.Dev.IsEnabled(DebugLogCategory.Tmpe))
            {
                Log.Dev.Info(DebugLogCategory.Tmpe, LogPath.TMPE, "PatchApplied", "type=AbstractFeatureManager | behavior=BackendRefresh");
            }
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

        private static void EnablePostfix(object __instance)
        {
            RefreshBackendState(__instance);
        }

        private static void DisablePostfix(object __instance)
        {
            RefreshBackendState(__instance);
        }

        private static void RefreshBackendState(object instance)
        {
            if (!IsAdvancedParkingManager(instance))
                return;

            ModRuntime runtime = ModRuntime.Current;
            if (runtime == null)
                return;

            ParkingBackendState state = runtime.ParkingBackendState;
            if (state == null)
                return;

            state.Refresh();
        }

        private static bool IsAdvancedParkingManager(object instance)
        {
            if (instance == null)
                return false;

            Type type = instance.GetType();
            string fullName = type.FullName;
            if (string.IsNullOrEmpty(fullName))
                return false;

            return string.Equals(fullName, AdvancedParkingManagerTypeName, StringComparison.Ordinal);
        }
    }
}
