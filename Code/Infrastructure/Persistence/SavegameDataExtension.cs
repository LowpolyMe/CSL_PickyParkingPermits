using ICities;
using PickyParking.Infrastructure;
using PickyParking.ModEntry;

namespace PickyParking.Infrastructure.Persistence
{
    
    
    
    
    public sealed class SavegameDataExtension : SerializableDataExtensionBase
    {
        private readonly SavegameRulesStorage _storage = new SavegameRulesStorage();

        public override void OnLoadData()
        {
            var data = serializableDataManager.LoadData(SavegameRulesStorage.DataId);
            LevelBootstrap.Context.RulesBytes = data;

            Log.Info($"[Persistence] Loaded rules bytes={(data?.Length ?? 0)}");
        }

        public override void OnSaveData()
        {
            var runtime = ModRuntime.Current;
            if (runtime == null)
            {
                Log.Info("[Persistence] ModRuntime.Current is null");
                return;
            }

            _storage.Save(runtime.ParkingRulesConfigRegistry, serializableDataManager);
        }
    }
}


