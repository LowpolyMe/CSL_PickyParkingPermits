using System;
using PickyParking.Logging;
using PickyParking.ModLifecycle;
using PickyParking.Settings;
using PickyParking.ParkingRulesSaving;
using PickyParking.GameAdapters;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.Features.ParkingRules;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Features.Debug;

namespace PickyParking.ModEntry
{
    public sealed class ModRuntime : IDisposable
    {
        public FeatureGate FeatureGate { get; private set; }
        public SupportedParkingLotRegistry SupportedParkingLotRegistry { get; private set; }
        public ParkingRulesConfigRegistry ParkingRulesConfigRegistry { get; private set; }
        public ModSettingsController SettingsController { get; private set; }
        public GameAccess GameAccess { get; private set; }
        public TmpeIntegration TmpeIntegration { get; private set; }
        public ParkingRuleEvaluator ParkingRuleEvaluator { get; private set; }
        public ParkingPermissionEvaluator ParkingPermissionEvaluator { get; private set; }
        public ParkedVehicleReevaluation ParkedVehicleReevaluation { get; private set; }
        public ParkingRulePreviewState ParkingRulePreviewState { get; private set; }
        public DebugHotkeyController DebugHotkeyController { get; private set; }
        public ParkingRulesConfigEditor ParkingRulesConfigEditor { get; private set; }

        private bool _disposed;
        
        public static ModRuntime Current { get; private set; }

        public static void ApplyLoggingSettings(ModSettings settings)
        {
            if (settings == null) return;

            Log.SetVerboseEnabled(settings.EnableVerboseLogging);
            Log.SetEnabledDebugCategories(settings.EnabledDebugLogCategories);
            ParkingStatsTicker.SetEnabled(settings.EnableVerboseLogging);

            ParkingSearchContext.EnableEpisodeLogs = settings.IsDebugLogCategoryEnabled(DebugLogCategory.DecisionPipeline);
            ParkingSearchContext.LogMinCandidates = settings.EnableVerboseLogging
                ? 1
                : ParkingSearchContext.DefaultLogMinCandidates;
            ParkingSearchContext.LogMinDurationMs = settings.EnableVerboseLogging
                ? 0
                : ParkingSearchContext.DefaultLogMinDurationMs;
            ParkingDebugSettings.DisableTMPECandidateBlocking = settings.DisableTMPECandidateBlocking;
            ParkingDebugSettings.DisableClearKnownParkingOnDenied = settings.DisableClearKnownParkingOnDenied;
            ParkingDebugSettings.DisableParkingEnforcement = settings.DisableParkingEnforcement;
            ParkingDebugSettings.BuildingDebugId = settings.DebugBuildingId;
            ParkingDebugSettings.EnableLotInspectionLogs = settings.IsDebugLogCategoryEnabled(DebugLogCategory.LotInspection);

            if (Log.IsVerboseEnabled && Log.IsRuleUiDebugEnabled)
                Log.Info(DebugLogCategory.RuleUi, "[Settings] Patch settings applied.");
        }
        
        public static ModRuntime Create(ModSettings settings, ModSettingsController settingsController, LevelContext levelContext = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settingsController == null) throw new ArgumentNullException(nameof(settingsController));
            
            var featureGate = new FeatureGate();
            var registry = new SupportedParkingLotRegistry(settings.SupportedParkingLotPrefabs);
            var rulesRepo = new ParkingRulesConfigRegistry();
            
            if (levelContext?.RulesBytes != null && levelContext.RulesBytes.Length > 0)
            {
                try
                {
                    var storage = new SavegameRulesStorage();
                    storage.LoadIntoFromBytes(rulesRepo, levelContext.RulesBytes);
                    if (Log.IsVerboseEnabled && Log.IsRuleUiDebugEnabled)
                        Log.Info(DebugLogCategory.RuleUi, $"[Persistence] Applied savegame building rules ({levelContext.RulesBytes.Length} bytes).");
                }
                catch (Exception ex)
                {
                    Log.Warn(DebugLogCategory.None, "[Persistence] Failed to apply savegame building rules: " + ex);
                }
                finally
                {
                    levelContext.Clear();
                }
            }

            var gameAccess = new GameAccess();

            var policy = new ParkingRuleEvaluator();
            var evaluator = new ParkingPermissionEvaluator(
                featureGate,
                rulesRepo,
                gameAccess,
                policy);

            var tmpe = new TmpeIntegration(featureGate, evaluator);
            var reevaluation = new ParkedVehicleReevaluation(featureGate, rulesRepo, evaluator, gameAccess, registry, tmpe, settingsController);
            var previewState = new ParkingRulePreviewState();
            var debugHotkeys = new DebugHotkeyController(
                gameAccess,
                registry,
                settingsController,
                rulesRepo,
                reevaluation);
            var rulesController = new ParkingRulesConfigEditor(
                rulesRepo,
                previewState,
                reevaluation);

