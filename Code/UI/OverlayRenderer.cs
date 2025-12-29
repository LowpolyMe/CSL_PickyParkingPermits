using ColossalFramework;

namespace PickyParking.UI
{
    public static class OverlayRenderer
    {
        public static void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            RadiusOverlayRenderer.RenderOverlay(cameraInfo);
        }
    }
}
