using PickyParking.Features.ParkingPolicing;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.Logging;
using PickyParking.ModLifecycle;
using PickyParking.GameAdapters;
using PickyParking.Features.ParkingRules;
using PickyParking.ModEntry;

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
        public ParkingPermissionEvaluator ParkingPermissionEvaluator => _runtime.ParkingPermissionEvaluator;
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
            Log.AlwaysWarn("[Runtime] Runtime context missing; caller=" + (caller ?? "UNKNOWN"));
            return null;
        }
    }
}
