using System;
using System.Collections.Generic;
using PickyParking.Features.Debug;
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
            if (reloaded == null)
                throw new ArgumentNullException(nameof(reloaded));
            
            if (SupportedParkingLotPrefabs == null)
                SupportedParkingLotPrefabs = new List<PrefabKey>();
            else
                SupportedParkingLotPrefabs.Clear();

            if (reloaded.SupportedParkingLotPrefabs != null && reloaded.SupportedParkingLotPrefabs.Count > 0)
                SupportedParkingLotPrefabs.AddRange(reloaded.SupportedParkingLotPrefabs);
            
            EnableVerboseLogging = reloaded.EnableVerboseLogging;
            EnabledDebugLogCategories = reloaded.EnabledDebugLogCategories;
            DisableTMPECandidateBlocking = reloaded.DisableTMPECandidateBlocking;
            DisableClearKnownParkingOnDenied = reloaded.DisableClearKnownParkingOnDenied;
            DisableParkingEnforcement = reloaded.DisableParkingEnforcement;
            DebugBuildingId = reloaded.DebugBuildingId;
            ResidentsRadiusHue = reloaded.ResidentsRadiusHue;
            WorkSchoolRadiusHue = reloaded.WorkSchoolRadiusHue;
            EnableParkingRuleSweeps = reloaded.EnableParkingRuleSweeps;
            EnableStuckParkedVehicleFix = reloaded.EnableStuckParkedVehicleFix;
            ReevaluationMaxEvaluationsPerTick = reloaded.ReevaluationMaxEvaluationsPerTick;
            ReevaluationMaxRelocationsPerTick = reloaded.ReevaluationMaxRelocationsPerTick;
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
