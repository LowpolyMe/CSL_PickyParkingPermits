using ColossalFramework.Packaging;
using PickyParking.Domain;

namespace PickyParking.Infrastructure.Integration
{
    
    
    
    public sealed class PrefabIdentity
    {
        public PrefabKey CreateKey(global::BuildingInfo prefab)
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

        public global::BuildingInfo Resolve(PrefabKey key)
        {
            
            return null;
        }
    }
}



