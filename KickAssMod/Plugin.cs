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
    private const float KICK_FORCE = 30f;
    private const float KICK_RADIUS = 2f;
    private const float KICK_RANGE = 3f;
    private const float UPWARDS_MODIFIER = 0.5f;
    private RaycastHit[] hits = new RaycastHit[10];

    [PunRPC]
    public void KickRPC(Vector3 kickOrigin, Vector3 kickDirection, PhotonMessageInfo info)
    {
        // Only the master client handles the kick logic for consistency
        if (!PhotonNetwork.IsMasterClient) return;

        // Create a ray from the kick origin in the kick direction
        Ray ray = new Ray(kickOrigin, kickDirection);
        var hitCount = Physics.SphereCastNonAlloc(ray, KICK_RADIUS, hits, KICK_RANGE, LayerMask.GetMask("Character"));

        for (int i = 0; i < hitCount; i++)
        {
            var hit = hits[i];
            Character hitCharacter = hit.collider.GetComponentInParent<Character>();
            Plugin.Logger.LogInfo("Hit character: " + hitCharacter.name);
            if (hitCharacter != null && !hitCharacter.IsLocal)
            {
                Plugin.Logger.LogInfo("Kicking character: " + hitCharacter.name);
                // Calculate force direction with slight upward angle
                Vector3 forceDirection =
                    (hit.transform.position - kickOrigin).normalized + Vector3.up * UPWARDS_MODIFIER;
                forceDirection.Normalize();

                // Apply force to all ragdoll parts
                foreach (Bodypart bodypart in hitCharacter.refs.ragdoll.partList)
                {
                    if (bodypart.Rig != null)
                    {
                        bodypart.Rig.isKinematic = false;
                        bodypart.Rig.AddForce(forceDirection * KICK_FORCE, ForceMode.VelocityChange);
                    }
                }

                // Optional: Play kick sound effect
                //SFX_Player.Instance.PlaySFX(SFX_Instance.Get("SFX_Player_Hit"), hit.point);

                Plugin.Logger.LogInfo($"Kicked player: {hitCharacter.name}");
            }
        }
    }
}

[HarmonyPatch(typeof(EmoteWheel), nameof(EmoteWheel.Hover), MethodType.Normal)]
internal class KickAss
{
    static void Prefix(EmoteWheelData emoteWheelData, EmoteWheel __instance)
    {
        if (emoteWheelData.emoteName == "Shrug")
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
        Vector3 kickOrigin = MainCamera.instance.transform.position;
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