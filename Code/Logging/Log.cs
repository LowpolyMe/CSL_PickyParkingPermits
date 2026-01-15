using System;
using System.Diagnostics;
using ColossalFramework.Plugins;
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

            Write(PluginManager.MessageType.Message, $"[{category}] {message}", stackTraceEnabled: false);
        }

        public static void Warn(DebugLogCategory category, string message, bool stackTraceEnabled = false)
        {
            if (!Policy.ShouldLog(category))
                return;

            Write(PluginManager.MessageType.Warning, $"[{category}] {message}", stackTraceEnabled);
        }

        public static void Error(DebugLogCategory category, string message, bool stackTraceEnabled = false)
        {
            if (!Policy.ShouldLog(category))
                return;

            Write(PluginManager.MessageType.Error, $"[{category}] {message}", stackTraceEnabled);
        }

        public static void WarnOnce(DebugLogCategory category, string key, string message) =>
            WarnOnce(category, key, message, DefaultRateLimit);

        public static void AlwaysWarn(string message, bool stackTraceEnabled = false) =>
            Write(PluginManager.MessageType.Warning, message, stackTraceEnabled);

        public static void AlwaysError(string message, bool stackTraceEnabled = false) =>
            Write(PluginManager.MessageType.Error, message, stackTraceEnabled);

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

        private static void Write(PluginManager.MessageType type, string message, bool stackTraceEnabled)
        {
            string stack = stackTraceEnabled ? "\n" + new StackTrace(2, true) : string.Empty;
            string callerTag = CallerTag(type);

            string line = $"{Prefix}{callerTag}{message}{stack}";

            // Unity log
            switch (type)
            {
                case PluginManager.MessageType.Warning:
                    Debug.LogWarning(line);
                    break;
                case PluginManager.MessageType.Error:
                    Debug.LogError(line);
                    break;
                default:
                    Debug.Log(line);
                    break;
            }
            
            DebugPanel.EnqueueOrWrite(type, line);
        }

        private static string CallerTag(PluginManager.MessageType type)
        {
            if (type != PluginManager.MessageType.Message)
                return string.Empty;

            var frame = new StackFrame(2, false);
            var method = frame.GetMethod();
            var declaringType = method != null ? method.DeclaringType : null;

            return declaringType != null ? $"[{declaringType.Name}] " : string.Empty;
        }
    }
}
