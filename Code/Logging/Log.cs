using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ColossalFramework.Plugins;
using Debug = UnityEngine.Debug;

namespace PickyParking.Logging
{
    public static class Log
    {
        private const string Prefix = "[PickyParking] ";
        private const int MaxQueuedMessages = 500;
        private static readonly TimeSpan DefaultRateLimit = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan DebugPanelFailureRateLimit = TimeSpan.FromMinutes(5);
        private static readonly Queue<QueuedMessage> PendingMessages = new Queue<QueuedMessage>();
        private static readonly object QueueLock = new object();
        private static readonly object RateLimitLock = new object();
        private static readonly Dictionary<string, DateTime> RateLimitedLogs = new Dictionary<string, DateTime>();
        private static volatile bool _debugPanelReady;
        private static int _mainThreadId;
        private static DateTime _debugPanelFailureLast;

        public static bool IsVerboseEnabled { get; private set; }
        public static bool IsRuleUiDebugEnabled { get; private set; }
        public static bool IsLotDebugEnabled { get; private set; }
        public static bool IsDecisionDebugEnabled { get; private set; }
        public static bool IsEnforcementDebugEnabled { get; private set; }
        public static bool IsTmpeDebugEnabled { get; private set; }

        public static void Info(string message) => Write(PluginManager.MessageType.Message, message, false);
        public static void Warn(string message) => Write(PluginManager.MessageType.Warning, message, true);
        public static void Error(string message) => Write(PluginManager.MessageType.Error, message, true);
        public static void WarnOnce(string key, string message) => WarnOnce(key, message, DefaultRateLimit);

        public static void WarnOnce(string key, string message, TimeSpan interval)
        {
            if (!ShouldLogRateLimited(key, interval))
                return;

            Warn(message);
        }

        public static void MarkDebugPanelReady()
        {
            _debugPanelReady = true;
            InitializeMainThread();
            FlushQueuedMessagesFromMainThread();
        }

        public static void MarkDebugPanelNotReady()
        {
            _debugPanelReady = false;
            ClearQueuedMessages();
        }

        public static void InitializeMainThread()
        {
            if (_mainThreadId == 0)
                _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public static void FlushQueuedMessagesFromMainThread()
        {
            if (!CanWriteToDebugPanel())
                return;

            FlushQueuedMessagesInternal();
        }

        public static void SetVerboseEnabled(bool isEnabled)
        {
            IsVerboseEnabled = isEnabled;
        }

        public static void SetRuleUiDebugEnabled(bool isEnabled)
        {
            IsRuleUiDebugEnabled = isEnabled;
        }

        public static void SetLotDebugEnabled(bool isEnabled)
        {
            IsLotDebugEnabled = isEnabled;
        }

        public static void SetDecisionDebugEnabled(bool isEnabled)
        {
            IsDecisionDebugEnabled = isEnabled;
        }

        public static void SetEnforcementDebugEnabled(bool isEnabled)
        {
            IsEnforcementDebugEnabled = isEnabled;
        }

        public static void SetTmpeDebugEnabled(bool isEnabled)
        {
            IsTmpeDebugEnabled = isEnabled;
        }

        private static void Write(PluginManager.MessageType type, string message, bool stacktrace)
        {
            var st = stacktrace ? $"\n{new StackTrace(2, true)}" : string.Empty;
            var callerTag = string.Empty;

            if (type == PluginManager.MessageType.Message)
            {
                var frame = new StackFrame(2, false);
                var method = frame.GetMethod();
                var declaringType = method != null ? method.DeclaringType : null;
                if (declaringType != null)
                    callerTag = $"[{declaringType.Name}] ";
            }

            var line = $"{Prefix}{callerTag}{message}{st}";

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

            if (ShouldQueueDebugPanelMessage())
            {
                EnqueueMessage(type, line);
                return;
            }

            FlushQueuedMessagesInternal();
            AddToDebugPanel(type, line);
        }

        private static bool ShouldQueueDebugPanelMessage()
        {
            return !CanWriteToDebugPanel();
        }

        private static bool CanWriteToDebugPanel()
        {
            if (!_debugPanelReady)
                return false;

            if (_mainThreadId == 0)
                return false;

            return Thread.CurrentThread.ManagedThreadId == _mainThreadId;
        }

        private static void EnqueueMessage(PluginManager.MessageType type, string line)
        {
            lock (QueueLock)
            {
                while (PendingMessages.Count >= MaxQueuedMessages)
                    PendingMessages.Dequeue();

                PendingMessages.Enqueue(new QueuedMessage(type, line));
            }
        }

        private static void ClearQueuedMessages()
        {
            lock (QueueLock)
            {
                PendingMessages.Clear();
            }
        }

        private static void FlushQueuedMessagesInternal()
        {
            List<QueuedMessage> drained = null;
            lock (QueueLock)
            {
                if (PendingMessages.Count == 0)
                    return;

                drained = new List<QueuedMessage>(PendingMessages.Count);
                while (PendingMessages.Count > 0)
                    drained.Add(PendingMessages.Dequeue());
            }

            for (int i = 0; i < drained.Count; i++)
            {
                var queued = drained[i];
                AddToDebugPanel(queued.Type, queued.Line);
            }
        }

        private static void AddToDebugPanel(PluginManager.MessageType type, string line)
        {
            try
            {
                DebugOutputPanel.AddMessage(type, line);
            }
            catch (InvalidOperationException ex)
            {
                LogDebugPanelFailure(ex);
            }
            catch (ArgumentException ex)
            {
                LogDebugPanelFailure(ex);
            }
            catch (NullReferenceException ex)
            {
                LogDebugPanelFailure(ex);
            }
        }

        private static bool ShouldLogRateLimited(string key, TimeSpan interval)
        {
            var now = DateTime.UtcNow;
            lock (RateLimitLock)
            {
                if (RateLimitedLogs.TryGetValue(key, out var last) && now - last < interval)
                    return false;

                RateLimitedLogs[key] = now;
                return true;
            }
        }

        private static void LogDebugPanelFailure(Exception ex)
        {
            var now = DateTime.UtcNow;
            if (now - _debugPanelFailureLast < DebugPanelFailureRateLimit)
                return;

            _debugPanelFailureLast = now;
            Debug.LogWarning($"{Prefix}[Logging] DebugOutputPanel.AddMessage failed: {ex}");
        }

        private readonly struct QueuedMessage
        {
            public QueuedMessage(PluginManager.MessageType type, string line)
            {
                Type = type;
                Line = line;
            }

            public PluginManager.MessageType Type { get; }
            public string Line { get; }
        }
    }
    
}
