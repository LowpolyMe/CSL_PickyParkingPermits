using System.Reflection;
using HarmonyLib;
using PickyParking.Features.Debug;
using PickyParking.Logging;
using PickyParking.UI.BuildingOptionsPanel.OverlayRendering;
using PickyParking.Settings;

namespace PickyParking.Patching.Game
{
    internal static class DefaultTool_RenderOverlayPatch
    {
        private const string TargetMethodName = "RenderOverlay";

        public static void Apply(Harmony harmony)
        {
            MethodInfo method = AccessTools.Method(typeof(DefaultTool), TargetMethodName, new[] { typeof(RenderManager.CameraInfo) });
            if (method == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                {
                    Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "PatchSkippedMissingMethod", "type=DefaultTool | method=" + TargetMethodName);
                }
                return;
            }

            harmony.Patch(
                method,
                postfix: new HarmonyMethod(typeof(DefaultTool_RenderOverlayPatch), nameof(Postfix)));

            if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
            {
                Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "PatchApplied", "type=DefaultTool | method=" + TargetMethodName);
            }
        }

        private static void Postfix(RenderManager.CameraInfo cameraInfo)
        {
            OverlayRenderer.RenderOverlay(cameraInfo);
        }
    }
}
