using System;

namespace PickyParking.Features.Debug
{
    [Flags]
    public enum DebugLogCategory
    {
        None = 0, //  "uncategorized", always allowed (still gated by verbose).

        RuleUi = 1 << 0,
        LotInspection = 1 << 1,
        DecisionPipeline = 1 << 2,
        Enforcement = 1 << 3,
        Tmpe = 1 << 4,

        All = RuleUi | LotInspection | DecisionPipeline | Enforcement | Tmpe 
    }
}
