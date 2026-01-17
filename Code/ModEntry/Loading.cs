using System;
using System.Reflection;
using ICities;
using PickyParking.Logging;
using PickyParking.ModLifecycle;
using PickyParking.Patching;
using PickyParking.ParkingRulesSaving;
using PickyParking.Settings;
using PickyParking.UI;
using PickyParking.UI.BuildingOptionsPanel;
using PickyParking.UI.BuildingOptionsPanel.ParkingRulesPanel;
using PickyParking.UI.BuildingOptionsPanel.OverlayRendering;
using PickyParking.Features.Debug;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Patching.Diagnostics.TMPE;
using PickyParking.Patching.TMPE;
using UnityEngine;

namespace PickyParking.ModEntry
{
//TODO On unload, clear static caches (VehicleDespawnReasonCache, stats counters, etc.) on the correct thread.
    public sealed class Loading : LoadingExtensionBase
    {
        private PatchSetup _patches;
        private GameObject _runtimeObject;
        private static bool _assemblyVersionLogged;
        private int _unloadSequence;
        private UiServices _uiServices;

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            Log.InitializeMainThread();
            LogAssemblyVersionLoadedOnce();
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            Unload(clearLevelContext: false);

            var settingsStorage = new ModSettingsStorage();
            var settingsController = ModSettingsController.Load(settingsStorage);
            ModSettings settings = settingsController.Current;
            var runtime = ModRuntime.Create(settings, settingsController, LevelBootstrap.Context);
            ModRuntime.SetCurrent(runtime);
            ModRuntime.ApplyLoggingSettings(settings);
            if (ModRuntime.Current != null)
            {
                ModRuntime.Current.TmpeIntegration.RefreshState();
            }

            _patches = new PatchSetup();
            _patches.ApplyAll();

            _uiServices = new UiServices(() => runtime, settingsController);
            OverlayRenderer.SetServices(_uiServices);
            CreateRuntimeObjects(mode);

            Log.MarkDebugPanelReady();
            Log.Info(DebugLogCategory.None, "[Parking] Loaded.");
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();

            Unload(clearLevelContext: true);
            Log.Info(DebugLogCategory.None, "[Parking] Unloaded.");
            Log.MarkDebugPanelNotReady();
        }

        public override void OnReleased()
        {
            base.OnReleased();
            Unload(clearLevelContext: true);
            Log.MarkDebugPanelNotReady();
        }

        private void CreateRuntimeObjects(LoadMode mode)
        {
            
            if (!IsGameplayMode(mode))
                return;

            DestroyRuntimeObjects();

            _runtimeObject = new GameObject("PickyParking.Runtime");
            UnityEngine.Object.DontDestroyOnLoad(_runtimeObject);

            _runtimeObject.AddComponent<Logging.LogFlushPump>();
            _runtimeObject.AddComponent<DebugHotkeyListener>();
            _runtimeObject.AddComponent<ParkingStatsTicker>();
            var attachPanel = _runtimeObject.AddComponent<AttachPanelToBuildingInfo>();
            attachPanel.Initialize(_uiServices);
        }

        private void DestroyRuntimeObjects()
        {
            if (_runtimeObject == null)
                return;

            UnityEngine.Object.Destroy(_runtimeObject);
            _runtimeObject = null;
        }

        private void Unload(bool clearLevelContext)
        {
            DestroyRuntimeObjects();
            OverlayRenderer.SetServices(null);
            _uiServices = null;
            ParkingRulesIconAtlas.ClearCache();
            ParkingSearchContext.ClearAll();
            ParkingStatsCounter.ResetAll();
            VehicleDespawnReasonCache.ClearAll();
            ParkingPathModeTracker.ClearAll();
            TMPE_FindParkingSpaceForCitizenDiagnosticsPatch.ClearAll();
            int unloadId = ++_unloadSequence;
            Log.Info(DebugLogCategory.None, "[Runtime] Unload started id=" + unloadId);
            ParkingSearchContextPatchHandler.ClearCaches();
            if (clearLevelContext)
            {
                LevelBootstrap.Context.Clear();
            }

            if (_patches != null)
            {
                _patches.RemoveAll();
                _patches = null;
            }

            ModRuntime.ClearCurrent();
            Log.SetVerboseEnabled(false);
            Log.SetEnabledDebugCategories(DebugLogCategory.None);
            
            //TODO reset all ParkingDebugSettings.* fields
            ParkingSearchContext.EnableEpisodeLogs = false;
            ParkingSearchContext.LogMinCandidates = ParkingSearchContext.DefaultLogMinCandidates;
            ParkingSearchContext.LogMinDurationMs = ParkingSearchContext.DefaultLogMinDurationMs;
            ParkingDebugSettings.EnableLotInspectionLogs = false;
            ParkingDebugSettings.DisableTMPECandidateBlocking = false;
            ParkingDebugSettings.DisableClearKnownParkingOnDenied = false;
            ParkingDebugSettings.BuildingDebugId = 0;

            SimThread.Dispatch(() =>
            {
                if (_unloadSequence != unloadId)
                {
                    Log.Info(DebugLogCategory.None, "[Runtime] Sim-thread cleanup skipped (newer unload id=" + _unloadSequence + ")");
                    return;
                }

                if (ModRuntime.Current != null)
                {
                    Log.Info(DebugLogCategory.None, "[Runtime] Sim-thread cleanup skipped (new runtime active)");
                    return;
                }

                ParkingSearchContext.ClearAll();
                ParkingCandidateBlocker.ClearThreadStatic();
                Log.Info(DebugLogCategory.None, "[Runtime] Sim-thread cleanup complete id=" + unloadId);
            });
        }

        private static bool IsGameplayMode(LoadMode mode)
        {
            switch (mode)
            {
                case LoadMode.NewGame:
                case LoadMode.LoadGame:
                case LoadMode.NewGameFromScenario:
                case LoadMode.LoadScenario:
                    return true;

                default:
                    return false;
            }
        }

        private static void LogAssemblyVersionLoaded()
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            string currentName = currentAssembly.GetName().Name;
            Version currentVersion = currentAssembly.GetName().Version;

            Version previousVersion = null;
            string previousVersions = string.Empty;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                AssemblyName name = assembly.GetName();
                if (!string.Equals(name.Name, currentName, StringComparison.Ordinal))
                    continue;

                Version version = name.Version;
                if (version == null || version.Equals(currentVersion))
                    continue;

                if (previousVersion == null || version > previousVersion)
                    previousVersion = version;

                if (previousVersions.Length > 0)
                    previousVersions += ", ";
                previousVersions += version;
            }

            string previousText = previousVersion != null ? previousVersion.ToString() : "<none>";
            if (previousVersions.Length == 0)
                previousVersions = "<none>";

            Log.Info(DebugLogCategory.None,
                "Assembly loaded. Current=" + currentVersion +
                " Previous=" + previousText +
                " AllPrevious=[" + previousVersions + "]"
            );
        }

        private static void LogAssemblyVersionLoadedOnce()
        {
            if (_assemblyVersionLogged)
                return;

            _assemblyVersionLogged = true;
            LogAssemblyVersionLoaded();
        }
    }
}
