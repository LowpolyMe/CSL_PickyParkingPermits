using System;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.Features.ParkingRules;
using PickyParking.GameAdapters;
using PickyParking.ModEntry;
using PickyParking.Settings;

namespace PickyParking.UI
{
    public sealed class UiServices
    {
        private readonly Func<ModRuntime> _runtimeAccessor;
        private readonly ModSettingsController _settingsController;

        public UiServices(Func<ModRuntime> runtimeAccessor, ModSettingsController settingsController)
        {
            _runtimeAccessor = runtimeAccessor;
            _settingsController = settingsController ?? (runtimeAccessor != null ? runtimeAccessor()?.SettingsController : null);
            Game = new UiGameQueries(_runtimeAccessor);
        }

        public UiServices(ModRuntime runtime, ModSettingsController settingsController)
            : this(runtime != null ? (Func<ModRuntime>)(() => runtime) : null, settingsController)
        {
        }

        private ModRuntime Runtime => _runtimeAccessor != null ? _runtimeAccessor() : null;

        public bool IsFeatureActive => Runtime != null && Runtime.FeatureGate.IsActive;
        public UiGameQueries Game { get; }
        public ParkingRulesConfigEditor ParkingRulesConfigEditor => Runtime != null ? Runtime.ParkingRulesConfigEditor : null;
        public ParkingRulesConfigRegistry ParkingRulesConfigRegistry => Runtime != null ? Runtime.ParkingRulesConfigRegistry : null;
        public ParkingRulePreviewState ParkingRulePreviewState => Runtime != null ? Runtime.ParkingRulePreviewState : null;
        public SupportedParkingLotRegistry SupportedParkingLotRegistry => Runtime != null ? Runtime.SupportedParkingLotRegistry : null;
        public ModSettingsController SettingsController => _settingsController;
        public ModSettings Settings => _settingsController != null ? _settingsController.Current : null;

        public void ReloadSettings(string reason)
        {
            if (_settingsController == null || _settingsController.Current == null)
                return;
            _settingsController.Reload(reason);
        }

        public void ApplyLoggingSettings(ModSettings settings)
        {
            ModRuntime.ApplyLoggingSettings(settings);
        }
    }
}
