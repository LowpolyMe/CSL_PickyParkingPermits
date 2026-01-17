using System;

namespace PickyParking.Features.ParkingPolicing
{
    
    
    
    public sealed class ParkingContextScope : IDisposable
    {
        private bool _disposed;

        public ParkingContextScope(ushort vehicleId, uint citizenId, string source)
        {
            ParkingSearchContext.Push(vehicleId, citizenId, source);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            ParkingSearchContext.Pop();
        }
    }
}

