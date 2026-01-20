using System.Runtime.CompilerServices;
using ColossalFramework.Packaging;
using PickyParking.Features.ParkingLotPrefabs;

namespace PickyParking.Features.ParkingLotPrefabs
{
    public static class ParkingLotPrefabKeyFactory
    {
        private sealed class PrefabKeyHolder
        {
            public readonly PrefabKey Key;

            public PrefabKeyHolder(PrefabKey key)
            {
                Key = key;
            }
        }

        private static ConditionalWeakTable<global::BuildingInfo, PrefabKeyHolder> _keyCache
            = new ConditionalWeakTable<global::BuildingInfo, PrefabKeyHolder>();

        public static PrefabKey CreateKey(global::BuildingInfo prefab)
        {
            if (prefab == null)
            {
                return new PrefabKey(string.Empty, string.Empty);
            }

            lock (CacheLock)
            {
                PrefabKey cached;
                if (_keyCache.TryGetValue(prefab, out cached))
                    return cached;

                PrefabKey created = CreateKeyUncached(prefab);
                _keyCache[prefab] = created;
                return created;
            }
        }

        public static void ClearCache()
        {
            _keyCache = new ConditionalWeakTable<global::BuildingInfo, PrefabKeyHolder>();
        }

        private static PrefabKeyHolder CreateKeyHolder(global::BuildingInfo prefab)
        {
            return new PrefabKeyHolder(CreateKeyUncached(prefab));
        }

        private static PrefabKey CreateKeyUncached(global::BuildingInfo prefab)
        {
            string prefabName = (prefab != null) ? prefab.name : string.Empty;

            string packageName = string.Empty;
            var asset = PackageManager.FindAssetByName(prefabName);
            if (asset != null && asset.package != null)
            {
                packageName = asset.package.packageName ?? string.Empty;
            }

            return new PrefabKey(packageName, prefabName);
        }
    }
}
