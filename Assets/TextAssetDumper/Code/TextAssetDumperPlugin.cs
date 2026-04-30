using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using KSP.Game;
using SpaceWarp2.API.Mods;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TextAssetDumper
{
    /* Extend KerbalMod instead if you need the MonoBehaviour update loop/references to game stuff like SW 1.x mods */
    public class TextAssetDumperPlugin : GeneralMod
    {
        public override void OnPreInitialized()
        {
            /*
                Code that runs before addressables/assets are loaded goes here
                This is where you want to register loading actions or other such things
            */
        }

        public override void OnInitialized()
        {
            SpaceWarp2.UI.API.MainMenu.RegisterMenuButton("Dump", Dump);
        }

        public override void OnPostInitialized()
        {
            /*
                Code that runs after all mods have been initialized goes here
            */
        }

        private static bool IsUsefulKey(string key)
        {
            key = key.Replace(".bundle", "").Replace(".json", "");
            if (int.TryParse(key, out _))
            {
                return false;
            }

            return key.Length != 32 || key.Any(x => "0123456789abcdef".IndexOf(x) == -1);
        }

        private static AsyncOperationHandle<IList<TextAsset>> DumpLabel(DirectoryInfo parent, string label)
        {
            var archiveFiles = new Dictionary<string, string>();

            bool unchanged = true;


            AsyncOperationHandle<IList<TextAsset>> handle = Addressables.LoadAssetsAsync<TextAsset>(
                label,
                asset =>
                {
                    archiveFiles[asset.name] = asset.text;
                    unchanged = false;
                }
            );

            handle.Completed += results =>
            {
                if (results.Status != AsyncOperationStatus.Succeeded || unchanged)
                {
                    return;
                }

                DirectoryInfo archive = parent.CreateSubdirectory(label.Replace("/", "_"));
                foreach (KeyValuePair<string, string> archiveFile in archiveFiles)
                {
                    File.WriteAllText(Path.Combine(archive.FullName, archiveFile.Key), archiveFile.Value);
                }
            };

            return handle;
        }

        private static void Dump()
        {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            DirectoryInfo folder = new FileInfo(assemblyLocation).Directory!;
            if (Directory.Exists(Path.Combine(folder.FullName, "dump")))
            {
                Directory.Delete(Path.Combine(folder.FullName, "dump"), true);
            }

            folder.CreateSubdirectory(Path.Combine("dump", "localizations"));

            for (int i = 0; i < I2.Loc.LocalizationManager.Sources.Count; i++)
            {
                string filename = $"loc_source_{i}.csv";
                string text = I2.Loc.LocalizationManager.Sources[i].Export_CSV("");
                File.WriteAllText(Path.Combine(folder.FullName, "dump", "localizations", filename), text);
            }

            DirectoryInfo textAssetsDirectory = folder.CreateSubdirectory(Path.Combine("dump", "text_assets"));
            List<object> keys = GameManager.Instance
                .Assets
                .RegisteredResourceLocators
                .SelectMany(locator => locator.Keys)
                .ToList();

            keys.AddRange(Addressables.ResourceLocators.SelectMany(locator => locator.Keys));

            List<string> distinctKeys = keys.Select(key => key.ToString())
                .Distinct()
                .Where(IsUsefulKey)
                .ToList();

            List<AsyncOperationHandle<IList<TextAsset>>> allHandles = distinctKeys
                .Select(key => DumpLabel(textAssetsDirectory, key))
                .ToList();

            CoroutineUtil.Instance.StartCoroutine(WaitForDump(allHandles));
        }

        private static IEnumerator WaitForDump(List<AsyncOperationHandle<IList<TextAsset>>> handles)
        {
            foreach (AsyncOperationHandle<IList<TextAsset>> handle in handles)
            {
                if (!handle.IsDone)
                {
                    yield return null;
                }
            }
        }
    }
}