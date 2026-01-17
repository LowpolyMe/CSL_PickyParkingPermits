using System;
using System.Collections.Generic;

namespace PickyParking.Features.ParkingRules
{
    public sealed class ParkingRulesConfigRegistry
    {
        private readonly Dictionary<ushort, ParkingRulesConfigDefinition> _rulesByBuildingId =
            new Dictionary<ushort, ParkingRulesConfigDefinition>();
        private int _version;

        public int Version => _version;

        public bool TryGet(ushort buildingId, out ParkingRulesConfigDefinition rule)
            => _rulesByBuildingId.TryGetValue(buildingId, out rule);

        public void Set(ushort buildingId, ParkingRulesConfigDefinition rule)
        {
            _rulesByBuildingId[buildingId] = rule;
            _version++;
        }

        public void Remove(ushort buildingId)
        {
            if (_rulesByBuildingId.Remove(buildingId))
                _version++;
        }

        public void Clear()
        {
            if (_rulesByBuildingId.Count == 0)
                return;

            _rulesByBuildingId.Clear();
            _version++;
        }

        public IEnumerable<KeyValuePair<ushort, ParkingRulesConfigDefinition>> Enumerate()
            => _rulesByBuildingId;

        public bool RemoveIf(Func<KeyValuePair<ushort, ParkingRulesConfigDefinition>, bool> predicate)
        {
            if (predicate == null)
                return false;

            bool removedAny = false;
            var toRemove = new List<ushort>();
            foreach (var kvp in _rulesByBuildingId)
            {
                if (predicate(kvp))
                    toRemove.Add(kvp.Key);
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                if (_rulesByBuildingId.Remove(toRemove[i]))
                    removedAny = true;
            }

            if (removedAny)
                _version++;

            return removedAny;
        }
    }
}
