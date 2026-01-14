using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using ColossalFramework.IO;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.Logging;

namespace PickyParking.Settings
{

    public sealed class ModSettingsStorage
    {
        public ModSettings LoadOrCreate()
        {
            string path = GetSettingsPath();
            try
            {
                if (!File.Exists(path))
                {
                    return new ModSettings();
                }

                var serializer = new XmlSerializer(typeof(ModSettings));
                using (var stream = File.OpenRead(path))
                {
                    var settings = (ModSettings)serializer.Deserialize(stream);
                    return NormalizeSettings(settings);
                }
            }
            catch (Exception ex)
            {
                Log.WarnOnce("Settings.LoadOrCreate", $"[Settings] Failed to load settings from {path}; using defaults. {ex.Message}");
                return new ModSettings();
            }
        }

        public void Save(ModSettings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");

            settings = NormalizeSettings(settings);
            string path = GetSettingsPath();
            var serializer = new XmlSerializer(typeof(ModSettings));
            using (var stream = File.Create(path))
            {
                serializer.Serialize(stream, settings);
            }
        }

        private static string GetSettingsPath()
        {
            return Path.Combine(DataLocation.localApplicationData, ModSettings.FileName);
        }

        private static ModSettings NormalizeSettings(ModSettings settings)
        {
            if (settings == null)
                return new ModSettings();

            if (settings.SupportedParkingLotPrefabs == null)
            {
                settings.SupportedParkingLotPrefabs = new List<PrefabKey>();
                if (Log.IsVerboseEnabled && Log.IsRuleUiDebugEnabled)
                    Log.Info("[Settings] Normalized settings: initialized SupportedParkingLotPrefabs.");
            }

            bool normalizedHue = NormalizeHueValues(settings);

            var unique = new HashSet<PrefabKey>();
            var cleaned = new List<PrefabKey>(settings.SupportedParkingLotPrefabs.Count);

            for (int i = 0; i < settings.SupportedParkingLotPrefabs.Count; i++)
            {
                PrefabKey key = settings.SupportedParkingLotPrefabs[i];
                string prefabName = key.PrefabName ?? string.Empty;
                if (prefabName.Length == 0)
                    continue;

                string packageName = key.PackageName ?? string.Empty;
                var normalized = new PrefabKey(packageName, prefabName);

                if (unique.Add(normalized))
                    cleaned.Add(normalized);
            }

            if (cleaned.Count != settings.SupportedParkingLotPrefabs.Count)
            {
                if (Log.IsVerboseEnabled && Log.IsRuleUiDebugEnabled)
                    Log.Info("[Settings] Normalized settings: removed invalid or duplicate prefabs.");
            }

            if (normalizedHue)
            {
                if (Log.IsVerboseEnabled && Log.IsRuleUiDebugEnabled)
                    Log.Info("[Settings] Normalized settings: clamped overlay hue values.");
            }

            bool normalizedReevaluation = NormalizeReevaluationValues(settings);
            if (normalizedReevaluation)
            {
                if (Log.IsVerboseEnabled && Log.IsRuleUiDebugEnabled)
                    Log.Info("[Settings] Normalized settings: clamped reevaluation limits.");
            }

            settings.SupportedParkingLotPrefabs = cleaned;
            return settings;
        }

        public void ResetToDefaults()
        {
            string path = GetSettingsPath();
            if (File.Exists(path))
                File.Delete(path);
            Save(new ModSettings());
        }

        private static bool NormalizeHueValues(ModSettings settings)
        {
            bool changed = false;

            float residentsHue = ClampHue(settings.ResidentsRadiusHue);
            if (!AreEqual(residentsHue, settings.ResidentsRadiusHue))
            {
                settings.ResidentsRadiusHue = residentsHue;
                changed = true;
            }

            float workHue = ClampHue(settings.WorkSchoolRadiusHue);
            if (!AreEqual(workHue, settings.WorkSchoolRadiusHue))
            {
                settings.WorkSchoolRadiusHue = workHue;
                changed = true;
            }

            return changed;
        }

        private static float ClampHue(float value)
        {
            if (float.IsNaN(value))
                return 0f;
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }

        private static bool AreEqual(float a, float b)
            => Math.Abs(a - b) < 0.0001f;

        private static bool NormalizeReevaluationValues(ModSettings settings)
        {
            bool changed = false;

            int evals = ClampInt(settings.ReevaluationMaxEvaluationsPerTick, 1, 4096);
            if (evals != settings.ReevaluationMaxEvaluationsPerTick)
            {
                settings.ReevaluationMaxEvaluationsPerTick = evals;
                changed = true;
            }

            int relocations = ClampInt(settings.ReevaluationMaxRelocationsPerTick, 1, 512);
            if (relocations != settings.ReevaluationMaxRelocationsPerTick)
            {
                settings.ReevaluationMaxRelocationsPerTick = relocations;
                changed = true;
            }

            return changed;
        }

        private static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
