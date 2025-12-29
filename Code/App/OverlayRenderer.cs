using ColossalFramework;
using PickyParking.UI;

namespace PickyParking.App
{
    
    
    
    public static class OverlayRenderer
    {
        public static void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            RadiusOverlayRenderer.RenderOverlay(cameraInfo);
        }
    }
}
