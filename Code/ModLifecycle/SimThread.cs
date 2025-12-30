using System;
using System.Reflection;
using System.Threading;
using ColossalFramework;

namespace PickyParking.ModLifecycle
{
    
    
    
    public static class SimThread
    {
        private static FieldInfo _simulationThreadField;
        private static bool _simulationThreadFieldChecked;
        private static Thread _simulationThread;

        public static void Dispatch(Action action)
        {
            if (action == null) return;
            Singleton<SimulationManager>.instance.AddAction(action);
        }

        public static bool IsSimulationThread()
        {
            Thread simulationThread = TryGetSimulationThread();
            if (simulationThread == null)
                return true;

            return Thread.CurrentThread == simulationThread;
        }

        private static Thread TryGetSimulationThread()
        {
            if (_simulationThread != null)
                return _simulationThread;

            var instance = Singleton<SimulationManager>.instance;
            if (instance == null)
                return null;

            if (!_simulationThreadFieldChecked)
            {
                _simulationThreadField = typeof(SimulationManager)
                    .GetField("m_simulationThread", BindingFlags.Instance | BindingFlags.NonPublic);
                _simulationThreadFieldChecked = true;
            }

            if (_simulationThreadField == null)
                return null;

            _simulationThread = _simulationThreadField.GetValue(instance) as Thread;
            return _simulationThread;
        }
    }
}

