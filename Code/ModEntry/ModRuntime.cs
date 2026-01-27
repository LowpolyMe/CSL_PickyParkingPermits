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
using PickyParking.ModLifecycle.BackendSelection;

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
        public ParkingBackendState ParkingBackendState { get; private set; }
        public ParkingRuleEvaluator ParkingRuleEvaluator { get; private set; }
        public ParkingPermissionEvaluator ParkingPermissionEvaluator { get; private set; }
        public ParkingCandidateDecisionPipeline ParkingCandidateDecisionPipeline { get; private set; }
        public ParkedVehicleReevaluation ParkedVehicleReevaluation { get; private set; }
        public ParkingRulePreviewState ParkingRulePreviewState { get; private set; }
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
            ParkingDebugSettings.EnableLotInspectionLogs = settings.IsDebugLogCategoryEnabled(DebugLogCategory.LotInspection);

            if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
            {
                Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "SettingsApplied");
            }

            if (Current != null && Current.ParkingBackendState != null)
                Current.ParkingBackendState.Refresh();
        }
        
        public static ModRuntime Create(ModSettings settings, ModSettingsController settingsController, LevelContext levelContext = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settingsController == null) throw new ArgumentNullException(nameof(settingsController));
            
            var featureGate = new FeatureGate();
            featureGate.SetActive();
            var registry = new SupportedParkingLotRegistry(settings.SupportedParkingLotPrefabs);
            var rulesRepo = new ParkingRulesConfigRegistry();
            
            if (levelContext?.RulesBytes != null && levelContext.RulesBytes.Length > 0)
            {
                try
                {
                    var storage = new SavegameRulesStorage();
                    storage.LoadIntoFromBytes(rulesRepo, levelContext.RulesBytes);
                    if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                    {
                        Log.Dev.Info(
                            DebugLogCategory.RuleUi,
                            LogPath.Any,
                            "SavegameRulesApplied",
                            "byteCount=" + levelContext.RulesBytes.Length);
                    }
                }
                catch (Exception ex)
                {
                    Log.Player.Warn(DebugLogCategory.RuleUi, LogPath.Any, "SavegameRulesApplyFailed", "error=" + ex);
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
            var backendState = new ParkingBackendState();
            var candidateDecisionPipeline = new ParkingCandidateDecisionPipeline(evaluator);
            var reevaluation = new ParkedVehicleReevaluation(featureGate, rulesRepo, evaluator, gameAccess, registry, tmpe, settingsController, backendState);
            var previewState = new ParkingRulePreviewState();
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
                backendState,
                policy,
                evaluator,
                candidateDecisionPipeline,
                reevaluation,
                previewState,
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
            ParkingBackendState = dependencies.ParkingBackendState;
            ParkingRuleEvaluator = dependencies.ParkingRuleEvaluator;
            ParkingPermissionEvaluator = dependencies.ParkingPermissionEvaluator;
            ParkingCandidateDecisionPipeline = dependencies.ParkingCandidateDecisionPipeline;
            ParkedVehicleReevaluation = dependencies.ParkedVehicleReevaluation;
            ParkingRulePreviewState = dependencies.ParkingRulePreviewState;
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
                ParkingBackendState parkingBackendState,
                ParkingRuleEvaluator parkingRuleEvaluator,
                ParkingPermissionEvaluator parkingPermissionEvaluator,
                ParkingCandidateDecisionPipeline parkingCandidateDecisionPipeline,
                ParkedVehicleReevaluation parkedVehicleReevaluation,
                ParkingRulePreviewState parkingRulePreviewState,
                ParkingRulesConfigEditor parkingRulesConfigEditor)
            {
                FeatureGate = featureGate ?? throw new ArgumentNullException(nameof(featureGate));
                SupportedParkingLotRegistry = supportedParkingLotRegistry ?? throw new ArgumentNullException(nameof(supportedParkingLotRegistry));
                ParkingRulesRegistry = parkingRulesRegistry ?? throw new ArgumentNullException(nameof(parkingRulesRegistry));
                SettingsController = settingsController ?? throw new ArgumentNullException(nameof(settingsController));
                GameAccess = gameAccess ?? throw new ArgumentNullException(nameof(gameAccess));
                TmpeIntegration = tmpeIntegration ?? throw new ArgumentNullException(nameof(tmpeIntegration));
                ParkingBackendState = parkingBackendState ?? throw new ArgumentNullException(nameof(parkingBackendState));
                ParkingRuleEvaluator = parkingRuleEvaluator ?? throw new ArgumentNullException(nameof(parkingRuleEvaluator));
                ParkingPermissionEvaluator = parkingPermissionEvaluator ?? throw new ArgumentNullException(nameof(parkingPermissionEvaluator));
                ParkingCandidateDecisionPipeline = parkingCandidateDecisionPipeline ?? throw new ArgumentNullException(nameof(parkingCandidateDecisionPipeline));
                ParkedVehicleReevaluation = parkedVehicleReevaluation ?? throw new ArgumentNullException(nameof(parkedVehicleReevaluation));
                ParkingRulePreviewState = parkingRulePreviewState ?? throw new ArgumentNullException(nameof(parkingRulePreviewState));
                ParkingRulesConfigEditor = parkingRulesConfigEditor ?? throw new ArgumentNullException(nameof(parkingRulesConfigEditor));
            }

            public FeatureGate FeatureGate { get; }
            public SupportedParkingLotRegistry SupportedParkingLotRegistry { get; }
            public ParkingRulesConfigRegistry ParkingRulesRegistry { get; }
            public ModSettingsController SettingsController { get; }
            public GameAccess GameAccess { get; }
            public TmpeIntegration TmpeIntegration { get; }
            public ParkingBackendState ParkingBackendState { get; }
            public ParkingRuleEvaluator ParkingRuleEvaluator { get; }
            public ParkingPermissionEvaluator ParkingPermissionEvaluator { get; }
            public ParkingCandidateDecisionPipeline ParkingCandidateDecisionPipeline { get; }
            public ParkedVehicleReevaluation ParkedVehicleReevaluation { get; }
            public ParkingRulePreviewState ParkingRulePreviewState { get; }
            public ParkingRulesConfigEditor ParkingRulesConfigEditor { get; }
        }
    }
}
