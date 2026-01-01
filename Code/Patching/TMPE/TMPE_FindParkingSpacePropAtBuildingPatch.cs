using System;
using System.Reflection;
using ColossalFramework;
using HarmonyLib;
using PickyParking.Logging;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Features.ParkingPolicing.Runtime;
using PickyParking.Patching;

namespace PickyParking.Patching.TMPE
{
    
    
    
    
    
    
    
    internal static class TMPE_FindParkingSpacePropAtBuildingPatch
    {
        private const string TargetTypeName = "TrafficManager.Manager.Impl.AdvancedParkingManager, TrafficManager";
        private const string TargetMethodName = "FindParkingSpacePropAtBuilding";

        public static void Apply(Harmony harmony)
        {
            Type type = Type.GetType(TargetTypeName, throwOnError: false);
            if (type == null)
            {
                Log.Info("[TMPE] AdvancedParkingManager not found; skipping FindParkingSpacePropAtBuilding patch.");
                return;
            }

            MethodInfo method = AccessTools.Method(type, TargetMethodName);
            if (method == null)
            {
                Log.Info("[TMPE] FindParkingSpacePropAtBuilding not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(TMPE_FindParkingSpacePropAtBuildingPatch), nameof(Prefix)),
                postfix: new HarmonyMethod(typeof(TMPE_FindParkingSpacePropAtBuildingPatch), nameof(Postfix))
            );

            Log.Info("[TMPE] Patched FindParkingSpacePropAtBuilding (enforcement).");
        }

        private static bool Prefix(ref bool __result, object[] __args, ref bool __state)
        {
            bool callOriginal = ParkingCandidateBlockerPatchHandler.HandleFindParkingSpacePropAtBuildingPrefix(ref __result, __args);
            __state = !callOriginal;
            return callOriginal;
        }

        private static void Postfix(bool __result, object[] __args, bool __state)
        {
            if (__state)
                return;

            if (__result)
                return;

            if (!Log.IsVerboseEnabled || !Log.IsTmpeDebugEnabled)
                return;

            if (__args == null || __args.Length < 4)
                return;

            if (!(__args[3] is ushort))
                return;

            ushort buildingId = (ushort)__args[3];

            var context = ParkingRuntimeContext.GetCurrentOrLog("TMPE_FindParkingSpacePropAtBuildingPatch.Postfix");
            if (context == null)
                return;

            if (!IsSupportedParkingLot(context, buildingId))
                return;

            string buildingName = "NONE";
            try
            {
                buildingName = Singleton<BuildingManager>.instance.GetBuildingName(buildingId, default(InstanceID));
                if (string.IsNullOrEmpty(buildingName))
                    buildingName = "NONE";
            }
            catch
            {
                buildingName = "NAME_LOOKUP_FAILED";
            }

            int totalSpaces;
            int occupiedSpaces;
            bool hasStats = context.GameAccess.TryGetParkingSpaceStats(buildingId, out totalSpaces, out occupiedSpaces);
            string stats = hasStats ? $"spaces={totalSpaces} occupied={occupiedSpaces}" : "spaces=n/a";

            Log.Info(
                "[TMPE] FindParkingSpacePropAtBuilding failed " +
                $"buildingId={buildingId} name={buildingName} {stats} " +
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
    }
}



