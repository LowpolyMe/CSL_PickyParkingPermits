using PickyParking.Features.Debug;
using PickyParking.Features.ParkingPolicing.Runtime;
using PickyParking.ModLifecycle.BackendSelection;
using PickyParking.Settings;

namespace PickyParking.Features.ParkingPolicing
{
    internal static class VanillaCandidateSearchFilter
    {
        public static bool ShouldRunOriginal(ushort buildingId, ref bool result)
        {
            ParkingRuntimeContext context = ParkingRuntimeContext.Current;
            if (context == null)
                return true;

            ParkingBackendState backendState = context.ParkingBackendState;
            if (backendState == null)
                return true;

            if (backendState.ActiveBackend != ParkingBackendKind.Vanilla)
                return true;

            if (ParkingDebugSettings.DisableParkingEnforcement)
                return true;

            bool denied;
            if (!ParkingCandidateBlocker.TryGetCandidateDecision(buildingId, out denied))
                return true;

            if (!denied)
                return true;

            result = false;
            return false;
        }
    }
}
