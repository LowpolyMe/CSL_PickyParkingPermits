using System.Collections.Generic;
using PickyParking.Domain;

namespace PickyParking.Infrastructure.Persistence
{
    
    
    
    public sealed class ParkingRestrictionsConfigRegistry
    {
        private readonly Dictionary<ushort, ParkingRestrictionsConfigDefinition> _rulesByBuildingId = new Dictionary<ushort, ParkingRestrictionsConfigDefinition>();

        public bool TryGet(ushort buildingId, out ParkingRestrictionsConfigDefinition rule)
            => _rulesByBuildingId.TryGetValue(buildingId, out rule);

        public void Set(ushort buildingId, ParkingRestrictionsConfigDefinition rule)
            => _rulesByBuildingId[buildingId] = rule;

        public void Remove(ushort buildingId)
            => _rulesByBuildingId.Remove(buildingId);

        public void Clear()
            => _rulesByBuildingId.Clear();

        public IEnumerable<KeyValuePair<ushort, ParkingRestrictionsConfigDefinition>> Enumerate()
            => _rulesByBuildingId;
    }
}