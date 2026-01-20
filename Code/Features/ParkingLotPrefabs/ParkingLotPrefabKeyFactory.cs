using System.Collections.Generic;
using ColossalFramework.Packaging;
using PickyParking.Features.ParkingLotPrefabs;

namespace PickyParking.Features.ParkingLotPrefabs
{
    public static class ParkingLotPrefabKeyFactory
    {
        private static readonly object CacheLock = new object();
        private static Dictionary<global::BuildingInfo, PrefabKey> _keyCache =
            new Dictionary<global::BuildingInfo, PrefabKey>();

        public static PrefabKey CreateKey(global::BuildingInfo prefab)
        {
            if (prefab == null)
            {
                return new PrefabKey(string.Empty, string.Empty);
            }

            lock (CacheLock)
            {
                if (_keyCache.TryGetValue(prefab, out var cached))
                    return cached;

                var created = CreateKeyUncached(prefab);
                _keyCache[prefab] = created;
                return created;
            }
        }

        public static void ClearCache()
        {
            lock (CacheLock)
            {
                _keyCache.Clear();
            }
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
