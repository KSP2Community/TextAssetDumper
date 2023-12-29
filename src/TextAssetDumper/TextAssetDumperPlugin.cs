using System.Collections;
using System.Reflection;
using BepInEx;
using JetBrains.Annotations;
using KSP.Game;
using SpaceWarp;
using SpaceWarp.API.Mods;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TextAssetDumper;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
public class TextAssetDumperPlugin : BaseSpaceWarpPlugin
{
    [PublicAPI] public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    [PublicAPI] public const string ModName = MyPluginInfo.PLUGIN_NAME;
    [PublicAPI] public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

    public override void OnInitialized()
    {
        base.OnInitialized();
        SpaceWarp.API.UI.MainMenu.RegisterMenuButton("Dump", Dump);
    }

    private static bool IsUsefulKey(string key)
    {
        key = key.Replace(".bundle", "").Replace(".json", "");
        if (int.TryParse(key, out _))
        {
            return false;
        }

        if (key.Length == 32)
        {
            return !key.All(x => "0123456789abcdef".Contains(x));
        }

        return true;
    }

    private static AsyncOperationHandle<IList<TextAsset>> DumpLabel(DirectoryInfo parent, string label)
    {
        var archiveFiles = new Dictionary<string, string>();

        var unchanged = true;


        var handle = Addressables.LoadAssetsAsync<TextAsset>(label, asset =>
        {
            archiveFiles[asset.name] = asset.text;
            unchanged = false;
        });

        handle.Completed += results =>
        {
            if (results.Status != AsyncOperationStatus.Succeeded || unchanged)
            {
                return;
            }

            var archive = parent.CreateSubdirectory(label.Replace("/", "_"));
            foreach (var archiveFile in archiveFiles)
            {
                File.WriteAllText(Path.Combine(archive.FullName, archiveFile.Key), archiveFile.Value);
            }
        };

        return handle;
    }

    private static void Dump()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var folder = new FileInfo(assemblyLocation).Directory!;
        if (Directory.Exists(Path.Combine(folder.FullName, "dump")))
        {
            Directory.Delete(Path.Combine(folder.FullName, "dump"), true);
        }

        folder.CreateSubdirectory(Path.Combine("dump", "localizations"));

        for (int i = 0; i < I2.Loc.LocalizationManager.Sources.Count; i++)
        {
            var filename = $"loc_source_{i}.csv";
            var text = I2.Loc.LocalizationManager.Sources[i].Export_CSV();
            File.WriteAllText(Path.Combine(folder.FullName, "dump", "localizations", filename), text);
        }

        var textAssetsDirectory = folder.CreateSubdirectory(Path.Combine("dump", "text_assets"));
        var keys = GameManager.Instance.Assets._registeredResourceLocators
            .SelectMany(locator => locator.Keys)
            .ToList();

        keys.AddRange(Addressables.ResourceLocators.SelectMany(locator => locator.Keys));

        var distinctKeys = keys.Select(key => key.ToString())
            .Distinct()
            .Where(IsUsefulKey)
            .ToList();

        var allHandles = new List<AsyncOperationHandle<IList<TextAsset>>>();
        foreach (var key in distinctKeys)
        {
            allHandles.Add(DumpLabel(textAssetsDirectory, key));
        }

        CoroutineUtil.Instance.StartCoroutine(WaitForDump(allHandles));
    }



    private static IEnumerator WaitForDump(List<AsyncOperationHandle<IList<TextAsset>>> handles)
    {
        foreach (var handle in handles)
        {
            if (!handle.IsDone)
            {
                yield return null;
            }
        }
    }
}