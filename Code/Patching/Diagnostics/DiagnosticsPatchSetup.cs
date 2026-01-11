using HarmonyLib;
using PickyParking.Patching.Diagnostics.Game;
using PickyParking.Patching.Diagnostics.TMPE;

namespace PickyParking.Patching.Diagnostics
{
    public sealed class DiagnosticsPatchSetup
    {
        public void ApplyAll(Harmony harmony)
        {
            VehicleManager_CreateParkedVehicleDiagnosticsPatch.Apply(harmony);
            VehicleManager_ReleaseParkedVehicleDiagnosticsPatch.Apply(harmony);
            VehicleManager_ReleaseVehicleDiagnosticsPatch.Apply(harmony);

            TMPE_FindParkingSpaceForCitizenDiagnosticsPatch.Apply(harmony);
            TMPE_FindParkingSpacePropAtBuildingDiagnosticsPatch.Apply(harmony);
            TMPE_FindParkingSpaceRoadSideForVehiclePosDiagnosticsPatch.Apply(harmony);
            TMPE_ParkPassengerCarDiagnosticsPatch.Apply(harmony);
            TMPE_StartPassengerCarPathFindDiagnosticsPatch.Apply(harmony);
            TMPE_UpdateCarPathStateDiagnosticsPatch.Apply(harmony);
        }
    }
}
