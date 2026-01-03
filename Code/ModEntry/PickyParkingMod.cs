using ICities;
using UnityEngine;
using PickyParking.UI;
using PickyParking.Settings;

namespace PickyParking.ModEntry
{
    
    
    
    
    public sealed class PickyParkingMod : IUserMod
    {
        public string Name => "Picky Parking";
        public string Description => "Restrict who may use parking lots via radius-based permits (TM:PE realistic parking required).";

        public void OnSettingsUI(UIHelperBase helper)
        {
            
            
            var storage = new ModSettingsStorage();
            var controller = ModSettingsController.Load(storage);
            var services = new UiServices(ModRuntime.Current, controller);
            OptionsUI.Build(helper, controller.Current, () => controller.Save("OptionsUI"), services);
        }
    }
}

