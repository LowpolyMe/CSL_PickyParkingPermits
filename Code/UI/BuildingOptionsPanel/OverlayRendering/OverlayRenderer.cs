using ColossalFramework;
using PickyParking.UI;

namespace PickyParking.UI.BuildingOptionsPanel.OverlayRendering
{
    public static class OverlayRenderer
    {
        private static UiServices _services;

        public static void SetServices(UiServices services)
        {
            _services = services;
        }

        public static void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (_services == null)
                return;

            RadiusOverlayRenderer.RenderOverlay(cameraInfo, _services);
        }
    }
}





