using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

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
        Harmony.CreateAndPatchAll(typeof(Patch02));
    }
}

[HarmonyPatch(typeof(AirportCheckInKiosk), nameof(AirportCheckInKiosk.HoverEnter), MethodType.Normal)]
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
    static void Prefix(EmoteWheelData emoteWheelData, EmoteWheel __instance)
    {
        Plugin.Logger.LogInfo($"Choose EmoteWheel {emoteWheelData.emoteName}");
        Plugin.Logger.LogInfo($"Choose EmoteWheel {emoteWheelData.emoteName}");
        var players = PlayerHandler.GetAllPlayers();
        foreach (var player in players)
        {
            player.character.transform.position += Vector3.up * 10;
            //player.character.refs.animations.PlaySpecificAnimation(emoteWheelData.anim);
        }
    }
}