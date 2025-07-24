using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using Zorro.Core;
using Logger = BepInEx.Logging.Logger;

namespace KickAssMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    public static ConfigEntry<string> ConfigKickEmotion;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        ConfigKickEmotion = Config.Bind("Controls", "KickEmotion", "Shrug", "Emote to use for kick");
        Harmony.CreateAndPatchAll(typeof(KickAss));
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
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
    private const float KICK_FORCE = 3000f;
    private const float KICK_RADIUS = 2f;
    private const float KICK_RANGE = 3f;
    private const float UPWARDS_MODIFIER = 0.5f;
    private RaycastHit[] hits = new RaycastHit[100];
    private Dictionary<Character, Coroutine> activeKicks = new Dictionary<Character, Coroutine>();

    [PunRPC]
    public void KickRPC(Vector3 kickOrigin, Vector3 kickDirection, PhotonMessageInfo info)
    {
        // Only process on the master client
        if (!PhotonNetwork.IsMasterClient) return;
        Plugin.Logger.LogInfo("Received kick from " + info.Sender);
        Ray ray = new Ray(kickOrigin, kickDirection);
        var hitCount = Physics.SphereCastNonAlloc(ray, KICK_RADIUS, hits, KICK_RANGE, LayerMask.GetMask("Character"));
        Plugin.Logger.LogInfo($"Kicked times {hitCount} from {info.Sender}");
        for (int i = 0; i < hitCount; i++)
        {
            var hit = hits[i];
            Character hitCharacter = hit.collider.GetComponentInParent<Character>();
            if (hitCharacter == null)
            {
                Plugin.Logger.LogInfo($"No character found at {hit.point}; Skipping");
                continue;
            }

            if (hitCharacter.IsLocal)
            {
                Plugin.Logger.LogInfo($"Character {hitCharacter.name} is local; skipping");
                continue;
            }

            Plugin.Logger.LogInfo($"Kicking {hitCharacter.name}");
            // If this character is already being kicked, skip
            if (activeKicks.ContainsKey(hitCharacter))
            {
                Plugin.Logger.LogInfo($"Character {hitCharacter.name} is already being kicked");
                continue;
            }

            Plugin.Logger.LogInfo($"Kicking {hitCharacter.name}");

            // Start the kick coroutine
            var coroutine = StartCoroutine(ApplyKickForce(hitCharacter, kickOrigin));
            activeKicks[hitCharacter] = coroutine;
        }
    }

    private IEnumerator ApplyKickForce(Character character, Vector3 kickOrigin)
    {
        // Calculate force direction with slight upward angle
        Plugin.Logger.LogInfo($"Applying kick force to {character.name}");
        Vector3 forceDirection = (character.transform.position - kickOrigin).normalized + Vector3.up * UPWARDS_MODIFIER;
        forceDirection.Normalize();

        // Temporarily disable character controller to allow physics to take over
        var controller = character.GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;
        if (controller != null)
        {
            Plugin.Logger.LogInfo($"Disabling character controller for {character.name}");
            controller.enabled = false;
        }

        // Apply force to all ragdoll parts
        foreach (Bodypart bodypart in character.refs.ragdoll.partList)
        {
            if (bodypart.Rig != null)
            {
                Plugin.Logger.LogInfo($"Applying kick force to {bodypart.name}");
                bodypart.Rig.isKinematic = false;
                bodypart.Rig.velocity = Vector3.zero;
                bodypart.Rig.AddForce(forceDirection * KICK_FORCE, ForceMode.VelocityChange);
            }
        }

        // Wait for physics to apply
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // Re-enable character controller after physics have been applied
        if (controller != null && wasEnabled)
        {
            Plugin.Logger.LogInfo($"Re-enabling character controller for {character.name}");
            controller.enabled = true;
        }

        // Clean up
        if (activeKicks.ContainsKey(character))
        {
            Plugin.Logger.LogInfo($"Cleaning up kick for {character.name}");
            activeKicks.Remove(character);
        }
    }

    private void OnDisable()
    {
        // Clean up any active kicks
        foreach (var kvp in activeKicks)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }

        activeKicks.Clear();
    }
}

[HarmonyPatch(typeof(EmoteWheel), nameof(EmoteWheel.Hover), MethodType.Normal)]
internal class KickAss
{
    static void Prefix(EmoteWheelData emoteWheelData, EmoteWheel __instance)
    {
        Plugin.Logger.LogInfo("Emote hovered: " + emoteWheelData.emoteName);
        if (emoteWheelData.emoteName == Plugin.ConfigKickEmotion.Value)
        {
            __instance.StartCoroutine(PerformKick());
        }
    }

    static IEnumerator PerformKick()
    {
        Plugin.Logger.LogInfo("Performing kick");
        yield return new WaitForFixedUpdate();

        Character localCharacter = Character.localCharacter;
        if (localCharacter == null)
        {
            Plugin.Logger.LogInfo("No local character found");
            yield break;
        }

        // Get the player component
        Player localPlayer = localCharacter.player;
        if (localPlayer == null)
        {
            Plugin.Logger.LogInfo("No player component found");
            yield break;
        }

        // Add KickMono if not present
        if (!localPlayer.TryGetComponent(out KickMono _))
        {
            localPlayer.gameObject.AddComponent<KickMono>();
        }

        // Calculate kick origin and direction
        Vector3 kickOrigin = MainCamera.instance.transform.position + Vector3.forward * 0.5f;
        Vector3 kickDirection = MainCamera.instance.transform.forward;

        // Visual feedback
        //localCharacter.animator.SetTrigger("Kick");

        // Play kick sound
        //SFX_Player.Instance.PlaySFX(SFX_Instance.Get("SFX_Player_Kick"), localCharacter.transform.position);

        // Send kick RPC
        Plugin.Logger.LogInfo($"Kick performed from {kickOrigin} in direction {kickDirection}");
        localPlayer.photonView.RPC("KickRPC", RpcTarget.All, kickOrigin, kickDirection);
    }
}