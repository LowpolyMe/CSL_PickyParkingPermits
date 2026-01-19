using System;
using System.Diagnostics;
using ColossalFramework.Plugins;
using PickyParking.Features.Debug;
using PickyParking.Settings;
using Debug = UnityEngine.Debug;
//Intentionally dropped DebugOutputPanel messages and suppressed most warnings/errors unless verbose logging is enabled. 
namespace PickyParking.Logging
{
    public static class Log
    {
        private const string Prefix = "[PickyParking]";

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

        public static class Dev
        {
            public static bool IsEnabled(DebugLogCategory feature)
            {
                return Policy.ShouldLog(feature);
            }

            public static void Debug(DebugLogCategory feature, LogPath path, string eventName, string fields = null, string rateKey = null, TimeSpan? rateInterval = null, bool stackTraceEnabled = false)
            {
                Emit(LogAudience.Dev, LogLevel.Debug, feature, path, eventName, fields, rateKey, rateInterval, stackTraceEnabled, null);
            }

            public static void Info(DebugLogCategory feature, LogPath path, string eventName, string fields = null, string rateKey = null, TimeSpan? rateInterval = null, bool stackTraceEnabled = false)
            {
                Emit(LogAudience.Dev, LogLevel.Info, feature, path, eventName, fields, rateKey, rateInterval, stackTraceEnabled, null);
            }

            public static void Warn(DebugLogCategory feature, LogPath path, string eventName, string fields = null, string rateKey = null, TimeSpan? rateInterval = null, bool stackTraceEnabled = false)
            {
                Emit(LogAudience.Dev, LogLevel.Warn, feature, path, eventName, fields, rateKey, rateInterval, stackTraceEnabled, null);
            }

            public static void Error(DebugLogCategory feature, LogPath path, string eventName, string fields = null, string rateKey = null, TimeSpan? rateInterval = null, bool stackTraceEnabled = false)
            {
                Emit(LogAudience.Dev, LogLevel.Error, feature, path, eventName, fields, rateKey, rateInterval, stackTraceEnabled, null);
            }

            public static void Exception(DebugLogCategory feature, LogPath path, string eventName, Exception exception, string fields = null, string rateKey = null, TimeSpan? rateInterval = null)
            {
                Emit(LogAudience.Dev, LogLevel.Exception, feature, path, eventName, fields, rateKey, rateInterval, false, exception);
            }
        }

        public static class Player
        {
            public static void Info(DebugLogCategory feature, LogPath path, string eventName, string fields = null, string rateKey = null, TimeSpan? rateInterval = null, bool stackTraceEnabled = false)
            {
                Emit(LogAudience.Player, LogLevel.Info, feature, path, eventName, fields, rateKey, rateInterval, stackTraceEnabled, null);
            }

            public static void Warn(DebugLogCategory feature, LogPath path, string eventName, string fields = null, string rateKey = null, TimeSpan? rateInterval = null, bool stackTraceEnabled = false)
            {
                Emit(LogAudience.Player, LogLevel.Warn, feature, path, eventName, fields, rateKey, rateInterval, stackTraceEnabled, null);
            }

            public static void Error(DebugLogCategory feature, LogPath path, string eventName, string fields = null, string rateKey = null, TimeSpan? rateInterval = null, bool stackTraceEnabled = false)
            {
                Emit(LogAudience.Player, LogLevel.Error, feature, path, eventName, fields, rateKey, rateInterval, stackTraceEnabled, null);
            }

            public static void Exception(DebugLogCategory feature, LogPath path, string eventName, Exception exception, string fields = null, string rateKey = null, TimeSpan? rateInterval = null)
            {
                Emit(LogAudience.Player, LogLevel.Exception, feature, path, eventName, fields, rateKey, rateInterval, false, exception);
            }
        }

        public static void Info(DebugLogCategory category, string message)
        {
            EmitLegacy(LogLevel.Info, category, message, "LegacyInfo", false, LogAudience.Dev);
        }

