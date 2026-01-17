using ICities;
using UnityEngine;
using PickyParking.UI;
using PickyParking.Settings;
using PickyParking.UI.ModOptions;

namespace PickyParking.ModEntry
{
    
    
    
    
    public sealed class PickyParkingMod : IUserMod
    {
        public string Name => "Picky Parking";
        public string Description => "Restrict who may use parking lots via radius-based permits (TM:PE realistic parking required).";

        public void OnSettingsUI(UIHelperBase helper)
        {
            
            
            var runtimeController = ModRuntime.Current != null ? ModRuntime.Current.SettingsController : null;
            ModSettingsController controller;
            if (runtimeController != null)
            {
                controller = runtimeController;
            }
            else
            {
                var storage = new ModSettingsStorage();
                controller = ModSettingsController.Load(storage);
                controller.Reload("OptionsUI.Open");
            }
            var services = new UiServices(() => ModRuntime.Current, controller);
            if (ModRuntime.Current != null && ModRuntime.Current.ParkingBackendState != null)
            {
                ModRuntime.Current.ParkingBackendState.Refresh();
            }
            OptionsUI.Build(helper, controller.Current, () => controller.Save("OptionsUI"), services);
        }
    }
}


