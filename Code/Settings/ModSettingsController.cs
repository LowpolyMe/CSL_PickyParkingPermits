using System;
using PickyParking.Features.Debug;
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
            if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
            {
                string fields = reason == null ? null : "reason=" + reason;
                Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "SettingsSaved", fields);
            }
        }

        public void Reload(string reason = null)
        {
            var reloaded = _storage.LoadOrCreate();
            Current.CopyFrom(reloaded); 
            if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
            {
                string fields = reason == null ? null : "reason=" + reason;
                Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "SettingsReloaded", fields);
            }
        }
    }
}

