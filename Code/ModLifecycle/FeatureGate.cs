using PickyParking.Logging;

namespace PickyParking.ModLifecycle
{
    public sealed class FeatureGate
    {
        public bool IsActive { get; private set; }
        
        public string InactiveReason { get; private set; }
        
        public void SetActive()
        {
            IsActive = true;
            InactiveReason = null;
        }
        
        public void SetInactive(string reason)
        {
            IsActive = false;
            InactiveReason = reason;
            Log.AlwaysWarn("[Runtime] Feature gate inactive: " + (reason ?? "UNKNOWN"));
        }
    }
}

