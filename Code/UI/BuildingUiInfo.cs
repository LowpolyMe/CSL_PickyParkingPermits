using PickyParking.Features.ParkingLotPrefabs;

namespace PickyParking.UI
{
    public readonly struct BuildingUiInfo
    {
        public string PrefabName { get; }
        public PrefabKey PrefabKey { get; }
        public bool HasPrefabKey { get; }

        public BuildingUiInfo(string prefabName, PrefabKey prefabKey)
        {
            PrefabName = prefabName;
            PrefabKey = prefabKey;
            HasPrefabKey = !string.IsNullOrEmpty(prefabKey.PrefabName);
        }
    }
}
