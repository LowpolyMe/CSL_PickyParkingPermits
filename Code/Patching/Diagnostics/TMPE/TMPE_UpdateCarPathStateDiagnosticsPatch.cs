using System;
using System.Reflection;
using HarmonyLib;
using PickyParking.Logging;
using PickyParking.ModLifecycle;
using PickyParking.Patching.TMPE;

namespace PickyParking.Patching.Diagnostics.TMPE
{
    internal static class TMPE_UpdateCarPathStateDiagnosticsPatch
    {
        private const string TargetTypeName = "TrafficManager.Manager.Impl.AdvancedParkingManager, TrafficManager";
        private const string TargetMethodName = "UpdateCarPathState";
        private const int MaxPrefixLogs = 20;
        private static int _prefixLogCount;

        public static void Apply(Harmony harmony)
        {
            Type type = Type.GetType(TargetTypeName, throwOnError: false);
            if (type == null)
            {
                if (Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled)
                    Log.Info("[TMPE] AdvancedParkingManager not found; skipping UpdateCarPathState diagnostics patch.");
                return;
            }

            MethodInfo[] methods = FindTargetMethods(type);
            if (methods == null || methods.Length == 0)
            {
                if (Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled)
                    Log.Info("[TMPE] UpdateCarPathState overload not found; skipping diagnostics patch.");
                return;
            }

            foreach (MethodInfo method in methods)
            {
                harmony.Patch(
                    method,
                    prefix: new HarmonyMethod(typeof(TMPE_UpdateCarPathStateDiagnosticsPatch), nameof(Prefix)),
                    postfix: new HarmonyMethod(typeof(TMPE_UpdateCarPathStateDiagnosticsPatch), nameof(Postfix))
                );
            }

            if (Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled)
                Log.Info($"[TMPE] Patched UpdateCarPathState (diagnostics). count={methods.Length}");
        }

        private static MethodInfo[] FindTargetMethods(Type advancedParkingManagerType)
        {
            var matches = new System.Collections.Generic.List<MethodInfo>();
            foreach (var m in advancedParkingManagerType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(m.Name, TargetMethodName, StringComparison.Ordinal))
                    continue;

                var ps = m.GetParameters();
                if (ps.Length != 5)
                    continue;

                if (ps[0].ParameterType != typeof(ushort)) continue;
                if (!IsByRefOf(ps[1].ParameterType, typeof(Vehicle))) continue;
                if (!IsByRefOf(ps[2].ParameterType, typeof(CitizenInstance))) continue;

                if (!ps[3].ParameterType.IsByRef) continue;
                var extElem = ps[3].ParameterType.GetElementType();
                if (extElem == null || !string.Equals(extElem.Name, "ExtCitizenInstance", StringComparison.Ordinal)) continue;

                matches.Add(m);
            }

            return matches.ToArray();
        }

        private static bool IsByRefOf(Type maybeByRef, Type elementType)
        {
            if (!maybeByRef.IsByRef) return false;
            return maybeByRef.GetElementType() == elementType;
        }

        private static void Prefix(
            [HarmonyArgument(0)] ushort vehicleId,
            [HarmonyArgument(3)] object extDriverInstance,
            [HarmonyArgument(4)] object pathStateObj)
        {
            if (!SimThread.IsSimulationThread())
                return;

            try
            {
                if (Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled && _prefixLogCount < MaxPrefixLogs)
                {
                    if (ParkingPathModeTracker.TryDescribeUpdateArgs(vehicleId, extDriverInstance, pathStateObj, out string desc))
                    {
                        Log.Info("[TMPE] UpdateCarPathState prefix hit. " + desc);
                    }
                    _prefixLogCount++;
                }
                ParkingPathModeTracker.RecordIfCalculating(vehicleId, extDriverInstance);
                ParkingPathModeTracker.RecordBeforeUpdateCarPathState(vehicleId, extDriverInstance, pathStateObj);
                ParkingPathModeTracker.RecordFromUpdateCarPathState(vehicleId, extDriverInstance, pathStateObj);
            }
            catch (Exception ex)
            {
                Log.Error("[TMPE] UpdateCarPathState diagnostics prefix exception\n" + ex);
            }
        }

        private static void Postfix(
            [HarmonyArgument(0)] ushort vehicleId,
            [HarmonyArgument(3)] object extDriverInstance,
            [HarmonyArgument(4)] object pathStateObj)
        {
            if (!SimThread.IsSimulationThread())
                return;

            try
            {
                ParkingPathModeTracker.RecordAfterUpdateCarPathState(vehicleId, extDriverInstance, pathStateObj);
            }
            catch (Exception ex)
            {
                Log.Error("[TMPE] UpdateCarPathState diagnostics postfix exception\n" + ex);
            }
        }
    }
}
