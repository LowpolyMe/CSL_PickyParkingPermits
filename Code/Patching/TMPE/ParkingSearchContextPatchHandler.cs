using System;
using PickyParking.Features.Debug;
using PickyParking.Logging;
using PickyParking.Features.ParkingPolicing;
using PickyParking.Settings;

namespace PickyParking.Patching.TMPE
{
    internal static class ParkingSearchContextPatchHandler
    {
        public static void ClearCaches()
        {
        }

        public static void BeginFindParkingForCitizen(
            ref CitizenInstance driverInstance,
            ushort vehicleId,
            ref bool state)
        {
            state = false;

            try
            {
                uint citizenId = driverInstance.m_citizen;
                if (Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled && citizenId == 0u && vehicleId != 0)
                {
                    Log.Info(DebugLogCategory.Tmpe,
                        "[TMPE] BeginFindParkingForCitizen: driver citizenId=0 " +
                        $"vehicleId={vehicleId} sourceBuilding={driverInstance.m_sourceBuilding} targetBuilding={driverInstance.m_targetBuilding}"
                    );
                }

                if (citizenId != 0u || vehicleId != 0)
                {
                    ParkingSearchContext.Push(vehicleId, citizenId, "TMPE.FindParkingSpaceForCitizen");
                    state = true;
                }
                else
                {
                    if (Log.IsVerboseEnabled && Log.IsTmpeDebugEnabled)
                        Log.Info(DebugLogCategory.Tmpe, "[TMPE] CitizenId and VehicleId are 0, not pushing context");
                }
            }
            catch (Exception ex)
            {
                Log.AlwaysError("[TMPE] Prefix exception\n" + ex);
            }
        }

        public static Exception EndFindParkingForCitizen(Exception exception, bool state)
        {
            try
            {
                if (state)
                    ParkingSearchContext.Pop();
            }
            catch (Exception ex)
            {
                Log.AlwaysError("[TMPE] Finalizer exception\n" + ex);
            }

            return exception;
        }

        public static void BeginParkPassengerCar(ushort vehicleId, uint driverCitizenId, ref bool state)
        {
            state = false;

            try
            {

                if (ParkingSearchContext.HasCitizenId)
                    return;

                if (driverCitizenId == 0u)
                    return;

                ParkingSearchContext.Push(
                    vehicleId: vehicleId,
                    citizenId: driverCitizenId,
                    source: "TMPE.ParkPassengerCar"
                );

                state = true;
            }
            catch (Exception ex)
            {
                Log.AlwaysError("[TMPE] Prefix exception\n" + ex);
            }
        }

        public static Exception EndParkPassengerCar(Exception exception, bool state)
        {
            try
            {
                if (state)
                    ParkingSearchContext.Pop();
            }
            catch (Exception ex)
            {
                Log.AlwaysError("[TMPE] Finalizer exception\n" + ex);
            }

            return exception;
        }

        public static void BeginTryMoveParkedVehicle(ref VehicleParked parkedVehicle, ref bool state)
        {
            state = false;

            try
            {

                if (ParkingSearchContext.HasCitizenId)
                    return;

                uint ownerCitizenId = parkedVehicle.m_ownerCitizen;
                if (ownerCitizenId == 0u)
                    return;

                ParkingSearchContext.Push(
                    vehicleId: 0,
                    citizenId: ownerCitizenId,
                    source: "TMPE.TryMoveParkedVehicle"
                );

                state = true;
            }
            catch (Exception ex)
            {
                Log.AlwaysError("[TMPE] Prefix exception\n" + ex);
            }
        }

        public static Exception EndTryMoveParkedVehicle(Exception exception, bool state)
        {
            try
            {
                if (state)
                    ParkingSearchContext.Pop();
            }
            catch (Exception ex)
            {
                Log.AlwaysError("[TMPE] Finalizer exception\n" + ex);
            }

            return exception;
        }

        public static void BeginTrySpawnParkedCar(uint citizenId, ref bool state)
        {
            state = false;
            try
            {
                if (citizenId == 0u) return;
                ParkingSearchContext.Push(vehicleId: 0, citizenId: citizenId, source: "TMPE.TrySpawnParkedPassengerCar");
                state = true;
            }
            catch (Exception ex)
            {
                Log.AlwaysError("[TMPE] Prefix exception\n" + ex);
            }
        }

        public static Exception EndTrySpawnParkedCar(Exception exception, bool state)
        {
            try
            {
                if (state)
                    ParkingSearchContext.Pop();
            }
            catch (Exception ex)
            {
                Log.AlwaysError("[TMPE] Finalizer exception\n" + ex);
            }

            return exception;
        }

    }
}
