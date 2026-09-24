using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace GK2ScarecrowPlots
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "de.w00dst0ckOo.gk2.scarecrowplots";

        public const string PluginName = "Graveyard Keeper 2 - Scarecrow Plots";

        public const string PluginVersion = "0.1.1";

        internal static ManualLogSource Log;
        internal static ConfigFile ConfigFile;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<string> CreatedPlotIds;

        private Harmony harmony;

        private void Awake()
        {
            Log = Logger;
            ConfigFile = Config;

            Enabled = Config.Bind(
                "General",
                "Enabled",
                true,
                "Adds the two garden plots occupied by the scarecrow. "
                    + "Set to false to remove plots previously created by this mod. "
                    + "Any crops planted on those plots will also be removed."
            );

            CreatedPlotIds = Config.Bind(
                "Internal",
                "CreatedPlotIds",
                "",
                "Internal list of garden plots created by this mod. " + "Do not edit manually."
            );

            Logger.LogInfo($"{PluginName} {PluginVersion} loading...");

            harmony = new Harmony(PluginGuid);

            harmony.PatchAll();

            Logger.LogInfo($"{PluginName} loaded.");
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }
    }
}
