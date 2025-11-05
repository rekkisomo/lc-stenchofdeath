using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace StenchofDeath;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class StenchofDeath : BaseUnityPlugin
{
    public static StenchofDeath Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        Patch();

        Logger.LogInfo($"{PluginInfo.PLUGIN_GUID} v{PluginInfo.PLUGIN_VERSION} making bodies extra stinky");
        
        Logger.LogInfo("Loading Fly Asset...");
        
        // I'm too lazy to make a proper asset bundle, and the original ONLY contains fly-related stuff.
        // @TODO: Don't use asset bundles. Pretty sure LC has fly particles already? Or is that only v73?
        var assetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "symbiosis"));
        Assets.FlyPrefab = assetBundle.LoadAsset<GameObject>("assets/symbiosis/prefabs/stenchofdeath.prefab");
        
        Logger.LogInfo("Finished Loading!");
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(PluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }
}