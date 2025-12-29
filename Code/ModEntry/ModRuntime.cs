using System;
using PickyParking.Infrastructure;
using PickyParking.Settings;
using PickyParking.Infrastructure.Persistence;
using PickyParking.App;
using PickyParking.Domain;
using PickyParking.Infrastructure.Integration;
using PickyParking.Features.ParkingPermits;
using PickyParking.Features.Debug;

namespace PickyParking.ModEntry
{
    
    
    
    
    public sealed class ModRuntime : IDisposable
    {
        public FeatureGate FeatureGate { get; private set; }
        public SupportedParkingLotRegistry SupportedParkingLotRegistry { get; private set; }
        public ParkingRestrictionsConfigRegistry ParkingRestrictionsConfigRegistry { get; private set; }
        public ModSettingsController SettingsController { get; private set; }

        public GameAccess GameAccess { get; private set; }
        public PrefabIdentity PrefabIdentity { get; private set; }
        public TmpeIntegration TmpeIntegration { get; private set; }

        public ParkingPermissionDecider ParkingPermissionDecider { get; private set; }
        public ParkingPermissionEvaluator ParkingPermissionEvaluator { get; private set; }

        public ParkedVehicleReevaluation ParkedVehicleReevaluation { get; private set; }
        public ParkingRulePreviewState ParkingRulePreviewState { get; private set; }
        public DebugHotkeyController DebugHotkeyController { get; private set; }
        public ParkingRestrictionsConfigEditor ParkingRestrictionsConfigEditor { get; private set; }

        private bool _disposed;

        private ModRuntime(
            FeatureGate featureGate,
            SupportedParkingLotRegistry supportedParkingLotRegistry,
            ParkingRestrictionsConfigRegistry parkingRulesRepository,
            ModSettingsController settingsController,
            GameAccess gameAccess,
            PrefabIdentity prefabIdentity,
            TmpeIntegration tmpeIntegration,
            ParkingPermissionDecider parkingPermissionPolicy,
            ParkingPermissionEvaluator parkingPermitEvaluator,
            ParkedVehicleReevaluation parkedVehicleReevaluation,
            ParkingRulePreviewState parkingRulePreviewState,
            DebugHotkeyController debugHotkeyController,
            ParkingRestrictionsConfigEditor parkingPermitsRuleController)
        {
            FeatureGate = featureGate;
            SupportedParkingLotRegistry = supportedParkingLotRegistry;
            ParkingRestrictionsConfigRegistry = parkingRulesRepository;
            SettingsController = settingsController;
            GameAccess = gameAccess;
            PrefabIdentity = prefabIdentity;
            TmpeIntegration = tmpeIntegration;
            ParkingPermissionDecider = parkingPermissionPolicy;
            ParkingPermissionEvaluator = parkingPermitEvaluator;
            ParkedVehicleReevaluation = parkedVehicleReevaluation;
            ParkingRulePreviewState = parkingRulePreviewState;
            DebugHotkeyController = debugHotkeyController;
            ParkingRestrictionsConfigEditor = parkingPermitsRuleController;
        }

        
        
        
        
        public static ModRuntime Current { get; private set; }

        
        
        
        
        public static ModRuntime Create(ModSettings settings, ModSettingsController settingsController, LevelContext levelContext = null)
        {
            var featureGate = new FeatureGate();
            var registry = new SupportedParkingLotRegistry(settings.SupportedParkingLotPrefabs);

            var rulesRepo = new ParkingRestrictionsConfigRegistry();

            
            
            if (levelContext?.RulesBytes != null && levelContext.RulesBytes.Length > 0)
            {
                try
                {
                    var storage = new SavegameRulesStorage();
                    storage.LoadIntoFromBytes(rulesRepo, levelContext.RulesBytes);
                    Log.Info($"[Persistence] Applied savegame building rules ({levelContext.RulesBytes.Length} bytes).");
                }
                catch (Exception ex)
                {
                    Log.Warn("[Persistence] Failed to apply savegame building rules: " + ex);
                }
                finally
                {
                    
                    levelContext.Clear();
                }
            }
            else
            {
                
            }

            var gameAccess = new GameAccess();
            var prefabIdentity = new PrefabIdentity();

            var policy = new ParkingPermissionDecider();
            var evaluator = new ParkingPermissionEvaluator(
                featureGate,
                rulesRepo,
                gameAccess,
                policy);

            var tmpe = new TmpeIntegration(featureGate, evaluator);
            var reevaluation = new ParkedVehicleReevaluation(featureGate, rulesRepo, evaluator, gameAccess, tmpe);
            var previewState = new ParkingRulePreviewState();
            var debugHotkeys = new DebugHotkeyController(
                gameAccess,
                prefabIdentity,
                registry,
                settingsController,
                rulesRepo,
                reevaluation);
            var permitsController = new ParkingRestrictionsConfigEditor(
                rulesRepo,
                previewState,
                reevaluation);

            return new ModRuntime(
                featureGate,
                registry,
                rulesRepo,
                settingsController,
                gameAccess,
                prefabIdentity,
                tmpe,
                policy,
                evaluator,
                reevaluation,
                previewState,
                debugHotkeys,
                permitsController);
        }

        public static void SetCurrent(ModRuntime runtime)
        {
            if (Current != null)
            {
                Current.Dispose();
            }

            Current = runtime;
        }

        public static void ClearCurrent()
        {
            if (Current != null)
            {
                Current.Dispose();
                Current = null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (ParkedVehicleReevaluation != null)
            {
                ParkedVehicleReevaluation.Dispose();
            }
        }
    }
}
