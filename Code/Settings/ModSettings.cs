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
        public bool EnableDebugParkingSearchEpisodes { get; set; }
        public bool EnableDebugGameAccessLogs { get; set; }
        public bool EnableDebugCandidateBlockerLogs { get; set; }
        public bool EnableDebugCreateParkedVehicleLogs { get; set; }
        public bool EnableDebugBuildingLogs { get; set; }
        public ushort DebugBuildingId { get; set; }
        public bool EnableDebugUiLogs { get; set; }
        public bool EnableDebugTmpeLogs { get; set; }
        public bool EnableDebugPermissionEvaluatorLogs { get; set; }
        public float ResidentsRadiusHue { get; set; }
        public float WorkSchoolRadiusHue { get; set; }

        public ModSettings()
        {
            SupportedParkingLotPrefabs = new List<PrefabKey>();
            EnableVerboseLogging = false;
            EnableDebugParkingSearchEpisodes = false;
            EnableDebugGameAccessLogs = false;
            EnableDebugCandidateBlockerLogs = false;
            EnableDebugCreateParkedVehicleLogs = false;
            EnableDebugBuildingLogs = false;
            DebugBuildingId = 0;
            EnableDebugUiLogs = false;
            EnableDebugTmpeLogs = false;
            EnableDebugPermissionEvaluatorLogs = false;
            ResidentsRadiusHue = 0.35f;
            WorkSchoolRadiusHue = 0.1f;
        }

        internal const string FileName = "PickyParking.xml";
    }
}
