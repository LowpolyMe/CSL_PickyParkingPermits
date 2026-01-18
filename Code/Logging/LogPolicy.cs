using PickyParking.Features.Debug;
using PickyParking.Settings;

namespace PickyParking.Logging
{
    internal sealed class LogPolicy
    {
        private volatile bool _isVerboseEnabled;
        private DebugLogCategory _enabledCategories;

        public bool IsVerboseEnabled => _isVerboseEnabled;
        public DebugLogCategory EnabledDebugCategories => _enabledCategories;

        public void SetVerboseEnabled(bool enabled)
        {
            _isVerboseEnabled = enabled;
        }

        public void SetEnabledCategories(DebugLogCategory enabledCategories)
        {
            _enabledCategories = enabledCategories;
        }

        public bool ShouldLog(DebugLogCategory category)
        {
            if (!_isVerboseEnabled)
                return false;

            return IsCategoryEnabled(category);
        }

        public bool IsCategoryEnabled(DebugLogCategory category)
        {
            if (category == DebugLogCategory.None)
                return true;

            return (_enabledCategories & category) != 0;
        }
    }
}