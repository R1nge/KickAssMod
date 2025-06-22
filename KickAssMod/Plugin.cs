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
    internal static new ManualLogSource Logger;

    static void Prefix(AirportCheckInKiosk __instance)
    {
        Logger.LogInfo("HoverEnter");
    }
}