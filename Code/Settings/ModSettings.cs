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
        public bool EnableDebugRuleUiLogs { get; set; }
        public bool EnableDebugLotInspectionLogs { get; set; }
        public bool EnableDebugDecisionPipelineLogs { get; set; }
        public bool EnableDebugEnforcementLogs { get; set; }
        public bool EnableDebugTmpeLogs { get; set; }
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
            EnableDebugRuleUiLogs = false;
            EnableDebugLotInspectionLogs = false;
            EnableDebugDecisionPipelineLogs = false;
            EnableDebugEnforcementLogs = false;
            EnableDebugTmpeLogs = false;
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
    }
}
