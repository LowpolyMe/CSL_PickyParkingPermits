using System;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.Features.ParkingRules;
using PickyParking.GameAdapters;
using PickyParking.ModEntry;
using PickyParking.Settings;
using UnityEngine;

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
        }

        public UiServices(ModRuntime runtime, ModSettingsController settingsController)
        {
            _runtimeAccessor = () => runtime;
            _settingsController = settingsController ?? (runtime != null ? runtime.SettingsController : null);
        }

        private ModRuntime Runtime => _runtimeAccessor != null ? _runtimeAccessor() : null;

        public bool IsFeatureActive => Runtime != null && Runtime.FeatureGate.IsActive;
        public GameAccess GameAccess => Runtime != null ? Runtime.GameAccess : null;
        public ParkingRulesConfigEditor ParkingRulesConfigEditor => Runtime != null ? Runtime.ParkingRulesConfigEditor : null;
        public ParkingRulesConfigRegistry ParkingRulesConfigRegistry => Runtime != null ? Runtime.ParkingRulesConfigRegistry : null;
        public ParkingRulePreviewState ParkingRulePreviewState => Runtime != null ? Runtime.ParkingRulePreviewState : null;
        public SupportedParkingLotRegistry SupportedParkingLotRegistry => Runtime != null ? Runtime.SupportedParkingLotRegistry : null;
        public ModSettingsController SettingsController => _settingsController;
        public ModSettings Settings => _settingsController != null ? _settingsController.Current : null;

        public bool TryGetSelectedBuilding(out ushort buildingId, out BuildingInfo info)
        {
            buildingId = 0;
            info = null;
            var game = GameAccess;
            if (game == null)
                return false;
            return game.TryGetSelectedBuilding(out buildingId, out info);
        }

        public bool TryGetBuildingPosition(ushort buildingId, out Vector3 position)
        {
            position = default(Vector3);
            var game = GameAccess;
            if (game == null)
                return false;
            return game.TryGetBuildingPosition(buildingId, out position);
        }

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
