using System;
using System.Diagnostics;
using PickyParking.Settings;
using Debug = UnityEngine.Debug;

namespace PickyParking.Logging
{
    public static class Log
    {
        private const string Prefix = "[PickyParking] ";

        private static readonly LogPolicy Policy = new LogPolicy();
        private static readonly LogRateLimiter RateLimiter = new LogRateLimiter(
            maxKeys: 1000,
            ttl: TimeSpan.FromMinutes(10));

        private static readonly DebugPanelWriter DebugPanel = new DebugPanelWriter(
            maxQueuedMessages: 50,
            failureRateLimit: TimeSpan.FromMinutes(5));

        private static readonly TimeSpan DefaultRateLimit = TimeSpan.FromMinutes(5);
        

        public static bool IsVerboseEnabled => Policy.IsVerboseEnabled;
        public static DebugLogCategory EnabledDebugCategories => Policy.EnabledDebugCategories;

        public static bool IsRuleUiDebugEnabled => Policy.IsCategoryEnabled(DebugLogCategory.RuleUi);
        public static bool IsLotDebugEnabled => Policy.IsCategoryEnabled(DebugLogCategory.LotInspection);
        public static bool IsDecisionDebugEnabled => Policy.IsCategoryEnabled(DebugLogCategory.DecisionPipeline);
        public static bool IsEnforcementDebugEnabled => Policy.IsCategoryEnabled(DebugLogCategory.Enforcement);
        public static bool IsTmpeDebugEnabled => Policy.IsCategoryEnabled(DebugLogCategory.Tmpe);

        public static void SetVerboseEnabled(bool isEnabled) => Policy.SetVerboseEnabled(isEnabled);
        public static void SetEnabledDebugCategories(DebugLogCategory enabledCategories) => Policy.SetEnabledCategories(enabledCategories);

        public static void MarkDebugPanelReady() => DebugPanel.MarkReady();
        public static void MarkDebugPanelNotReady() => DebugPanel.MarkNotReady();
        public static void InitializeMainThread() => DebugPanel.InitializeMainThreadIfNeeded();
        public static void FlushQueuedMessagesFromMainThread() => DebugPanel.FlushFromMainThread();

        public static void Info(DebugLogCategory category, string message)
        {
            if (!Policy.ShouldLog(category))
                return;
          
            string line = $"{Prefix}{message}";
            Debug.Log(line);
        }

        public static void Warn(DebugLogCategory category, string message, bool stackTraceEnabled = false)
        {
            if (!Policy.ShouldLog(category))
                return;
            
            string stack = stackTraceEnabled ? "\n" + new StackTrace(2, true) : string.Empty;
            string line = $"{Prefix}{message}{stack}";
            Debug.LogWarning(line);
        }

        public static void Error(DebugLogCategory category, string message, bool stackTraceEnabled = false)
        {
            if (!Policy.ShouldLog(category))
                return;

            string stack = stackTraceEnabled ? "\n" + new StackTrace(2, true) : string.Empty;
            string line = $"{Prefix}{message}{stack}";
            Debug.LogError(line);
        }

        public static void WarnOnce(DebugLogCategory category, string key, string message) =>
            WarnOnce(category, key, message, DefaultRateLimit);

        //ignores verbose flag
        public static void AlwaysWarn(string message, bool stackTraceEnabled = false)
        {
            string stack = stackTraceEnabled ? "\n" + new StackTrace(2, true) : string.Empty;
            string line = $"{Prefix}{message}{stack}";
            Debug.LogWarning(line);
        }

        public static void AlwaysError(string message, bool stackTraceEnabled = false)
        {
            string stack = stackTraceEnabled ? "\n" + new StackTrace(2, true) : string.Empty;
            string line = $"{Prefix}{message}{stack}";
            Debug.LogError(line);
        }

        public static void AlwaysWarnOnce(string key, string message) =>
            AlwaysWarnOnce(key, message, DefaultRateLimit);



        private static void WarnOnce(DebugLogCategory category, string key, string message, TimeSpan interval)
        {
            string namespacedKey = key;

            if (!RateLimiter.ShouldLog(namespacedKey, interval))
                return;

            Warn(category, message);
        }

        private static void AlwaysWarnOnce(string key, string message, TimeSpan interval)
        {
            string namespacedKey = key;

            if (!RateLimiter.ShouldLog(namespacedKey, interval))
                return;

            AlwaysWarn(message);
        }

    }
}
