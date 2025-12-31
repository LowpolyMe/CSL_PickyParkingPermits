using System;
using PickyParking.Features.ParkingRules;

namespace PickyParking.UI
{
    internal sealed class ParkingRulesConfigPanelView
    {
        public PickyParkingPanelVisuals Visuals { get; private set; }
        public ParkingRulesSliderRow ResidentsRow { get; private set; }
        public ParkingRulesSliderRow WorkSchoolRow { get; private set; }
        public ParkingRulesToggleRow VisitorsRow { get; private set; }

        public static ParkingRulesConfigPanelView Build(
            ParkingRulesConfigPanel panel,
            ParkingPanelTheme theme,
            ParkingRulesConfigUiConfig uiConfig,
            Func<float> getDefaultSliderValue,
            Action onToggleRestrictions,
            Action<ParkingRulesSliderRow> onToggleSlider,
            Action<ParkingRulesSliderRow, float> onSliderValueChanged,
            Action onToggleVisitors,
            Action onApplyChanges)
        {
            var view = new ParkingRulesConfigPanelView();
            view.Visuals = new PickyParkingPanelVisuals(
                panel,
                theme,
                uiConfig.SliderMinValue,
                uiConfig.SliderMaxValue,
                uiConfig.SliderStep,
                getDefaultSliderValue,
                uiConfig.DistanceSliderMinValue,
                uiConfig.DistanceSliderMaxValue,
                ParkingRulesLimits.MinRadiusMeters,
                ParkingRulesLimits.MidRadiusMeters,
                ParkingRulesLimits.MaxRadiusMeters,
                onToggleRestrictions,
                onToggleSlider,
                onSliderValueChanged,
                onToggleVisitors,
                onApplyChanges);

            view.Visuals.ConfigurePanel();
            view.Visuals.BuildUi();
            view.ResidentsRow = view.Visuals.ResidentsRow;
            view.WorkSchoolRow = view.Visuals.WorkSchoolRow;
            view.VisitorsRow = view.Visuals.VisitorsRow;

            return view;
        }
    }
}
