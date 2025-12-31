using ColossalFramework;
using ColossalFramework.Math;
using PickyParking.ModLifecycle;
using UnityEngine;
using PickyParking.ModEntry;
using PickyParking.Features.ParkingRules;

namespace PickyParking.UI
{
    
    
    
    public static class RadiusOverlayRenderer
    {
        private const float OverlayAlpha = 0.7f;
        private const float MinOverlayY = -100000f;
        private const float MaxOverlayY = 100000f;

        public static void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            ModRuntime runtime = ModRuntime.Current;
            if (runtime == null || !runtime.FeatureGate.IsActive)
                return;

            if (!runtime.GameAccess.TryGetSelectedBuilding(out ushort buildingId, out _))
                return;

            ParkingRulesConfigDefinition rule;
            if (runtime.ParkingRulePreviewState != null
                && runtime.ParkingRulePreviewState.TryGetPreview(buildingId, out var previewRule))
            {
                rule = previewRule;
            }
            else if (!runtime.ParkingRulesConfigRegistry.TryGet(buildingId, out rule))
            {
                return;
            }
            

            if (!runtime.GameAccess.TryGetBuildingPosition(buildingId, out Vector3 center))
                return;

            if (cameraInfo == null)
                return;

            float minY = MinOverlayY;
            float maxY = MaxOverlayY;

            float residentsHue = 0.35f;
            float workSchoolHue = 0.1f;
            if (runtime.SettingsController != null && runtime.SettingsController.Current != null)
            {
                residentsHue = runtime.SettingsController.Current.ResidentsRadiusHue;
                workSchoolHue = runtime.SettingsController.Current.WorkSchoolRadiusHue;
            }

            if (rule.ResidentsWithinRadiusOnly)
            {
                Color color = ColorConversion.FromHue(residentsHue, OverlayAlpha);
                if (rule.ResidentsRadiusMeters == ushort.MaxValue)
                {
                    DrawFullMapOverlay(cameraInfo, color, minY, maxY);
                }
                else
                {
                    DrawCircle(cameraInfo, color, center, rule.ResidentsRadiusMeters, minY, maxY);
                }
            }

            if (rule.WorkSchoolWithinRadiusOnly)
            {
                Color color = ColorConversion.FromHue(workSchoolHue, OverlayAlpha);
                if (rule.WorkSchoolRadiusMeters == ushort.MaxValue)
                {
                    DrawFullMapOverlay(cameraInfo, color, minY, maxY);
                }
                else
                {
                    DrawCircle(cameraInfo, color, center, rule.WorkSchoolRadiusMeters, minY, maxY);
                }
            }
        }

        private static Quad3 BuildMapQuad()
        {
            const float mapSizeMeters = 17280f;
            float half = mapSizeMeters * 0.5f;
            return new Quad3(
                new Vector3(-half, 0f, -half),
                new Vector3(half, 0f, -half),
                new Vector3(half, 0f, half),
                new Vector3(-half, 0f, half));
        }

        private static void DrawCircle(RenderManager.CameraInfo cameraInfo, Color color, Vector3 center, float radius, float minY, float maxY)
        {
            if (radius <= 0f)
                return;

            float size = radius * 2f;
            Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(
                cameraInfo,
                color,
                center,
                size,
                minY,
                maxY,
                renderLimits: false,
                alphaBlend: true);
        }

        private static void DrawFullMapOverlay(RenderManager.CameraInfo cameraInfo, Color color, float minY, float maxY)
        {
            Singleton<RenderManager>.instance.OverlayEffect.DrawQuad(
                cameraInfo,
                color,
                BuildMapQuad(),
                minY,
                maxY,
                renderLimits: false,
                alphaBlend: true);
        }
    }
}
