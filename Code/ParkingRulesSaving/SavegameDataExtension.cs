using ICities;
using PickyParking.Features.Debug;
using PickyParking.Logging;
using PickyParking.ModLifecycle;
using PickyParking.ModEntry;
using PickyParking.Settings;

namespace PickyParking.ParkingRulesSaving
{
    
    
    
    
    public sealed class SavegameDataExtension : SerializableDataExtensionBase
    {
        private readonly SavegameRulesStorage _storage = new SavegameRulesStorage();

        public override void OnLoadData()
        {
            var data = serializableDataManager.LoadData(SavegameRulesStorage.DataId);
            LevelBootstrap.Context.RulesBytes = data;

            if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
            {
                int byteCount = data == null ? 0 : data.Length;
                Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "RulesBytesLoaded", "byteCount=" + byteCount);
            }
        }

        public override void OnSaveData()
        {
            var runtime = ModRuntime.Current;
            if (runtime == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                {
                    Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "RulesSaveSkippedRuntimeMissing");
                }
                return;
            }

            _storage.Save(runtime.ParkingRulesConfigRegistry, serializableDataManager);
        }
    }
}


