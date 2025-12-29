using System;
using System.Reflection;
using ICities;
using PickyParking.Infrastructure;
using PickyParking.Patching;
using PickyParking.Infrastructure.Persistence;
using PickyParking.Infrastructure.Integration;
using PickyParking.Settings;
using PickyParking.UI;
using PickyParking.Features.Debug;
using PickyParking.App;
using UnityEngine;

namespace PickyParking.ModEntry
{
    
    
    
    
    
    public sealed class Loading : LoadingExtensionBase
    {
        private PatchSetup _patches;
        private GameObject _runtimeObject;
        private static bool _assemblyVersionLogged;

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            LogAssemblyVersionLoadedOnce();
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            Unload(clearLevelContext: false);

            var settingsStorage = new ModSettingsStorage();
            var settingsController = ModSettingsController.Load(settingsStorage);
            ModSettings settings = settingsController.Current;
            Log.SetVerboseEnabled(settings.EnableVerboseLogging);
            ParkingSearchContext.EnableEpisodeLogs = settings.EnableVerboseLogging;
            var runtime = ModRuntime.Create(settings, settingsController, LevelBootstrap.Context);
            ModRuntime.SetCurrent(runtime);
            ParkingRuntimeContext.SetCurrent(new ParkingRuntimeContext(
                runtime.FeatureGate,
                runtime.SupportedParkingLotRegistry,
                runtime.ParkingRestrictionsConfigRegistry,
                runtime.GameAccess,
                runtime.PrefabIdentity,
                runtime.TmpeIntegration,
                runtime.ParkingPermissionEvaluator,
                runtime.ParkedVehicleReevaluation));

            if (ModRuntime.Current != null)
            {
                ModRuntime.Current.TmpeIntegration.RefreshState();
            }

            _patches = new PatchSetup();
            _patches.ApplyAll();

            CreateRuntimeObjects(mode);

            Log.Info("[Parking] Loaded.");
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();

            Unload(clearLevelContext: true);
            Log.Info("[Parking] Unloaded.");
        }

        public override void OnReleased()
        {
            base.OnReleased();
            Unload(clearLevelContext: true);
        }

        private void CreateRuntimeObjects(LoadMode mode)
        {
            
            if (!IsGameplayMode(mode))
                return;

            DestroyRuntimeObjects();

            _runtimeObject = new GameObject("PickyParking.Runtime");
            UnityEngine.Object.DontDestroyOnLoad(_runtimeObject);

            _runtimeObject.AddComponent<DebugHotkeyListener>();
            _runtimeObject.AddComponent<AttachPanelToBuildingInfo>();
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
            ParkingSearchContext.ClearAll();
            SimThread.Dispatch(() =>
            {
                ParkingSearchContext.ClearAll();
                ParkingCandidateBlocker.ClearThreadStatic();
                if (Log.IsVerboseEnabled)
                    Log.Info("[Runtime] Sim-thread cleanup complete.");
            });
            ParkingSearchContextSetup.ClearCaches();
            if (clearLevelContext)
            {
                LevelBootstrap.Context.Clear();
            }

            if (_patches != null)
            {
                _patches.RemoveAll();
                _patches = null;
            }

            ParkingRuntimeContext.ClearCurrent();
            ModRuntime.ClearCurrent();
            Log.SetVerboseEnabled(false);
            ParkingSearchContext.EnableEpisodeLogs = false;
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

            Log.Info(
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
