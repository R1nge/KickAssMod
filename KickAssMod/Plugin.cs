using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
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
        Harmony.CreateAndPatchAll(typeof(Patch03));
        Harmony.CreateAndPatchAll(typeof(Patch04));
        Harmony.CreateAndPatchAll(typeof(Patch05));
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

[HarmonyPatch(typeof(GUIManager), nameof(GUIManager.TriggerMenuWindowOpened), MethodType.Normal)]
internal class Patch03
{
    static void Prefix(MenuWindow window, GUIManager __instance)
    {
        if (Character.localCharacter.input.pauseWasPressed)
        {
            Plugin.Logger.LogInfo("Pressed pause");
            if (__instance.wheelActive)
            {
                return;
            }

            if (__instance.endScreen.isOpen)
            {
                return;
            }

            Plugin.Logger.LogInfo("Open pause menu and freeze time");

            Time.timeScale = 0.1f;
            Character.localCharacter.input.pauseWasPressed = false;
        }
    }
}

[HarmonyPatch(typeof(GUIManager), nameof(GUIManager.TriggerMenuWindowClosed), MethodType.Normal)]
internal class Patch04
{
    static void Prefix(MenuWindow window, GUIManager __instance)
    {
        if (Character.localCharacter.input.pauseWasPressed)
        {
            Plugin.Logger.LogInfo("Pressed pause");
            if (!__instance.pauseMenu.isOpen)
            {
                Plugin.Logger.LogInfo("Close pause menu amd resume time");
                Time.timeScale = 1f;
                Character.localCharacter.input.pauseWasPressed = false;
            }
        }
    }
}

[HarmonyPatch(typeof(SteamLobbyHandler), nameof(SteamLobbyHandler.LeaveLobby), MethodType.Normal)]
internal class Patch05
{
    static void Prefix(SteamLobbyHandler __instance)
    {
        Plugin.Logger.LogInfo("LeaveLobby resume time");
        Time.timeScale = 1f;
    }
}


internal class KickMono : MonoBehaviourPun
{
    [PunRPC]
    public void KickRPC()
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
                        foreach (Bodypart bodypart in player.character.refs.ragdoll.partList)
                        {
                            bodypart.AddForce(rayDirection * 10000f, ForceMode.Acceleration);
                        }
                    }
                }
                else
                {
                    Plugin.Logger.LogInfo("No collision");
                    foreach (Bodypart bodypart in player.character.refs.ragdoll.partList)
                    {
                        bodypart.AddForce(rayDirection * 10000f, ForceMode.Acceleration);
                    }
                }
            }
        }
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
            var players = PlayerHandler.GetAllPlayers();
            foreach (var player in players)
            {
                if (!player.TryGetComponent(out KickMono kickMono))
                {
                    player.gameObject.AddComponent<KickMono>();
                }

                if (player.character.IsLocal)
                {
                    Plugin.Logger.LogInfo("Local player");
                    var rayDirection = MainCamera.instance.transform.forward;
                    var rayOrigin = player.character.transform.position;
                    var ray = new Ray(rayOrigin, rayDirection);
                    player.photonView.RPC("KickRPC", RpcTarget.All, Array.Empty<object>());
                }
            }
        }
    }
}