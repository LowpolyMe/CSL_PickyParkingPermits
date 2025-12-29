using System.Reflection;
using ColossalFramework;
using HarmonyLib;
using PickyParking.Infrastructure;
using PickyParking.UI;

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
                Log.Info("[Overlay] DefaultTool.RenderOverlay not found; skipping patch.");
                return;
            }

            harmony.Patch(
                method,
                postfix: new HarmonyMethod(typeof(DefaultTool_RenderOverlayPatch), nameof(Postfix)));

            Log.Info("[Overlay] Patched DefaultTool.RenderOverlay.");
        }

        private static void Postfix(RenderManager.CameraInfo cameraInfo)
        {
            OverlayRenderer.RenderOverlay(cameraInfo);
        }
    }
}
