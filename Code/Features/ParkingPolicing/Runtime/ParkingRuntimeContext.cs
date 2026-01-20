using PickyParking.Features.ParkingPolicing;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.Features.Debug;
using PickyParking.Logging;
using PickyParking.ModLifecycle;
using PickyParking.GameAdapters;
using PickyParking.Features.ParkingRules;
using PickyParking.ModEntry;
using PickyParking.ModLifecycle.BackendSelection;

namespace PickyParking.Features.ParkingPolicing.Runtime
{
    public sealed class ParkingRuntimeContext
    {
        private readonly ModRuntime _runtime;

        public FeatureGate FeatureGate => _runtime.FeatureGate;
        public SupportedParkingLotRegistry SupportedParkingLotRegistry => _runtime.SupportedParkingLotRegistry;
        public ParkingRulesConfigRegistry ParkingRulesConfigRegistry => _runtime.ParkingRulesConfigRegistry;
        public GameAccess GameAccess => _runtime.GameAccess;
        public TmpeIntegration TmpeIntegration => _runtime.TmpeIntegration;
        public ParkingBackendState ParkingBackendState => _runtime.ParkingBackendState;
        public ParkingPermissionEvaluator ParkingPermissionEvaluator => _runtime.ParkingPermissionEvaluator;
        public ParkingCandidateDecisionPipeline CandidateDecisionPipeline => _runtime.ParkingCandidateDecisionPipeline;
        public ParkedVehicleReevaluation ParkedVehicleReevaluation => _runtime.ParkedVehicleReevaluation;

        public static ParkingRuntimeContext Current
        {
            get
            {
                ModRuntime runtime = ModRuntime.Current;
                if (runtime == null)
                {
                    _currentRuntime = null;
                    _current = null;
                    return null;
                }

                if (!ReferenceEquals(runtime, _currentRuntime))
                {
                    _currentRuntime = runtime;
                    _current = new ParkingRuntimeContext(runtime);
                    _missingLogged = false;
                }

                return _current;
            }
        }

        private static ModRuntime _currentRuntime;
        private static ParkingRuntimeContext _current;
        private static bool _missingLogged;

        private ParkingRuntimeContext(ModRuntime runtime)
        {
            _runtime = runtime;
        }

        public static ParkingRuntimeContext GetCurrentOrLog(string caller)
        {
            var current = Current;
            if (current != null) return current;
            if (_missingLogged) return null;

            _missingLogged = true;
            if (Log.Dev.IsEnabled(DebugLogCategory.Enforcement))
            {
                Log.Dev.Warn(DebugLogCategory.Enforcement, LogPath.Any, "RuntimeContextMissing", "caller=" + (caller ?? "UNKNOWN"));
            }
            return null;
        }
    }
}
