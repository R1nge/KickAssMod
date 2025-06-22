using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace KickAssMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Harmony.CreateAndPatchAll(typeof(Patch01));
    }
}

[HarmonyPatch(typeof(AirportCheckInKiosk), MethodType.Normal)]
[HarmonyPatch("HoverEnter")]
internal class Patch01
{
    static void Prefix(AirportCheckInKiosk __instance)
    {
        Plugin.Logger.LogInfo("HoverEnter");
    }
}

[HarmonyPatch(typeof(EmoteWheel), nameof(EmoteWheel.Hover), MethodType.Normal)]
internal class Patch02
{
    static void Prefix(EmoteWheel __instance, EmoteWheelData ___data)
    {
        Plugin.Logger.LogInfo($"Choose EmoteWheel {___data.emoteName}");
    }
}