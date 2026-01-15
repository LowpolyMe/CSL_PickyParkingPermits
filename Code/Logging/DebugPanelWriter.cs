using System;
using System.Collections.Generic;
using System.Threading;
using ColossalFramework.Plugins;
using Debug = UnityEngine.Debug;

namespace PickyParking.Logging
{
    internal sealed class DebugPanelWriter
    {
        private readonly int _maxQueuedMessages;
        private readonly TimeSpan _failureRateLimit;

        private readonly object _queueLock = new object();
        private readonly object _failureLock = new object();

        private readonly Queue<QueuedMessage> _pending = new Queue<QueuedMessage>();

        private volatile bool _ready;
        private volatile int _mainThreadId;
        private DateTime _lastFailure;

        public DebugPanelWriter(int maxQueuedMessages, TimeSpan failureRateLimit)
        {
            _maxQueuedMessages = maxQueuedMessages;
            _failureRateLimit = failureRateLimit;
        }

        public void MarkReady()
        {
            _ready = true;
            InitializeMainThreadIfNeeded();
            FlushFromMainThread();
        }

        public void MarkNotReady()
        {
            _ready = false;
            ClearQueue();
        }

        public void InitializeMainThreadIfNeeded()
        {
            if (!_ready)
                return;

            if (_mainThreadId == 0)
                _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public void EnqueueOrWrite(PluginManager.MessageType type, string line)
        {
            if (!CanWriteToDebugPanel())
            {
                Enqueue(type, line);
                return;
            }
            
            FlushInternal();
            AddToDebugPanel(type, line);
        }

        public void FlushFromMainThread()
        {
            if (!CanWriteToDebugPanel())
                return;

            FlushInternal();
        }

        private bool CanWriteToDebugPanel()
        {
            if (!_ready)
                return false;

            if (_mainThreadId == 0)
                return false;

            return Thread.CurrentThread.ManagedThreadId == _mainThreadId;
        }

        private void Enqueue(PluginManager.MessageType type, string line)
        {
            lock (_queueLock)
            {
                while (_pending.Count >= _maxQueuedMessages)
                    _pending.Dequeue();

                _pending.Enqueue(new QueuedMessage(type, line));
            }
        }

        private void ClearQueue()
        {
            lock (_queueLock)
            {
                _pending.Clear();
            }
        }

        private void FlushInternal()
        {
            List<QueuedMessage> drained = null;

            lock (_queueLock)
            {
                if (_pending.Count == 0)
                    return;

                drained = new List<QueuedMessage>(_pending.Count);
                while (_pending.Count > 0)
                    drained.Add(_pending.Dequeue());
            }

            for (int i = 0; i < drained.Count; i++)
            {
                var q = drained[i];
                AddToDebugPanel(q.Type, q.Line);
            }
        }

        private void AddToDebugPanel(PluginManager.MessageType type, string line)
        {
            try
            {
                DebugOutputPanel.AddMessage(type, line);
            }
            catch (Exception ex)
            {
                LogFailureThrottled(ex);
            }
        }

        private void LogFailureThrottled(Exception ex)
        {
            var now = DateTime.UtcNow;

            lock (_failureLock)
            {
                if (now - _lastFailure < _failureRateLimit)
                    return;

                _lastFailure = now;
            }

            Debug.LogWarning("[PickyParking] [Logging] DebugOutputPanel.AddMessage failed: " + ex);
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
