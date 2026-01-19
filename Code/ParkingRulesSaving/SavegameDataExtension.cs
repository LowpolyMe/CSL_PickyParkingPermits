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

            if (Log.IsVerboseEnabled && Log.IsRuleUiDebugEnabled)
                Log.Info(DebugLogCategory.RuleUi, $"[Persistence] Loaded rules bytes={(data?.Length ?? 0)}");
        }

        public override void OnSaveData()
        {
            var runtime = ModRuntime.Current;
            if (runtime == null)
            {
                if (Log.IsVerboseEnabled && Log.IsRuleUiDebugEnabled)
                    Log.Info(DebugLogCategory.RuleUi, "[Persistence] ModRuntime.Current is null");
                return;
            }

            _storage.Save(runtime.ParkingRulesConfigRegistry, serializableDataManager);
        }
    }
}


