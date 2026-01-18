using PickyParking.Patching.Diagnostics;
using PickyParking.Patching.Game;
using PickyParking.Patching.TMPE;

namespace PickyParking.Patching
{
    
    
    
    public sealed class PatchSetup
    {
        private readonly HarmonyBootstrap _bootstrap = new HarmonyBootstrap();
        private readonly DiagnosticsPatchSetup _diagnostics = new DiagnosticsPatchSetup();
        private bool _applied;

        public void ApplyAll()
        {
            if (_applied) return;
            _applied = true;

            BuildingManager_ReleaseBuildingPatch.Apply(_bootstrap.Harmony);
            VehicleManager_CreateParkedVehiclePatch.Apply(_bootstrap.Harmony);
            DefaultTool_RenderOverlayPatch.Apply(_bootstrap.Harmony);
            PassengerCarAI_ParkVehicleContextPatch.Apply(_bootstrap.Harmony);
            PassengerCarAI_UpdateParkedVehicleContextPatch.Apply(_bootstrap.Harmony);
            PassengerCarAI_FindParkingSpaceBuildingPatch.Apply(_bootstrap.Harmony);
            PassengerCarAI_FindParkingSpaceBuildingRadiusPatch.Apply(_bootstrap.Harmony);
            
            
            TMPE_FindParkingSpaceForCitizenPatch.Apply(_bootstrap.Harmony);
            TMPE_TryMoveParkedVehiclePatch.Apply(_bootstrap.Harmony);
            TMPE_FindParkingSpacePropAtBuildingPatch.Apply(_bootstrap.Harmony);
            TMPE_VanillaFindParkingSpaceWithoutRestrictionsPatch.Apply(_bootstrap.Harmony);
            TMPE_ParkPassengerCarPatch.Apply(_bootstrap.Harmony);
            TMPE_TrySpawnParkedPassengerCarPatch.Apply(_bootstrap.Harmony);

            _diagnostics.ApplyAll(_bootstrap.Harmony);
            
        }

        public void RemoveAll()
        {
            if (!_applied) return;
            _applied = false;

            _bootstrap.Harmony.UnpatchAll(_bootstrap.Harmony.Id);
        }

    }
}

