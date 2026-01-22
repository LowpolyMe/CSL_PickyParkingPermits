using ColossalFramework.UI;
using PickyParking.Features.Debug;
using UnityEngine;
using PickyParking.Logging;
using PickyParking.UI.ModResources;

namespace PickyParking.UI.BuildingOptionsPanel.ParkingRulesPanel
{
    internal static class ParkingRulesIconAtlas
    {
        private struct SpriteSpec
        {
            public string Name;
            public int X;
            public int Y;
            public int Width;
            public int Height;
        }
        private static UITextureAtlas _atlas;
        private static bool _attempted;

        public static UITextureAtlas GetOrCreate()
        {
            if (_atlas != null || _attempted)
                return _atlas;

            _attempted = true;

            Texture2D texture = ModResourceLoader.LoadTexture(ParkingRulesIconAtlasUiValues.TextureFileName);
            if (texture == null)
            {
                Log.Player.Warn(DebugLogCategory.RuleUi, LogPath.Any, "IconAtlasTextureMissing", "fileName=" + ParkingRulesIconAtlasUiValues.TextureFileName);
                return null;
            }

            var atlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            atlas.name = "PickyParkingIconsAtlas";
            atlas.material = new Material(Shader.Find("UI/Default UI Shader"))
            {
                mainTexture = texture
            };

            int iconWidth = texture.width / 4;
            int iconHeight = texture.height / 2;
            int topRowY = iconHeight;
            int bottomRowY = 0;

            AddSprite(atlas, texture, new SpriteSpec
            {
                Name = ParkingRulesIconAtlasUiValues.ResidentsSpriteName,
                X = 0,
                Y = topRowY,
                Width = iconWidth,
                Height = iconHeight
            });
            AddSprite(atlas, texture, new SpriteSpec
            {
                Name = ParkingRulesIconAtlasUiValues.WorkSchoolSpriteName,
                X = iconWidth,
                Y = topRowY,
                Width = iconWidth,
                Height = iconHeight
            });
            AddSprite(atlas, texture, new SpriteSpec
            {
                Name = ParkingRulesIconAtlasUiValues.VisitorsSpriteName,
                X = iconWidth * 2,
                Y = topRowY,
                Width = iconWidth,
                Height = iconHeight
            });
            AddSprite(atlas, texture, new SpriteSpec
            {
                Name = ParkingRulesIconAtlasUiValues.CrossedOutSpriteName,
                X = iconWidth * 3,
                Y = topRowY,
                Width = iconWidth,
                Height = iconHeight
            });
            AddSprite(atlas, texture, new SpriteSpec
            {
                Name = ParkingRulesIconAtlasUiValues.CopySpriteName,
                X = 0,
                Y = bottomRowY,
                Width = iconWidth,
                Height = iconHeight
            });
            AddSprite(atlas, texture, new SpriteSpec
            {
                Name = ParkingRulesIconAtlasUiValues.PasteSpriteName,
                X = iconWidth,
                Y = bottomRowY,
                Width = iconWidth,
                Height = iconHeight
            });
            AddSprite(atlas, texture, new SpriteSpec
            {
                Name = ParkingRulesIconAtlasUiValues.ResetSpriteName,
                X = iconWidth * 2,
                Y = bottomRowY,
                Width = iconWidth,
                Height = iconHeight
            });
            AddSprite(atlas, texture, new SpriteSpec
            {
                Name = ParkingRulesIconAtlasUiValues.ApplySpriteName,
                X = iconWidth * 3,
                Y = bottomRowY,
                Width = iconWidth,
                Height = iconHeight
            });
            _atlas = atlas;
            return _atlas;
        }

        public static void ClearCache()
        {
            if (_atlas == null && !_attempted)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                {
                    Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "IconAtlasCleanupSkippedNeverCreated");
                }
                return;
            }

            var atlas = _atlas;
            _atlas = null;
            _attempted = false;

            if (atlas == null)
            {
                if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
                {
                    Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "IconAtlasCacheReset");
                }
                return;
            }

            if (atlas.material != null)
            {
                var texture = atlas.material.mainTexture;
                Object.Destroy(atlas.material);
                if (texture != null)
                    Object.Destroy(texture);
            }

            Object.Destroy(atlas);
            if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
            {
                Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "IconAtlasCacheCleared");
            }
        }

        private static void AddSprite(UITextureAtlas atlas, Texture2D texture, SpriteSpec spec)
        {
            float texWidth = texture.width;
            float texHeight = texture.height;

            var info = new UITextureAtlas.SpriteInfo
            {
                name = spec.Name,
                texture = texture,
                region = new Rect(
                    spec.X / texWidth,
                    spec.Y / texHeight,
                    spec.Width / texWidth,
                    spec.Height / texHeight)
            };

            atlas.AddSprite(info);
        }
    }
}









