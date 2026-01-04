using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using PickyParking.Features.ParkingRules;
using PickyParking.UI;

namespace PickyParking.UI.BuildingOptionsPanel.OverlayRendering
{
    
    
    
    public static class RadiusOverlayRenderer
    {
        private struct CircleOverlay
        {
            public RenderManager.CameraInfo CameraInfo;
            public Color Color;
            public Vector3 Center;
            public float Radius;
            public float MinY;
            public float MaxY;
        }
        public static void RenderOverlay(RenderManager.CameraInfo cameraInfo, UiServices services)
        {
            if (services == null || !services.IsFeatureActive)
                return;

            if (!services.TryGetSelectedBuilding(out ushort buildingId, out _))
                return;

            ParkingRulesConfigDefinition rule;
            if (services.ParkingRulePreviewState != null
                && services.ParkingRulePreviewState.TryGetPreview(buildingId, out var previewRule))
            {
                rule = previewRule;
            }
            else if (services.ParkingRulesConfigRegistry == null
                || !services.ParkingRulesConfigRegistry.TryGet(buildingId, out rule))
            {
                return;
            }
            
            if (!services.TryGetBuildingPosition(buildingId, out Vector3 center))
                return;

            if (cameraInfo == null)
                return;

            float minY = OverlayUiValues.RadiusOverlay.MinOverlayY;
            float maxY = OverlayUiValues.RadiusOverlay.MaxOverlayY;

            float residentsHue = 0.35f;
            float workSchoolHue = 0.1f;
            if (services.Settings != null)
            {
                residentsHue = services.Settings.ResidentsRadiusHue;
                workSchoolHue = services.Settings.WorkSchoolRadiusHue;
            }

            if (rule.ResidentsWithinRadiusOnly)
            {
                Color color = ColorConversion.FromHue(residentsHue, OverlayUiValues.RadiusOverlay.OverlayAlpha);
                if (rule.ResidentsRadiusMeters == ushort.MaxValue)
                {
                    DrawFullMapOverlay(cameraInfo, color, minY, maxY);
                }
                else
                {
                    DrawCircle(new CircleOverlay
                    {
                        CameraInfo = cameraInfo,
                        Color = color,
                        Center = center,
                        Radius = rule.ResidentsRadiusMeters,
                        MinY = minY,
                        MaxY = maxY
                    });
                }
            }

            if (rule.WorkSchoolWithinRadiusOnly)
            {
                Color color = ColorConversion.FromHue(workSchoolHue, OverlayUiValues.RadiusOverlay.OverlayAlpha);
                if (rule.WorkSchoolRadiusMeters == ushort.MaxValue)
                {
                    DrawFullMapOverlay(cameraInfo, color, minY, maxY);
                }
                else
                {
                    DrawCircle(new CircleOverlay
                    {
                        CameraInfo = cameraInfo,
                        Color = color,
                        Center = center,
                        Radius = rule.WorkSchoolRadiusMeters,
                        MinY = minY,
                        MaxY = maxY
                    });
                }
            }
        }

        private static Quad3 BuildMapQuad()
        {
            float half = OverlayUiValues.RadiusOverlay.MapSizeMeters * 0.5f;
            return new Quad3(
                new Vector3(-half, 0f, -half),
                new Vector3(half, 0f, -half),
                new Vector3(half, 0f, half),
                new Vector3(-half, 0f, half));
        }

        private static void DrawCircle(CircleOverlay overlay)
        {
            if (overlay.Radius <= 0f)
                return;

            float size = overlay.Radius * 2f;
            Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(
                overlay.CameraInfo,
                overlay.Color,
                overlay.Center,
                size,
                overlay.MinY,
                overlay.MaxY,
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





