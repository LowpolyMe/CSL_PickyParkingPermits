using PickyParking.Features.ParkingPolicing;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.Infrastructure;
using PickyParking.Infrastructure.Integration;
using PickyParking.Infrastructure.Persistence;

namespace PickyParking.Features.ParkingPolicing.Runtime
{
    public sealed class ParkingRuntimeContext
    {
        public FeatureGate FeatureGate { get; private set; }
        public SupportedParkingLotRegistry SupportedParkingLotRegistry { get; private set; }
        public ParkingRulesConfigRegistry ParkingRulesConfigRegistry { get; private set; }
        public GameAccess GameAccess { get; private set; }
        public PrefabIdentity PrefabIdentity { get; private set; }
        public TmpeIntegration TmpeIntegration { get; private set; }
        public ParkingPermissionEvaluator ParkingPermissionEvaluator { get; private set; }
        public ParkedVehicleReevaluation ParkedVehicleReevaluation { get; private set; }

        public static ParkingRuntimeContext Current { get; private set; }

        private static bool _missingLogged;

        public ParkingRuntimeContext(
            FeatureGate featureGate,
            SupportedParkingLotRegistry supportedParkingLotRegistry,
            ParkingRulesConfigRegistry parkingRulesRepository,
            GameAccess gameAccess,
            PrefabIdentity prefabIdentity,
            TmpeIntegration tmpeIntegration,
            ParkingPermissionEvaluator parkingPermitEvaluator,
            ParkedVehicleReevaluation parkedVehicleReevaluation)
        {
            FeatureGate = featureGate;
            SupportedParkingLotRegistry = supportedParkingLotRegistry;
            ParkingRulesConfigRegistry = parkingRulesRepository;
            GameAccess = gameAccess;
            PrefabIdentity = prefabIdentity;
            TmpeIntegration = tmpeIntegration;
            ParkingPermissionEvaluator = parkingPermitEvaluator;
            ParkedVehicleReevaluation = parkedVehicleReevaluation;
        }

        public static void SetCurrent(ParkingRuntimeContext context)
        {
            Current = context;
            _missingLogged = false;
        }

        public static void ClearCurrent()
        {
            Current = null;
        }

        public static ParkingRuntimeContext GetCurrentOrLog(string caller)
        {
            if (Current != null) return Current;
            if (_missingLogged) return null;

            _missingLogged = true;
            Log.Warn("[Runtime] Runtime context missing; caller=" + (caller ?? "UNKNOWN"));
            return null;
        }
    }
}
