using System;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.Features.ParkingRules;
using PickyParking.GameAdapters;
using PickyParking.ModEntry;
using PickyParking.Settings;
using PickyParking.ModLifecycle.BackendSelection;

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
        private ModSettingsController RuntimeSettingsController => Runtime != null ? Runtime.SettingsController : null;
        public bool HasRuntime => Runtime != null;

        public bool IsFeatureActive => Runtime != null && Runtime.FeatureGate.IsActive;
        public UiGameQueries Game { get; }
        public ParkingRulesConfigEditor ParkingRulesConfigEditor => Runtime != null ? Runtime.ParkingRulesConfigEditor : null;
        public ParkingRulesConfigRegistry ParkingRulesConfigRegistry => Runtime != null ? Runtime.ParkingRulesConfigRegistry : null;
        public ParkingRulePreviewState ParkingRulePreviewState => Runtime != null ? Runtime.ParkingRulePreviewState : null;
        public SupportedParkingLotRegistry SupportedParkingLotRegistry => Runtime != null ? Runtime.SupportedParkingLotRegistry : null;
        public ParkingBackendState ParkingBackendState => Runtime != null ? Runtime.ParkingBackendState : null;
        public ModSettingsController SettingsController => RuntimeSettingsController ?? _settingsController;
        public ModSettings Settings => SettingsController != null ? SettingsController.Current : null;

        public void ReloadSettings(string reason)
        {
            var controller = SettingsController;
            if (controller == null || controller.Current == null)
                return;
            controller.Reload(reason);
        }

        public void ApplyLoggingSettings(ModSettings settings)
        {
            ModRuntime.ApplyLoggingSettings(settings);
        }
    }
}
