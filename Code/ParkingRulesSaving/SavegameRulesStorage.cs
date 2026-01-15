using System;
using System.Collections.Generic;
using System.IO;
using ColossalFramework;
using ICities;
using PickyParking.Logging;
using PickyParking.Features.ParkingRules;
using PickyParking.Settings;

namespace PickyParking.ParkingRulesSaving
{
    
    
    public sealed class SavegameRulesStorage
    {
        
        public const string DataId = "PickyParking_Rules";

        private const int CurrentVersion = 2;

        public void Save(ParkingRulesConfigRegistry repository, ISerializableData serializableDataManager)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write(CurrentVersion);

                        var entries = new List<KeyValuePair<ushort, ParkingRulesConfigDefinition>>(repository.Enumerate());
                        writer.Write(entries.Count);

                        foreach (KeyValuePair<ushort, ParkingRulesConfigDefinition> kv in entries)
                        {
                            writer.Write(kv.Key);
                            kv.Value.Write(writer);
                        }
                    }

                    serializableDataManager.SaveData(DataId, stream.ToArray());
                }
            }
            catch (Exception ex)
            {
                Log.Warn(DebugLogCategory.None, "[Persistence] Failed to save rules: " + ex);
            }
        }

        public void LoadInto(ParkingRulesConfigRegistry repository, ISerializableData serializableDataManager)
        {
            try
            {
                byte[] data = serializableDataManager.LoadData(DataId);
                LoadIntoFromBytes(repository, data);
            }
            catch (Exception ex)
            {
                Log.Warn(DebugLogCategory.None, "[Persistence] Failed to load rules: " + ex);
            }
        }

        
        
        
        
        public void LoadIntoFromBytes(ParkingRulesConfigRegistry repository, byte[] data)
        {
            try
            {
                if (data == null || data.Length == 0)
                {
                    return;
                }

                int normalizedCount = 0;

                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                {
                    int version = reader.ReadInt32();

                    repository.Clear();

                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        ushort buildingId = reader.ReadUInt16();

                        ParkingRulesConfigDefinition rule;
                        switch (version)
                        {
                            case 1:
                                rule = ParkingRulesConfigDefinition.ReadV1(reader);
                                break;

                            case 2:
                                rule = ParkingRulesConfigDefinition.ReadV2(reader);
                                break;

                            default:
                                Log.Warn(DebugLogCategory.None, "[Persistence] Unknown rules version: " + version + " (skipping load)");
                                return;
                        }

                        rule = ParkingRulesLimits.ClampRule(rule, out bool normalized);
                        if (normalized)
                            normalizedCount++;
                        repository.Set(buildingId, rule);
                    }
                }

                PruneInvalidEntries(repository);

                if (Log.IsVerboseEnabled && Log.IsRuleUiDebugEnabled)
                    Log.Info(DebugLogCategory.RuleUi, "[Persistence] Normalized rules: " + normalizedCount);
            }
            catch (Exception ex)
            {
                Log.Warn(DebugLogCategory.None, "[Persistence] Failed to load rules from bytes: " + ex);
            }
        }

        public void PruneInvalidEntries(ParkingRulesConfigRegistry repository)
        {
            if (repository == null) return;

            var toRemove = new List<ushort>();

            try
            {
                BuildingManager bm = Singleton<BuildingManager>.instance;
                int maxSize = (int)bm.m_buildings.m_size;

                foreach (var kvp in repository.Enumerate())
                {
                    ushort buildingId = kvp.Key;
                    if (buildingId == 0 || buildingId >= maxSize)
                    {
                        toRemove.Add(buildingId);
                        continue;
                    }

                    ref Building b = ref bm.m_buildings.m_buffer[buildingId];
                    if ((b.m_flags & Building.Flags.Created) == 0 || (b.m_flags & Building.Flags.Deleted) != 0)
                        toRemove.Add(buildingId);
                }
            }
            catch (Exception ex)
            {
                Log.Warn(DebugLogCategory.None, "[Persistence] Failed to prune invalid rule entries: " + ex);
                return;
            }

            for (int i = 0; i < toRemove.Count; i++)
                repository.Remove(toRemove[i]);

            if (Log.IsVerboseEnabled && Log.IsRuleUiDebugEnabled)
                Log.Info(DebugLogCategory.RuleUi, "[Persistence] Pruned invalid rules: " + toRemove.Count);
        }

    }
}




