using BepInEx;
using HarmonyLib;

namespace THMDAllTowers
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Harmony harmony = new Harmony("com.thmd.all.towers");
            harmony.PatchAll();
            Logger.LogInfo("Harmony patches applied.");
        }
    }
}
