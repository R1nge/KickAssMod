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

[HarmonyPatch(typeof(AirportCheckInKiosk))]
[HarmonyPatch("IsInteractable")] // if possible use nameof() here
class Patch01
{
    static bool Prefix(AirportCheckInKiosk __instance, ref Character ___character)
    {
        return false;
    }
}

public class AirportCheckInKiosk
{
    // Token: 0x0600044B RID: 1099 RVA: 0x00019722 File Offset: 0x00017922
    public bool IsInteractible(Character interactor)
    {
        return true;
    }
}

public class Character
{
    
}