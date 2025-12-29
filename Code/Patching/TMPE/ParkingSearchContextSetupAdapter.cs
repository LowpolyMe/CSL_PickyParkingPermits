using System;
using System.Reflection;
using System.Collections.Generic;
using PickyParking.Infrastructure;
using PickyParking.Infrastructure.Integration;

namespace PickyParking.Patching.TMPE
{
    internal static class ParkingSearchContextSetupAdapter
    {
        private static readonly object _cacheLock = new object();
        private static readonly Dictionary<MethodBase, ParkPassengerCarIndices> _parkPassengerCarCache =
            new Dictionary<MethodBase, ParkPassengerCarIndices>();
        private static readonly Dictionary<MethodBase, int> _tryMoveParkedVehicleCache =
            new Dictionary<MethodBase, int>();

        private struct ParkPassengerCarIndices
        {
            public int VehicleIdIndex;
            public int DriverCitizenIdIndex;
        }

        public static void ClearCaches()
        {
            lock (_cacheLock)
            {
                _parkPassengerCarCache.Clear();
                _tryMoveParkedVehicleCache.Clear();
            }
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
                if (citizenId != 0u || vehicleId != 0)
                {
                    ParkingSearchContext.Push(vehicleId, citizenId, "TMPE.FindParkingSpaceForCitizen");
                    state = true;
                }
                else
                {
                    if (Log.IsVerboseEnabled)
                        Log.Info("[TMPE] CitizenId and VehicleId are 0, not pushing context");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[TMPE] Prefix exception\n" + ex);
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
                Log.Error("[TMPE] Finalizer exception\n" + ex);
            }

            return exception;
        }

        public static void BeginParkPassengerCar(MethodBase originalMethod, object[] args, ref bool state)
        {
            state = false;

            try
            {

                if (ParkingSearchContext.HasCitizenId)
                    return;

                if (originalMethod == null || args == null)
                    return;

                int idxDriverCitizenId;
                int idxVehicleId;
                if (!TryGetParkPassengerCarIndex(originalMethod, out idxVehicleId, out idxDriverCitizenId))
                    return;

                if (idxDriverCitizenId < 0 || idxDriverCitizenId >= args.Length)
                    return;

                if (idxVehicleId < 0 || idxVehicleId >= args.Length)
                    return;

                if (!(args[idxDriverCitizenId] is uint))
                    return;

                if (!(args[idxVehicleId] is ushort))
                    return;

                uint driverCitizenId = (uint)args[idxDriverCitizenId];
                if (driverCitizenId == 0u)
                    return;

                ushort vehicleId = (ushort)args[idxVehicleId];

                ParkingSearchContext.Push(
                    vehicleId: vehicleId,
                    citizenId: driverCitizenId,
                    source: "TMPE.ParkPassengerCar"
                );

                state = true;
            }
            catch (Exception ex)
            {
                Log.Error("[TMPE] Prefix exception\n" + ex);
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
                Log.Error("[TMPE] Finalizer exception\n" + ex);
            }

            return exception;
        }

        public static void BeginTryMoveParkedVehicle(MethodBase originalMethod, object[] args, ref bool state)
        {
            state = false;

            try
            {

                if (ParkingSearchContext.HasCitizenId)
                    return;

                if (originalMethod == null || args == null)
                    return;

                int idxParked;
                if (!TryGetMoveParkedVehicleIndex(originalMethod, out idxParked))
                    return;

                if (idxParked < 0 || idxParked >= args.Length)
                    return;

                if (!(args[idxParked] is VehicleParked))
                    return;

                VehicleParked pv = (VehicleParked)args[idxParked];
                uint ownerCitizenId = pv.m_ownerCitizen;
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
                Log.Error("[TMPE] Prefix exception\n" + ex);
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
                Log.Error("[TMPE] Finalizer exception\n" + ex);
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
                Log.Error("[TMPE] Prefix exception\n" + ex);
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
                Log.Error("[TMPE] Finalizer exception\n" + ex);
            }

            return exception;
        }

        private static bool TryGetParkPassengerCarIndex(MethodBase originalMethod, out int vehicleIdIndex, out int driverCitizenIdIndex)
        {
            vehicleIdIndex = -1;
            driverCitizenIdIndex = -1;
            if (originalMethod == null) return false;

            ParkPassengerCarIndices cached;
            lock (_cacheLock)
            {
                if (_parkPassengerCarCache.TryGetValue(originalMethod, out cached))
                {
                    vehicleIdIndex = cached.VehicleIdIndex;
                    driverCitizenIdIndex = cached.DriverCitizenIdIndex;
                    return vehicleIdIndex >= 0 && driverCitizenIdIndex >= 0;
                }
            }

            ParameterInfo[] ps = originalMethod.GetParameters();

            for (int i = 0; i < ps.Length; i++)
            {
                Type pt = ps[i].ParameterType;


                if (driverCitizenIdIndex < 0 && pt == typeof(uint))
                {
                    string n = ps[i].Name ?? "";
                    if (n.IndexOf("driver", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        n.IndexOf("citizen", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        driverCitizenIdIndex = i;
                    }
                }


                if (vehicleIdIndex < 0 && pt == typeof(ushort))
                {
                    string n = ps[i].Name ?? "";
                    if (n.Equals("vehicleID", StringComparison.OrdinalIgnoreCase) ||
                        n.Equals("vehicleId", StringComparison.OrdinalIgnoreCase))
                    {
                        vehicleIdIndex = i;
                    }
                }
            }


            if (driverCitizenIdIndex < 0)
            {

                for (int i = 0; i < ps.Length; i++)
                {
                    if (ps[i].ParameterType == typeof(uint))
                    {
                        driverCitizenIdIndex = i;
                        break;
                    }
                }
            }

            if (vehicleIdIndex < 0)
            {

                for (int i = 0; i < ps.Length; i++)
                {
                    if (ps[i].ParameterType == typeof(ushort))
                    {
                        vehicleIdIndex = i;
                        break;
                    }
                }
            }

            cached = new ParkPassengerCarIndices
            {
                VehicleIdIndex = vehicleIdIndex,
                DriverCitizenIdIndex = driverCitizenIdIndex
            };

            lock (_cacheLock)
            {
                _parkPassengerCarCache[originalMethod] = cached;
            }

            return vehicleIdIndex >= 0 && driverCitizenIdIndex >= 0;
        }

        private static bool TryGetMoveParkedVehicleIndex(MethodBase originalMethod, out int parkedIndex)
        {
            parkedIndex = -1;
            if (originalMethod == null) return false;

            int cached;
            lock (_cacheLock)
            {
                if (_tryMoveParkedVehicleCache.TryGetValue(originalMethod, out cached))
                {
                    parkedIndex = cached;
                    return parkedIndex >= 0;
                }
            }

            ParameterInfo[] ps = originalMethod.GetParameters();
            for (int i = 0; i < ps.Length; i++)
            {
                Type pt = ps[i].ParameterType;
                if (pt.IsByRef && pt.GetElementType() == typeof(VehicleParked))
                {
                    parkedIndex = i;
                    break;
                }
            }

            lock (_cacheLock)
            {
                _tryMoveParkedVehicleCache[originalMethod] = parkedIndex;
            }

            return parkedIndex >= 0;
        }
    }
}
