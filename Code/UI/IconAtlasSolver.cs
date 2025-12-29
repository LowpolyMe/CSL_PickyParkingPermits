using ColossalFramework.UI;
using UnityEngine;
using PickyParking.Infrastructure;

namespace PickyParking.UI
{
    internal static class ParkingPermitsIconAtlas
    {
        public const string ResidentsSpriteName = "ResidentsIcon";
        public const string WorkSchoolSpriteName = "WorkSchoolIcon";
        public const string VisitorsSpriteName = "VisitorsIcon";
        public const string CrossedOutSpriteName = "CrossedIcon";
        
        private const string TextureFileName = "IconsAtlas.png";

        private static UITextureAtlas _atlas;
        private static bool _attempted;

        public static UITextureAtlas GetOrCreate()
        {
            if (_atlas != null || _attempted)
                return _atlas;

            _attempted = true;

            Texture2D texture = ModResourceLoader.LoadTexture(TextureFileName);
            if (texture == null)
            {
                Log.Warn("[UI] IconsAtlas texture not found in mod Resources.");
                return null;
            }

            var atlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            atlas.name = "PickyParkingIconsAtlas";
            atlas.material = new Material(Shader.Find("UI/Default UI Shader"))
            {
                mainTexture = texture
            };

            int iconWidth = texture.width / 4;
            int iconHeight = texture.height;

            AddSprite(atlas, ResidentsSpriteName, 0, 0, iconWidth, iconHeight, texture);
            AddSprite(atlas, WorkSchoolSpriteName, iconWidth, 0, iconWidth, iconHeight, texture);
            AddSprite(atlas, VisitorsSpriteName, iconWidth * 2, 0, iconWidth, iconHeight, texture);
            AddSprite(atlas, CrossedOutSpriteName, iconWidth * 3, 0, iconWidth, iconHeight, texture);
            _atlas = atlas;
            return _atlas;
        }

        private static void AddSprite(
            UITextureAtlas atlas,
            string name,
            int x,
            int y,
            int width,
            int height,
            Texture2D texture)
        {
            float texWidth = texture.width;
            float texHeight = texture.height;

            var info = new UITextureAtlas.SpriteInfo
            {
                name = name,
                texture = texture,
                region = new Rect(x / texWidth, y / texHeight, width / texWidth, height / texHeight)
            };

            atlas.AddSprite(info);
        }
    }
}
