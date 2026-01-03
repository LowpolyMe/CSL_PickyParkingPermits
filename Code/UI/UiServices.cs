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
        private readonly ModRuntime _runtime;
        private readonly ModSettingsController _settingsController;

        public UiServices(ModRuntime runtime, ModSettingsController settingsController)
        {
            _runtime = runtime;
            _settingsController = settingsController ?? (runtime != null ? runtime.SettingsController : null);
        }

        public bool IsFeatureActive => _runtime != null && _runtime.FeatureGate.IsActive;
        public GameAccess GameAccess => _runtime != null ? _runtime.GameAccess : null;
        public ParkingRulesConfigEditor ParkingRulesConfigEditor => _runtime != null ? _runtime.ParkingRulesConfigEditor : null;
        public ParkingRulesConfigRegistry ParkingRulesConfigRegistry => _runtime != null ? _runtime.ParkingRulesConfigRegistry : null;
        public ParkingRulePreviewState ParkingRulePreviewState => _runtime != null ? _runtime.ParkingRulePreviewState : null;
        public SupportedParkingLotRegistry SupportedParkingLotRegistry => _runtime != null ? _runtime.SupportedParkingLotRegistry : null;
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