        public static void Warn(DebugLogCategory category, string message, bool stackTraceEnabled = false)
        {
            EmitLegacy(LogLevel.Warn, category, message, "LegacyWarn", stackTraceEnabled, LogAudience.Dev);
        }

        public static void Error(DebugLogCategory category, string message, bool stackTraceEnabled = false)
        {
            EmitLegacy(LogLevel.Error, category, message, "LegacyError", stackTraceEnabled, LogAudience.Dev);
        }

        public static void WarnOnce(DebugLogCategory category, string key, string message)
        {
            WarnOnce(category, key, message, DefaultRateLimit);
        }

        // TODO (Iteration2): Migrate call sites to explicit Log.Player or Log.Dev methods.
        public static void AlwaysWarn(string message, bool stackTraceEnabled = false)
        {
            EmitLegacy(LogLevel.Warn, DebugLogCategory.None, message, "LegacyAlwaysWarn", stackTraceEnabled, LogAudience.Player);
        }

        // TODO (Iteration2): Migrate call sites to explicit Log.Player or Log.Dev methods.
        public static void AlwaysError(string message, bool stackTraceEnabled = false)
        {
            EmitLegacy(LogLevel.Error, DebugLogCategory.None, message, "LegacyAlwaysError", stackTraceEnabled, LogAudience.Player);
        }

        public static void AlwaysWarnOnce(string key, string message)
        {
            AlwaysWarnOnce(key, message, DefaultRateLimit);
        }

        private static void EmitLegacy(LogLevel level, DebugLogCategory feature, string message, string eventName, bool stackTraceEnabled, LogAudience audience)
        {
            EmitLazy(audience, level, feature, LogPath.Any, eventName, () => BuildLegacyFields(message), null, null, stackTraceEnabled, null);
        }

        private static string BuildLegacyFields(string message)
        {
            if (message == null)
            {
                return "msg=NULL";
            }

            return "msg=" + message;
        }

        private static void WarnOnce(DebugLogCategory category, string key, string message, TimeSpan interval)
        {
            EmitLazy(LogAudience.Dev, LogLevel.Warn, category, LogPath.Any, "LegacyWarnOnce", () => BuildLegacyFields(message), key, interval, false, null);
        }

        private static void AlwaysWarnOnce(string key, string message, TimeSpan interval)
        {
            // TODO (Iteration2): Migrate call sites to explicit Log.Player or Log.Dev methods.
            EmitLazy(LogAudience.Player, LogLevel.Warn, DebugLogCategory.None, LogPath.Any, "LegacyAlwaysWarnOnce", () => BuildLegacyFields(message), key, interval, false, null);
        }

        private static void Emit(LogAudience audience, LogLevel level, DebugLogCategory feature, LogPath path, string eventName, string fields, string rateKey, TimeSpan? rateInterval, bool stackTraceEnabled, Exception exception)
        {
            if (!ShouldEmit(audience, feature))
                return;

            int dropped;
            bool rateLimited = TryConsumeRateLimit(rateKey, rateInterval, out dropped);
            if (!rateLimited)
                return;

            WriteFormatted(audience, level, feature, path, eventName, fields, rateKey, dropped, stackTraceEnabled, exception);
        }

        private static void EmitLazy(LogAudience audience, LogLevel level, DebugLogCategory feature, LogPath path, string eventName, Func<string> fieldsBuilder, string rateKey, TimeSpan? rateInterval, bool stackTraceEnabled, Exception exception)
        {
            if (!ShouldEmit(audience, feature))
                return;

            int dropped;
            bool rateLimited = TryConsumeRateLimit(rateKey, rateInterval, out dropped);
            if (!rateLimited)
                return;

            string fields = fieldsBuilder == null ? null : fieldsBuilder();
            WriteFormatted(audience, level, feature, path, eventName, fields, rateKey, dropped, stackTraceEnabled, exception);
        }

