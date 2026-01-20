using PickyParking.Features.Debug;
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
            string reasonText = reason ?? "UNKNOWN";
            Log.Player.Warn(DebugLogCategory.Enforcement, LogPath.Any, "FeatureGateInactive", "reason=" + reasonText);
        }
    }
}

