using ColossalFramework.Packaging;
using PickyParking.Features.ParkingLotPrefabs;

namespace PickyParking.Features.ParkingLotPrefabs
{
    public static class ParkingLotPrefabKeyFactory
    {
        public static PrefabKey CreateKey(global::BuildingInfo prefab)
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
