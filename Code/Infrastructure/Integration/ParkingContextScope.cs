using System;

namespace PickyParking.Infrastructure.Integration
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

