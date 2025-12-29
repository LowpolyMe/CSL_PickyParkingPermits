using System.Collections.Generic;
using PickyParking.Features.ParkingRules;

namespace PickyParking.Infrastructure.Persistence
{
    public sealed class ParkingRulesConfigRegistry
    {
        private readonly Dictionary<ushort, ParkingRulesConfigDefinition> _rulesByBuildingId =
            new Dictionary<ushort, ParkingRulesConfigDefinition>();

        public bool TryGet(ushort buildingId, out ParkingRulesConfigDefinition rule)
            => _rulesByBuildingId.TryGetValue(buildingId, out rule);

        public void Set(ushort buildingId, ParkingRulesConfigDefinition rule)
            => _rulesByBuildingId[buildingId] = rule;

        public void Remove(ushort buildingId)
            => _rulesByBuildingId.Remove(buildingId);

        public void Clear()
            => _rulesByBuildingId.Clear();

        public IEnumerable<KeyValuePair<ushort, ParkingRulesConfigDefinition>> Enumerate()
            => _rulesByBuildingId;
    }
}
