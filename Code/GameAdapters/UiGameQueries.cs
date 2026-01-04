using ColossalFramework.UI;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.ModEntry;
using PickyParking.UI;
using UnityEngine;

namespace PickyParking.GameAdapters
{
    public sealed class UiGameQueries
    {
        private readonly System.Func<ModRuntime> _runtimeAccessor;

        public UiGameQueries(System.Func<ModRuntime> runtimeAccessor)
        {
            _runtimeAccessor = runtimeAccessor;
        }

        private GameAccess GameAccess => _runtimeAccessor != null ? _runtimeAccessor()?.GameAccess : null;

        public bool IsBuildingManagerReadyForUi()
        {
            var game = GameAccess;
            return game != null && game.IsBuildingManagerReady();
        }

        public bool TryGetSelectedBuilding(out ushort buildingId, out BuildingUiInfo info)
        {
            buildingId = 0;
            info = default;
            var game = GameAccess;
            if (game == null || !game.IsBuildingManagerReady())
                return false;

            if (!game.TryGetSelectedBuilding(out buildingId, out var buildingInfo))
                return false;

            return TryBuildBuildingUiInfo(buildingInfo, out info);
        }

        public bool TryGetBuildingPrefabName(ushort buildingId, out string prefabName)
        {
            prefabName = null;
            if (buildingId == 0)
                return false;

            var game = GameAccess;
            if (game == null || !game.IsBuildingManagerReady())
                return false;

            if (!game.TryGetBuildingInfo(buildingId, out var info))
                return false;

            prefabName = info != null ? info.name : null;
            return !string.IsNullOrEmpty(prefabName);
        }

        public bool TryGetBuildingPosition(ushort buildingId, out Vector3 position)
        {
            position = default(Vector3);
            if (buildingId == 0)
                return false;

            var game = GameAccess;
            if (game == null || !game.IsBuildingManagerReady())
                return false;

            return game.TryGetBuildingPosition(buildingId, out position);
        }

        public bool TryGetParkingSpaceCount(ushort buildingId, out int totalSpaces)
        {
            totalSpaces = 0;
            if (buildingId == 0)
                return false;

            var game = GameAccess;
            if (game == null || !game.IsBuildingManagerReady())
                return false;

            return game.TryGetParkingSpaceCount(buildingId, out totalSpaces);
        }

        public bool TryGetParkingSpaceStats(ushort buildingId, out int totalSpaces, out int occupiedSpaces)
        {
            totalSpaces = 0;
            occupiedSpaces = 0;
            if (buildingId == 0)
                return false;

            var game = GameAccess;
            if (game == null || !game.IsBuildingManagerReady())
                return false;

            return game.TryGetParkingSpaceStats(buildingId, out totalSpaces, out occupiedSpaces);
        }

        public bool IsPrefabCollectionReady()
        {
            return PrefabCollection<BuildingInfo>.LoadedCount() > 0;
        }

        public bool TryGetPrefabDisplayName(string prefabId, out string displayName)
        {
            displayName = null;
            if (string.IsNullOrEmpty(prefabId))
                return false;

            BuildingInfo info = PrefabCollection<BuildingInfo>.FindLoaded(prefabId);
            if (info == null)
                return false;

            string localized = info.GetLocalizedTitle();
            displayName = string.IsNullOrEmpty(localized) ? prefabId : localized;
            return true;
        }

        public bool TryGetPrefabThumbnailSprite(
            string prefabId,
            out UITextureAtlas atlas,
            out string spriteName,
            out string reason)
        {
            atlas = null;
            spriteName = null;
            reason = null;

            if (string.IsNullOrEmpty(prefabId))
            {
                reason = "prefab id is empty";
                return false;
            }

            BuildingInfo info = PrefabCollection<BuildingInfo>.FindLoaded(prefabId);
            if (info == null)
            {
                reason = "prefab not loaded";
                return false;
            }

            atlas = info.m_Atlas;
            spriteName = info.m_Thumbnail;
            if (atlas == null)
            {
                reason = "atlas is null";
                return false;
            }

            if (string.IsNullOrEmpty(spriteName))
            {
                reason = "thumbnail name empty";
                return false;
            }

            if (atlas[spriteName] == null)
            {
                reason = "sprite missing in atlas";
                return false;
            }

            return true;
        }

        private static bool TryBuildBuildingUiInfo(BuildingInfo buildingInfo, out BuildingUiInfo info)
        {
            info = default;
            if (buildingInfo == null)
                return false;

            string prefabName = buildingInfo.name;
            if (string.IsNullOrEmpty(prefabName))
                return false;

            PrefabKey key = ParkingLotPrefabKeyFactory.CreateKey(buildingInfo);
            info = new BuildingUiInfo(prefabName, key);
            return true;
        }
    }
}
