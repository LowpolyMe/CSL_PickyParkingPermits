using System;
using System.Collections.Generic;
using PickyParking.Features.ParkingLotPrefabs;

namespace PickyParking.Settings
{
    
    
    
    
    [Serializable]
    public sealed class ModSettings
    {
        public List<PrefabKey> SupportedParkingLotPrefabs { get; set; }
        public bool EnableVerboseLogging { get; set; }
        public DebugLogCategory EnabledDebugLogCategories { get; set; }
        public bool DisableTMPECandidateBlocking { get; set; }
        public bool DisableClearKnownParkingOnDenied { get; set; }
        public bool DisableParkingEnforcement { get; set; }
        public ushort DebugBuildingId { get; set; }
        public float ResidentsRadiusHue { get; set; }
        public float WorkSchoolRadiusHue { get; set; }
        public bool EnableParkingRuleSweeps { get; set; }
        public bool EnableStuckParkedVehicleFix { get; set; }
        public int ReevaluationMaxEvaluationsPerTick { get; set; }
        public int ReevaluationMaxRelocationsPerTick { get; set; }

        public ModSettings()
        {
            SupportedParkingLotPrefabs = new List<PrefabKey>();
            EnableVerboseLogging = false;
            EnabledDebugLogCategories = DebugLogCategory.None;
            DisableTMPECandidateBlocking = false;
            DisableClearKnownParkingOnDenied = false;
            DisableParkingEnforcement = false;
            DebugBuildingId = 0;
            ResidentsRadiusHue = 0.35f;
            WorkSchoolRadiusHue = 0.1f;
            EnableParkingRuleSweeps = true;
            EnableStuckParkedVehicleFix = true;
            ReevaluationMaxEvaluationsPerTick = 256;
            ReevaluationMaxRelocationsPerTick = 16;
        }

        internal const string FileName = "PickyParking.xml";

        public void CopyFrom(ModSettings reloaded)
        {
            throw new NotImplementedException();
            //TODO:  implement CopyFrom to assign every field to mutate the existing Current instead of replacing it.
        }

        public bool IsDebugLogCategoryEnabled(DebugLogCategory category)
        {
            if (!EnableVerboseLogging)
                return false;

            return (EnabledDebugLogCategories & category) != 0;
        }

        public bool IsLotInspectionDebugEnabledForBuilding(ushort buildingId)
        {
            if (!IsDebugLogCategoryEnabled(DebugLogCategory.LotInspection))
                return false;

            if (DebugBuildingId == 0)
                return false;

            return buildingId == DebugBuildingId;
        }
    }
}
