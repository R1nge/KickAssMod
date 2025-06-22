using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Zorro.Core;

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
        if (emoteWheelData.emoteName == "Shrug")
        {
            Plugin.Logger.LogInfo("Kick");
            var players = PlayerHandler.GetAllPlayers();
            foreach (var player in players)
            {
                if (player.character.IsLocal)
                {
                    Plugin.Logger.LogInfo("Local player");
                    var rayDirection = MainCamera.instance.transform.forward;
                    var rayOrigin = player.character.transform.position;
                    var ray = new Ray(rayOrigin, rayDirection);
                    if (Physics.Raycast(ray, out var hit, 10f))
                    {
                        Plugin.Logger.LogInfo($"{player.character.name}: {hit.collider.name}");
                        Plugin.Logger.LogInfo($"{player.character.name}: {hit.point}");
                        Plugin.Logger.LogInfo($"{player.character.name}: {hit.distance}");
                        if (hit.collider.TryGetComponent(out CharacterRagdoll character))
                        {
                            foreach (Bodypart bodypart in character.partList)
                            {
                                bodypart.AddForce(rayDirection * 10000f, ForceMode.Acceleration);
                            }
                        }
                        else
                        {
                            Plugin.Logger.LogInfo("No collision");
                        }
                    }
                    else
                    {
                        Plugin.Logger.LogInfo("No collision");
                    }
                }
            }
        }
    }
}