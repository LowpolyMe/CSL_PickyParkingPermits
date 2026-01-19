using PickyParking.Features.Debug;
using UnityEngine;
using PickyParking.Logging;
using PickyParking.ModEntry;

namespace PickyParking.Features.Debug
{
    public sealed class DebugHotkeyListener : MonoBehaviour
    {
        private const KeyCode TogglePrefabKey = KeyCode.Keypad0;
        private const KeyCode ToggleApplyKey = KeyCode.Keypad1;
        private const KeyCode ToggleDefaultRuleKey = KeyCode.Keypad2;
        private const KeyCode ToggleResidentsKey = KeyCode.Keypad3;
        private const KeyCode ToggleWorkSchoolKey = KeyCode.Keypad4;
        private const KeyCode ToggleVisitorsKey = KeyCode.Keypad5;
        private const KeyCode DumpKey = KeyCode.Keypad9;

        public void Start()
        {
            if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
            {
                Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "DebugHotkeyListenerStarted");
            }
        }

        public void Update()
        {
            ModRuntime runtime = ModRuntime.Current;
            if (runtime == null || runtime.DebugHotkeyController == null)
                return;

            DebugHotkeyController controller = runtime.DebugHotkeyController;

            if (Input.GetKeyDown(TogglePrefabKey))
            {
                controller.TryToggleSupportedPrefab();
                return;
            }

            if (!IsAnyRuleKeyDown())
                return;

            if (Input.GetKeyDown(ToggleApplyKey))
            {
                controller.TryRequestReevaluationForSelection();
                return;
            }

            if (Input.GetKeyDown(ToggleDefaultRuleKey))
            {
                controller.TryToggleDefaultRule();
                return;
            }

            if (Input.GetKeyDown(ToggleResidentsKey))
            {
                controller.TryToggleResidentsRule();
                return;
            }

            if (Input.GetKeyDown(ToggleWorkSchoolKey))
            {
                controller.TryToggleWorkSchoolRule();
                return;
            }

            if (Input.GetKeyDown(ToggleVisitorsKey))
            {
                controller.TryToggleVisitorsRule();
                return;
            }

            if (Input.GetKeyDown(DumpKey))
            {
                controller.TryDumpSelectionState();
            }
        }

        private static bool IsAnyRuleKeyDown()
        {
            return Input.GetKeyDown(ToggleApplyKey)
                   || Input.GetKeyDown(ToggleDefaultRuleKey)
                   || Input.GetKeyDown(ToggleResidentsKey)
                   || Input.GetKeyDown(ToggleWorkSchoolKey)
                   || Input.GetKeyDown(ToggleVisitorsKey)
                   || Input.GetKeyDown(DumpKey);
        }
    }
}
