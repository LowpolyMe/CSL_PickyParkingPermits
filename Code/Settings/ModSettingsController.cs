using System;
using PickyParking.Logging;

namespace PickyParking.Settings
{
    
    
    
    public sealed class ModSettingsController
    {
        private readonly ModSettingsStorage _storage;

        public ModSettings Current { get; private set; }

        public ModSettingsController(ModSettingsStorage storage, ModSettings current)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (current == null) throw new ArgumentNullException("current");

            _storage = storage;
            Current = current;
        }

        public static ModSettingsController Load(ModSettingsStorage storage)
        {
            if (storage == null) throw new ArgumentNullException("storage");

            ModSettings settings = storage.LoadOrCreate();
            return new ModSettingsController(storage, settings);
        }

        public void Save(string reason = null)
        {
            _storage.Save(Current);
            if (Log.IsVerboseEnabled && Log.IsRuleUiDebugEnabled)
            {
                Log.Info(reason == null
                    ? "[Settings] Saved settings."
                    : "[Settings] Saved settings: " + reason);
            }
        }

        public void Reload(string reason = null)
        {
            Current = _storage.LoadOrCreate();
            if (Log.IsVerboseEnabled && Log.IsRuleUiDebugEnabled)
            {
                Log.Info(reason == null
                    ? "[Settings] Reloaded settings."
                    : "[Settings] Reloaded settings: " + reason);
            }
        }
    }
}

