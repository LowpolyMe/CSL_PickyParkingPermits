using System;
using System.Reflection;
using ColossalFramework.Plugins;
using ICities;
using PickyParking.Features.Debug;
using PickyParking.Logging;
using PickyParking.Settings;

namespace PickyParking.ModLifecycle.BackendSelection
{
    public sealed class ParkingBackendState
    {
        private const string AdvancedParkingManagerTypeName =
            "TrafficManager.Manager.Impl.AdvancedParkingManager, TrafficManager";

        private const string GlobalConfigTypeName =
            "TrafficManager.State.GlobalConfig, TrafficManager";

        private const string SavedGameOptionsTypeName =
            "TrafficManager.State.SavedGameOptions, TrafficManager";

        private static readonly string[] AdvancedParkingMemberNames =
        {
            "AdvancedParkingEnabled",
            "EnableAdvancedParking",
            "advancedParkingEnabled",
            "enableAdvancedParking"
        };

        public bool IsTmpeDetected { get; private set; }
        public bool IsTmpeAdvancedParkingActive { get; private set; }
        public ParkingBackendKind ActiveBackend { get; private set; }
        public string Reason { get; private set; }

        public void Refresh()
        {
            bool isTmpeDetected = false;
            bool isAdvancedActive = false;
            bool isAdvancedKnown = false;
            string reason;

            bool tmpePluginFound;
            bool tmpePluginEnabled;
            bool tmpePluginStateKnown = TryGetTmpePluginState(out tmpePluginFound, out tmpePluginEnabled);
            if (tmpePluginStateKnown)
            {
                isTmpeDetected = tmpePluginFound && tmpePluginEnabled;
            }
            else
            {
                isTmpeDetected = Type.GetType(AdvancedParkingManagerTypeName, false) != null;
            }

            if (isTmpeDetected)
            {
                try
                {
                    TryGetAdvancedParkingFlag(out isAdvancedActive, out isAdvancedKnown);
                }
                catch (Exception)
                {
                    isAdvancedActive = false;
                    isAdvancedKnown = false;
                }

                if (isAdvancedActive)
                    reason = "TmpeAdvancedEnabled";
                else
                    reason = isAdvancedKnown ? "TmpeAdvancedDisabled" : "TmpeAdvancedUnknownAssumedOff";
            }
            else
            {
                if (tmpePluginStateKnown && tmpePluginFound && !tmpePluginEnabled)
                    reason = "TmpeDisabled";
                else
                    reason = "TmpeNotDetected";
            }

            IsTmpeDetected = isTmpeDetected;
            IsTmpeAdvancedParkingActive = isAdvancedActive;
            ActiveBackend = isTmpeDetected && isAdvancedActive
                ? ParkingBackendKind.TmpeAdvanced
                : ParkingBackendKind.Vanilla;
            Reason = reason;

            if (Log.IsVerboseEnabled)
            {
                Log.Info(
                    DebugLogCategory.None,
                    "[BackendSelection] " +
                    "backend=" + ActiveBackend +
                    " tmpeDetected=" + IsTmpeDetected +
                    " tmpeAdvanced=" + IsTmpeAdvancedParkingActive +
                    " reason=" + (Reason ?? "UNKNOWN"));
            }
        }

        private static bool TryGetTmpePluginState(out bool found, out bool enabled)
        {
            found = false;
            enabled = false;

            PluginManager pluginManager = PluginManager.instance;
            if (pluginManager == null)
                return false;

            foreach (PluginManager.PluginInfo plugin in pluginManager.GetPluginsInfo())
            {
                if (plugin == null)
                    continue;

                if (!IsTmpePlugin(plugin))
                    continue;

                found = true;
                enabled = plugin.isEnabled;
                return true;
            }

            return true;
        }

