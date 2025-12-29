using System;
using ColossalFramework;

namespace PickyParking.Infrastructure
{
    
    
    
    public static class SimThread
    {
        public static void Dispatch(Action action)
        {
            if (action == null) return;
            Singleton<SimulationManager>.instance.AddAction(action);
        }
    }
}

