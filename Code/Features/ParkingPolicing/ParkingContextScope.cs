using System;

namespace PickyParking.Features.ParkingPolicing
{
    
    
    
    public struct ParkingContextScope : IDisposable
    {
        private readonly bool _active;

        public ParkingContextScope(ushort vehicleId, uint citizenId, string source)
        {
            _active = true;
            ParkingSearchContext.Push(vehicleId, citizenId, source);
        }

        public void Dispose()
        {
            if (_active)
                ParkingSearchContext.Pop();
        }
    }
}

