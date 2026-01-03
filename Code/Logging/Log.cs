using System.Diagnostics;
using ColossalFramework.Plugins;
using Debug = UnityEngine.Debug;

namespace PickyParking.Logging
{
    public static class Log
    {
        private const string Prefix = "[PickyParking] ";

        public static bool IsVerboseEnabled { get; private set; }
        public static bool IsUiDebugEnabled { get; private set; }
        public static bool IsTmpeDebugEnabled { get; private set; }
        public static bool IsPermissionDebugEnabled { get; private set; }

        public static void Info(string message) => Write(PluginManager.MessageType.Message, message, false);
        public static void Warn(string message) => Write(PluginManager.MessageType.Warning, message, true);
        public static void Error(string message) => Write(PluginManager.MessageType.Error, message, true);

        public static void SetVerboseEnabled(bool isEnabled)
        {
            IsVerboseEnabled = isEnabled;
        }

        public static void SetUiDebugEnabled(bool isEnabled)
        {
            IsUiDebugEnabled = isEnabled;
        }

        public static void SetTmpeDebugEnabled(bool isEnabled)
        {
            IsTmpeDebugEnabled = isEnabled;
        }

        public static void SetPermissionDebugEnabled(bool isEnabled)
        {
            IsPermissionDebugEnabled = isEnabled;
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
            
            try
            {
                DebugOutputPanel.AddMessage(type, line);
            }
            catch
            {
                
            }
        }
    }
}

