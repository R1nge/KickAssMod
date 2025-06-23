using System;
using System.Collections;
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
        Harmony.CreateAndPatchAll(typeof(KickAss));
        //Harmony.CreateAndPatchAll(typeof(FreezeTimeOnPause));
        //Harmony.CreateAndPatchAll(typeof(UnfreezeTimeUnpause));
        //Harmony.CreateAndPatchAll(typeof(UnfreezeTimeLeaveLobby));
        //Harmony.CreateAndPatchAll(typeof(RemoveLoudSoundUI));
    }
}

[HarmonyPatch(typeof(GUIManager), nameof(GUIManager.TriggerMenuWindowOpened), MethodType.Normal)]
internal class FreezeTimeOnPause
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

[HarmonyPatch(typeof(SFX_Player), nameof(SFX_Player.PlaySFX), MethodType.Normal)]
internal class RemoveLoudSoundUI
{
    static bool Prefix(SFX_Player ___instance, SFX_Instance SFX, Vector3 position, Transform followTransform = null,
        SFX_Settings overrideSettings = null, float volumeMultiplier = 1f, bool loop = false)
    {
        Plugin.Logger.LogInfo($"Play sfx {SFX.name}");
        if (SFX.name is "SFXI UI Titlescreen button wood 1" or "SFXI UI Titlescreen button generic"
            or "SFXI UI Titlescreen button wood 2" or "SFXI UI Titlescreen button wood 3"
            or "SFXI UI Titlescreen button wood 4" or "SFXI UI Titlescreen button wood 5")
        {
            Plugin.Logger.LogInfo("Muting Loud UI sound");
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(GUIManager), nameof(GUIManager.TriggerMenuWindowClosed), MethodType.Normal)]
internal class UnfreezeTimeUnpause
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
internal class UnfreezeTimeLeaveLobby
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
            if (!player.TryGetComponent(out KickMono kickMono))
            {
                player.gameObject.AddComponent<KickMono>();
            }
        }

        Plugin.Logger.LogInfo("Local player");
        var rayDirection = MainCamera.instance.transform.forward;
        var rayOrigin = MainCamera.instance.transform.position;
        var ray = new Ray(rayOrigin, rayDirection);
        if (Physics.Raycast(ray, out var hit, float.MaxValue, (LayerMask)LayerMask.GetMask("Character"),
                QueryTriggerInteraction.Collide))
        {
            Plugin.Logger.LogInfo($"{hit.collider.name}");
            Plugin.Logger.LogInfo($"{hit.point}");
            Plugin.Logger.LogInfo($"{hit.distance}");

            var char2 = hit.collider.GetComponentInParent<Character>();
            if (char2 != null)
            {
                Plugin.Logger.LogInfo($"Char2 {char2.name}: {hit.collider.name}");
                char2.data.avarageLastFrameVelocity = rayDirection * 100000f;
                char2.data.avarageVelocity = rayDirection * 1000000f;
                foreach (Bodypart bodypart in char2.refs.ragdoll.partList)
                {
                    bodypart.Rig.isKinematic = false;
                    bodypart.Rig.AddForce(rayDirection * 10000f, ForceMode.Acceleration);
                    bodypart.AddForce(rayDirection * 10000f, ForceMode.Acceleration);
                }
            }
        }
        else
        {
            Plugin.Logger.LogInfo("No collision at all");
        }
    }
}

[HarmonyPatch(typeof(EmoteWheel), nameof(EmoteWheel.Hover), MethodType.Normal)]
internal class KickAss
{
    static void Prefix(EmoteWheelData emoteWheelData, EmoteWheel __instance)
    {
        __instance.StartCoroutine(Cor(emoteWheelData));
    }

    static IEnumerator Cor(EmoteWheelData emoteWheelData)
    {
        yield return new WaitForFixedUpdate();
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