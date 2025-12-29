using HarmonyLib;

namespace PickyParking.Patching
{
    
    
    
    public sealed class HarmonyBootstrap
    {
        public Harmony Harmony { get; }

        public HarmonyBootstrap()
        {
            const string harmonyId = "com.lowpolyme.PickyParking";
            Harmony = new Harmony(harmonyId);
        }
    }
}

