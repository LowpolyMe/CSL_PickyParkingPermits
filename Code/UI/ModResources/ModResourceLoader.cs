using System;
using System.IO;
using System.Reflection;
using ColossalFramework.Plugins;
using UnityEngine;
using PickyParking.Logging;

namespace PickyParking.UI.ModResources
{
    
    
    
    public static class ModResourceLoader
    {
        private const string ResourcesFolderName = "Resources";

        public static Texture2D LoadTexture(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            string path = TryGetResourcePath(fileName);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Log.Warn("[Resources] Missing texture file: " + fileName);
                return null;
            }

            try
            {
                byte[] data = File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                texture.LoadImage(data);
                texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
                return texture;
            }
            catch (Exception ex)
            {
                Log.Warn("[Resources] Failed to load texture '" + fileName + "': " + ex.Message);
                return null;
            }
        }

        private static string TryGetResourcePath(string fileName)
        {
            string modPath = TryGetModPath();
            if (string.IsNullOrEmpty(modPath))
                return null;

            string resourcesDir = Path.Combine(modPath, ResourcesFolderName);
            return Path.Combine(resourcesDir, fileName);
        }

        private static string TryGetModPath()
        {
            var pluginManager = PluginManager.instance;
            if (pluginManager == null)
                return null;

            Assembly targetAssembly = typeof(ModResourceLoader).Assembly;

            foreach (PluginManager.PluginInfo plugin in pluginManager.GetPluginsInfo())
            {
                var assemblies = plugin.GetAssemblies();
                if (assemblies == null)
                    continue;

                for (int i = 0; i < assemblies.Count; i++)
                {
                    if (assemblies[i] == targetAssembly)
                        return plugin.modPath;
                }
            }

            return null;
        }
    }
}
