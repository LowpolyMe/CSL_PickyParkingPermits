using System;
using System.Collections.Generic;
using PickyParking.Domain;

namespace PickyParking.Settings
{
    
    
    
    
    [Serializable]
    public sealed class ModSettings
    {
        public List<PrefabKey> SupportedParkingLotPrefabs { get; set; }
        public bool EnableVerboseLogging { get; set; }
        public float ResidentsRadiusHue { get; set; }
        public float WorkSchoolRadiusHue { get; set; }

        public ModSettings()
        {
            SupportedParkingLotPrefabs = new List<PrefabKey>();
            EnableVerboseLogging = false;
            ResidentsRadiusHue = 0.35f;
            WorkSchoolRadiusHue = 0.1f;
        }

        internal const string FileName = "PickyParking.xml";
    }
}
