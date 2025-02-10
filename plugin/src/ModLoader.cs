using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


namespace Stratagem
{
	public class ModLoader
    {
        public static bool modsLoaded = false;

        public static List<string> modDirectories = new List<string>();

        public static List<ModData> mods  = new List<ModData>();
        public delegate void GameStateDelegate();
        public static event GameStateDelegate GameStateEvent;

        [System.Serializable]
        public class ModData
        {
            public string name;
            public string directory;
            public AssetBundle bundle;
            public Stratagem_Package package;
        }

        public static IEnumerator LoadModPackages()
        {
            StratagemPlugin.Logger.LogMessage($"Loading Stratagem Mod Packages");

            modDirectories = Directory.GetFiles(Paths.PluginPath, "*.stratagem", SearchOption.AllDirectories).ToList();

            //Loop through all Mods
            for (int i = 0; i < modDirectories.Count; i++)
            {
                bool continueOut = false;
                //Make sure this mod isn't already loaded
                for (int z = 0; z < mods.Count; z++)
                {
                    if (mods[z].directory == modDirectories[i])
                    {
                        //Debug.Log("Mod Found " + modDirectories[i]);
                        continueOut = true;
                        break;
                    }
                }

                if (continueOut)
                    continue;
                
                StratagemPlugin.Logger.LogMessage($"Loading Stratagem Loading - " + modDirectories[i]);
                ModData newMod = new ModData();
                newMod.directory = modDirectories[i];
                newMod.name = Path.GetFileName(modDirectories[i]);

                AssetBundleCreateRequest asyncBundleRequest
                    = AssetBundle.LoadFromFileAsync(newMod.directory);

                yield return asyncBundleRequest;
                AssetBundle localAssetBundle = asyncBundleRequest.assetBundle;

                if (localAssetBundle == null)
                {
                    StratagemPlugin.Logger.LogError($"Failed to load AssetBundle - " + modDirectories[i]);
                    continue;
                }

                newMod.bundle = localAssetBundle;

                mods.Add(newMod);

                AssetBundleRequest assetRequest = localAssetBundle.LoadAssetWithSubAssetsAsync<Stratagem_Package>("Stratagem");
                yield return assetRequest;

                if (assetRequest == null)
                {
                    Debug.LogError("Map Generator - Missing Package " + modDirectories[i]);
                    continue;
                }

                newMod.package = assetRequest.asset as Stratagem_Package;
                

            }

            StratagemPlugin.Logger.LogMessage($"Stratagem Loaded: " + mods.Count);

            yield return null;

            modsLoaded = true;
        }

        public IEnumerator LoadThemes(string[] loadThemes)
        {
            int mainTexture = 0;
            int setIndex = 0;

            for (int x = 0; x < loadThemes.Length; x++)
            {
                for (int i = 0; i < mods.Count; i++)
                {
                    if (mods[i] == null || mods[i].package == null || loadThemes[x] == "")
                    {
                        continue;
                    }

                    if (mods[i].package.title == loadThemes[x])
                    {
                        //LOAD ALL OUR ASSETS INTO MEMORY

                        yield return null;
                        break;
                    }
                }
            }
        }

        public void SoftUnloadAllMods()
        {
            for (int i = 0; i < mods.Count; i++)
            {
                mods[i].bundle.Unload(false);
            }
        }

        public void UnloadMods(bool unloadAllLoadedObjects)
        {
            for (int i = 0; i < mods.Count; i++)
            {
                mods[i].bundle.Unload(unloadAllLoadedObjects);
            }
        }

        void OnDestroy()
        {
            for (int i = 0; i < mods.Count; i++)
            {
                mods[i].bundle.Unload(true);
            }

            mods.Clear();
        }
    }
}