using System.Collections.Generic;
using System.Linq;

namespace PickyParking.Domain
{
    
    
    
    public sealed class SupportedParkingLotRegistry
    {
        private readonly HashSet<PrefabKey> _keys;

        public SupportedParkingLotRegistry(IEnumerable<PrefabKey> initialKeys)
        {
            _keys = new HashSet<PrefabKey>(initialKeys ?? Enumerable.Empty<PrefabKey>());
        }

        public bool Contains(PrefabKey key) => _keys.Contains(key);

        public bool Add(PrefabKey key) => _keys.Add(key);

        public bool Remove(PrefabKey key) => _keys.Remove(key);

        public bool Toggle(PrefabKey key)
        {
            if (_keys.Remove(key)) return false;
            _keys.Add(key);
            return true;
        }

        public IEnumerable<PrefabKey> EnumerateKeys() => _keys;
    }
}



