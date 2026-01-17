using System;
using System.Reflection;
using ColossalFramework;
using HarmonyLib;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Features.ParkingPolicing.Runtime;
using PickyParking.Logging;
using PickyParking.Patching.TMPE;
using PickyParking.Settings;

namespace PickyParking.Patching.Diagnostics.TMPE
{
    internal static class TMPE_FindParkingSpacePropAtBuildingDiagnosticsPatch
    {
        private const string TargetTypeName = "TrafficManager.Manager.Impl.AdvancedParkingManager, TrafficManager";
        private const string TargetMethodName = "FindParkingSpacePropAtBuilding";

        public static void Apply(Harmony harmony)
        {
            var type = Type.GetType(TargetTypeName, throwOnError: false);
            if (type == null)
            {
                if (Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled)
                    Log.Info(DebugLogCategory.Tmpe, "[TMPE] AdvancedParkingManager not found; skipping FindParkingSpacePropAtBuilding diagnostics patch.");
                return;
            }

            MethodInfo method = AccessTools.Method(type, TargetMethodName);
            if (method == null)
            {
                if (Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled)
                    Log.Info(DebugLogCategory.Tmpe, "[TMPE] FindParkingSpacePropAtBuilding not found; skipping diagnostics patch.");
                return;
            }

            harmony.Patch(
                method,
                postfix: new HarmonyMethod(typeof(TMPE_FindParkingSpacePropAtBuildingDiagnosticsPatch), nameof(Postfix))
            );

            if (Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled)
                Log.Info(DebugLogCategory.Tmpe, "[TMPE] Patched FindParkingSpacePropAtBuilding (diagnostics).");
        }

        private static void Postfix(bool __result, [HarmonyArgument(3)] ushort buildingId)
        {
            if (TMPE_FindParkingSpacePropAtBuildingPatch.ConsumeSuppressDiagnostics())
                return;

            if (__result)
                return;

            if (!Log.IsVerboseEnabled || !Log.IsTmpeDebugEnabled)
                return;

            var context = ParkingRuntimeContext.GetCurrentOrLog("TMPE_FindParkingSpacePropAtBuildingDiagnosticsPatch.Postfix");
            if (context == null)
                return;

            if (!IsSupportedParkingLot(context, buildingId))
                return;

            string buildingName = "NONE";
            string prefabName = "UNKNOWN";
            try
            {
                var bm = Singleton<BuildingManager>.instance;
                buildingName = bm.GetBuildingName(buildingId, default(InstanceID));
                if (string.IsNullOrEmpty(buildingName))
                    buildingName = "NONE";

                ref Building building = ref bm.m_buildings.m_buffer[buildingId];
                if (building.Info != null && !string.IsNullOrEmpty(building.Info.name))
                    prefabName = building.Info.name;
            }
            catch
            {
                buildingName = "NAME_LOOKUP_FAILED";
            }

            int totalSpaces;
            int occupiedSpaces;
            bool hasStats = context.GameAccess.TryGetParkingSpaceStats(buildingId, out totalSpaces, out occupiedSpaces);
            string stats = hasStats ? $"spaces={totalSpaces} occupied={occupiedSpaces}" : "spaces=n/a";
            string propStats = TryFormatPropStats(context, buildingId);

            Log.Info(DebugLogCategory.Tmpe,
                "[TMPE] FindParkingSpacePropAtBuilding failed " +
                $"buildingId={buildingId} name={buildingName} prefab={prefabName} {stats} {propStats} " +
                $"isVisitor={ParkingSearchContext.IsVisitor} vehicleId={ParkingSearchContext.VehicleId} " +
                $"citizenId={ParkingSearchContext.CitizenId} source={ParkingSearchContext.Source ?? "NULL"}"
            );
        }

        private static bool IsSupportedParkingLot(ParkingRuntimeContext context, ushort buildingId)
        {
            if (context == null || context.SupportedParkingLotRegistry == null)
                return false;

            if (!context.GameAccess.TryGetBuildingInfo(buildingId, out var info))
                return false;

            var key = ParkingLotPrefabKeyFactory.CreateKey(info);
            return context.SupportedParkingLotRegistry.Contains(key);
        }

        private static string TryFormatPropStats(ParkingRuntimeContext context, ushort buildingId)
        {
            try
            {
                if (context == null || !context.GameAccess.TryGetBuildingInfo(buildingId, out var info) || info == null)
                    return "props=n/a";

                if (info.m_props == null || info.m_props.Length == 0)
                    return "props=0 spaces=0";

                int propsWithSpaces = 0;
                int totalSpaces = 0;

                for (int i = 0; i < info.m_props.Length; i++)
                {
                    var prop = info.m_props[i];
                    if (prop == null) continue;

                    PropInfo propInfo = prop.m_finalProp;
                    if (propInfo == null) continue;

                    if (propInfo.m_parkingSpaces == null || propInfo.m_parkingSpaces.Length == 0)
                        continue;

                    propsWithSpaces++;
                    totalSpaces += propInfo.m_parkingSpaces.Length;
                }

                return $"props={propsWithSpaces} propSpaces={totalSpaces}";
            }
            catch
            {
                return "props=ERR";
            }
        }
    }
}