        private static void WriteFormatted(LogAudience audience, LogLevel level, DebugLogCategory feature, LogPath path, string eventName, string fields, string rateKey, int dropped, bool stackTraceEnabled, Exception exception)
        {
            int? droppedValue = string.IsNullOrEmpty(rateKey) ? (int?)null : dropped;
            string line = FormatLine(audience, level, eventName, feature, path, fields, droppedValue);
            string finalLine = AppendDetails(line, stackTraceEnabled, exception);

            WriteToUnity(level, finalLine);
            WriteToDebugPanelIfNeeded(audience, level, finalLine);
        }

        private static bool ShouldEmit(LogAudience audience, DebugLogCategory feature)
        {
            if (audience == LogAudience.Player)
            {
                return true;
            }

            return Policy.ShouldLog(feature);
        }

        private static bool TryConsumeRateLimit(string rateKey, TimeSpan? rateInterval, out int dropped)
        {
            if (string.IsNullOrEmpty(rateKey))
            {
                dropped = 0;
                return true;
            }

            TimeSpan interval = rateInterval ?? DefaultRateLimit;
            return RateLimiter.TryConsume(rateKey, interval, out dropped);
        }

        private static string FormatLine(LogAudience audience, LogLevel level, string eventName, DebugLogCategory feature, LogPath path, string fields, int? dropped)
        {
            string audienceTag = GetAudienceTag(audience);
            string levelTag = GetLevelTag(level);
            string safeEvent = eventName ?? "UnknownEvent";

            string line = Prefix + " [" + audienceTag + "] [" + levelTag + "] event=" + safeEvent + " | feature=" + feature + " | path=" + path;

            if (!string.IsNullOrEmpty(fields))
            {
                line = line + " | " + fields;
            }

            if (dropped.HasValue)
            {
                line = line + " | dropped=" + dropped.Value;
            }

            return line;
        }

        private static string AppendDetails(string line, bool stackTraceEnabled, Exception exception)
        {
            string stack = string.Empty;
            if (stackTraceEnabled)
            {
                stack = "\n" + new StackTrace(3, true);
            }

            string exceptionText = string.Empty;
            if (exception != null)
            {
                exceptionText = "\n" + exception;
            }

            if (stack.Length == 0 && exceptionText.Length == 0)
            {
                return line;
            }

            return line + stack + exceptionText;
        }

        private static string GetAudienceTag(LogAudience audience)
        {
            if (audience == LogAudience.Player)
            {
                return "PLAYER";
            }

            return "DEV";
        }

        private static string GetLevelTag(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return "DEBUG";
                case LogLevel.Info:
                    return "INFO";
                case LogLevel.Warn:
                    return "WARN";
                case LogLevel.Error:
                    return "ERROR";
                case LogLevel.Exception:
                    return "EXCEPTION";
                default:
                    return "INFO";
            }
        }

        private static void WriteToUnity(LogLevel level, string line)
        {
            switch (level)
            {
                case LogLevel.Warn:
                    Debug.LogWarning(line);
                    return;
                case LogLevel.Error:
                case LogLevel.Exception:
                    Debug.LogError(line);
                    return;
                case LogLevel.Debug:
                case LogLevel.Info:
                default:
                    Debug.Log(line);
                    return;
            }
        }

        private static void WriteToDebugPanelIfNeeded(LogAudience audience, LogLevel level, string line)
        {
            if (audience != LogAudience.Player)
                return;

            PluginManager.MessageType messageType = ToMessageType(level);
            DebugPanel.EnqueueOrWrite(messageType, line);
        }

        private static PluginManager.MessageType ToMessageType(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Warn:
                    return PluginManager.MessageType.Warning;
                case LogLevel.Error:
                case LogLevel.Exception:
                    return PluginManager.MessageType.Error;
                case LogLevel.Debug:
                case LogLevel.Info:
                default:
                    return PluginManager.MessageType.Message;
            }
        }
    }
}