            var dependencies = new RuntimeDependencies(
                featureGate,
                registry,
                rulesRepo,
                settingsController,
                gameAccess,
                tmpe,
                policy,
                evaluator,
                reevaluation,
                previewState,
                debugHotkeys,
                rulesController);

            return new ModRuntime(dependencies);
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
        
        private ModRuntime(RuntimeDependencies dependencies)
        {
            FeatureGate = dependencies.FeatureGate;
            SupportedParkingLotRegistry = dependencies.SupportedParkingLotRegistry;
            ParkingRulesConfigRegistry = dependencies.ParkingRulesRegistry;
            SettingsController = dependencies.SettingsController;
            GameAccess = dependencies.GameAccess;
            TmpeIntegration = dependencies.TmpeIntegration;
            ParkingRuleEvaluator = dependencies.ParkingRuleEvaluator;
            ParkingPermissionEvaluator = dependencies.ParkingPermissionEvaluator;
            ParkedVehicleReevaluation = dependencies.ParkedVehicleReevaluation;
            ParkingRulePreviewState = dependencies.ParkingRulePreviewState;
            DebugHotkeyController = dependencies.DebugHotkeyController;
            ParkingRulesConfigEditor = dependencies.ParkingRulesConfigEditor;
        }
        
        private sealed class RuntimeDependencies
        {
            public RuntimeDependencies(
                FeatureGate featureGate,
                SupportedParkingLotRegistry supportedParkingLotRegistry,
                ParkingRulesConfigRegistry parkingRulesRegistry,
                ModSettingsController settingsController,
                GameAccess gameAccess,
                TmpeIntegration tmpeIntegration,
                ParkingRuleEvaluator parkingRuleEvaluator,
                ParkingPermissionEvaluator parkingPermissionEvaluator,
                ParkedVehicleReevaluation parkedVehicleReevaluation,
                ParkingRulePreviewState parkingRulePreviewState,
                DebugHotkeyController debugHotkeyController,
                ParkingRulesConfigEditor parkingRulesConfigEditor)
            {
                FeatureGate = featureGate ?? throw new ArgumentNullException(nameof(featureGate));
                SupportedParkingLotRegistry = supportedParkingLotRegistry ?? throw new ArgumentNullException(nameof(supportedParkingLotRegistry));
                ParkingRulesRegistry = parkingRulesRegistry ?? throw new ArgumentNullException(nameof(parkingRulesRegistry));
                SettingsController = settingsController ?? throw new ArgumentNullException(nameof(settingsController));
                GameAccess = gameAccess ?? throw new ArgumentNullException(nameof(gameAccess));
                TmpeIntegration = tmpeIntegration ?? throw new ArgumentNullException(nameof(tmpeIntegration));
                ParkingRuleEvaluator = parkingRuleEvaluator ?? throw new ArgumentNullException(nameof(parkingRuleEvaluator));
                ParkingPermissionEvaluator = parkingPermissionEvaluator ?? throw new ArgumentNullException(nameof(parkingPermissionEvaluator));
                ParkedVehicleReevaluation = parkedVehicleReevaluation ?? throw new ArgumentNullException(nameof(parkedVehicleReevaluation));
                ParkingRulePreviewState = parkingRulePreviewState ?? throw new ArgumentNullException(nameof(parkingRulePreviewState));
                DebugHotkeyController = debugHotkeyController ?? throw new ArgumentNullException(nameof(debugHotkeyController));
                ParkingRulesConfigEditor = parkingRulesConfigEditor ?? throw new ArgumentNullException(nameof(parkingRulesConfigEditor));
            }

            public FeatureGate FeatureGate { get; }
            public SupportedParkingLotRegistry SupportedParkingLotRegistry { get; }
            public ParkingRulesConfigRegistry ParkingRulesRegistry { get; }
            public ModSettingsController SettingsController { get; }
            public GameAccess GameAccess { get; }
            public TmpeIntegration TmpeIntegration { get; }
            public ParkingRuleEvaluator ParkingRuleEvaluator { get; }
            public ParkingPermissionEvaluator ParkingPermissionEvaluator { get; }
            public ParkedVehicleReevaluation ParkedVehicleReevaluation { get; }
            public ParkingRulePreviewState ParkingRulePreviewState { get; }
            public DebugHotkeyController DebugHotkeyController { get; }
            public ParkingRulesConfigEditor ParkingRulesConfigEditor { get; }
        }
    }
}