        private static bool IsTmpePlugin(PluginManager.PluginInfo plugin)
        {
            if (plugin == null)
                return false;

            IUserMod userMod = plugin.userModInstance as IUserMod;
            if (userMod != null)
            {
                string modName = userMod.Name;
                if (!string.IsNullOrEmpty(modName))
                {
                    if (modName.IndexOf("TM:PE", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                    if (modName.IndexOf("Traffic Manager", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            var assemblies = plugin.GetAssemblies();
            if (assemblies == null)
                return false;

            for (int i = 0; i < assemblies.Count; i++)
            {
                Assembly assembly = assemblies[i];
                if (assembly == null)
                    continue;

                AssemblyName name = assembly.GetName();
                if (name != null)
                {
                    string assemblyName = name.Name;
                    if (IsTmpeAssemblyId(assemblyName))
                        return true;
                }

                object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                for (int j = 0; j < attributes.Length; j++)
                {
                    AssemblyTitleAttribute titleAttribute = attributes[j] as AssemblyTitleAttribute;
                    if (titleAttribute == null)
                        continue;

                    if (IsTmpeAssemblyId(titleAttribute.Title))
                        return true;
                }
            }

            return false;
        }

        private static bool IsTmpeAssemblyId(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            return string.Equals(value, "TrafficManager", StringComparison.Ordinal)
                || string.Equals(value, "TLM", StringComparison.Ordinal);
        }

        private static void TryGetAdvancedParkingFlag(out bool isEnabled, out bool isKnown)
        {
            isEnabled = false;
            isKnown = false;

            Type configType = Type.GetType(GlobalConfigTypeName, false);
            if (configType != null)
            {
                if (TryReadAdvancedParkingFlag(configType, out isEnabled))
                {
                    isKnown = true;
                    return;
                }
            }

            bool savedGameValue;
            if (TryGetSavedGameOptionsParkingAi(out savedGameValue))
            {
                isEnabled = savedGameValue;
                isKnown = true;
            }
        }

        private static bool TryReadAdvancedParkingFlag(Type configType, out bool value)
        {
            value = false;
            for (int i = 0; i < AdvancedParkingMemberNames.Length; i++)
            {
                string name = AdvancedParkingMemberNames[i];
                if (TryReadBooleanMember(configType, name, out value))
                    return true;
            }

            object instance = TryGetConfigInstance(configType);
            if (instance == null)
                return false;

            object parkingAi = TryGetMemberValue(instance, "ParkingAI");
            if (parkingAi == null)
                return false;

            Type parkingAiType = parkingAi.GetType();
            for (int i = 0; i < AdvancedParkingMemberNames.Length; i++)
            {
                string name = AdvancedParkingMemberNames[i];
                if (TryReadBooleanMemberOnInstance(parkingAi, parkingAiType, name, out value))
                    return true;
            }

            return false;
        }

        private static bool TryGetSavedGameOptionsParkingAi(out bool value)
        {
            value = false;
            Type savedOptionsType = Type.GetType(SavedGameOptionsTypeName, false);
            if (savedOptionsType == null)
                return false;

            return TryReadBooleanMember(savedOptionsType, "parkingAI", out value);
        }

        private static bool TryReadBooleanMember(Type configType, string memberName, out bool value)
        {
            value = false;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            PropertyInfo property = configType.GetProperty(memberName, flags);
            if (property != null)
            {
                MethodInfo getter = property.GetGetMethod(true);
                if (getter == null)
                    return false;

                object target = getter.IsStatic ? null : TryGetConfigInstance(configType);
                if (!getter.IsStatic && target == null)
                    return false;

                object rawValue = property.GetValue(target, null);
                if (rawValue is bool)
                {
                    value = (bool)rawValue;
                    return true;
                }

                return false;
            }

            FieldInfo field = configType.GetField(memberName, flags);
            if (field == null)
                return false;

            object fieldTarget = field.IsStatic ? null : TryGetConfigInstance(configType);
            if (!field.IsStatic && fieldTarget == null)
                return false;

            object fieldValue = field.GetValue(fieldTarget);
            if (fieldValue is bool)
            {
                value = (bool)fieldValue;
                return true;
            }

            return false;
        }

        private static bool TryReadBooleanMemberOnInstance(object target, Type targetType, string memberName, out bool value)
        {
            value = false;
            if (target == null || targetType == null)
                return false;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            PropertyInfo property = targetType.GetProperty(memberName, flags);
            if (property != null)
            {
                object rawValue = property.GetValue(target, null);
                if (rawValue is bool)
                {
                    value = (bool)rawValue;
                    return true;
                }

                return false;
            }

            FieldInfo field = targetType.GetField(memberName, flags);
            if (field == null)
                return false;

            object fieldValue = field.GetValue(target);
            if (fieldValue is bool)
            {
                value = (bool)fieldValue;
                return true;
            }

            return false;
        }

        private static object TryGetMemberValue(object target, string memberName)
        {
            if (target == null)
                return null;

            Type targetType = target.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            PropertyInfo property = targetType.GetProperty(memberName, flags);
            if (property != null)
                return property.GetValue(target, null);

            FieldInfo field = targetType.GetField(memberName, flags);
            if (field != null)
                return field.GetValue(target);

            return null;
        }

        private static object TryGetConfigInstance(Type configType)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            PropertyInfo property = configType.GetProperty("Instance", flags);
            if (property != null)
            {
                MethodInfo getter = property.GetGetMethod(true);
                if (getter != null && getter.IsStatic)
                    return property.GetValue(null, null);
            }

            FieldInfo field = configType.GetField("Instance", flags);
            if (field != null && field.IsStatic)
                return field.GetValue(null);

            return null;
        }
    }
}
