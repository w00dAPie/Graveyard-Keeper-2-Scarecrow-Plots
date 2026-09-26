using BepInEx;
using GK2ScarecrowPlots.Configuration;
using GK2ScarecrowPlots.Logging;
using GK2ScarecrowPlots.Services;
using HarmonyLib;
using UnityEngine;

namespace GK2ScarecrowPlots
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "de.w00dst0ckOo.gk2.scarecrowplots";

        public const string PluginName = "Graveyard Keeper 2 - Scarecrow Plots";

        public const string PluginVersion = "0.1.8";

        private Harmony harmony;

        private Coroutine gardenStartupCoroutine;
        private static Plugin instance;

        private void Awake()
        {
            ModLog.Initialize(Logger);

            LegacyConfig legacy = ConfigMigration.ReadLegacy(Config.ConfigFilePath);

            if (legacy.Found)
            {
                ConfigMigration.RemoveLegacySections(Config.ConfigFilePath);

                Config.Reload();
            }

            ModConfig.Initialize(Config, legacy);

            ModLog.Info($"{PluginName} {PluginVersion} loading...");

            instance = this;
            harmony = new Harmony(PluginGuid);

            harmony.PatchAll();

            MainGame.OnGameStarted += OnGameStarted;

            ModLog.Info($"{PluginName} loaded.");
        }

        private void OnGameStarted()
        {
            RequestGardenProcessing("MainGame.OnGameStarted");
        }

        internal static void RequestGardenProcessing(string source)
        {
            if (instance == null)
                return;

            // Coalesce initialization events into one bounded retry coroutine.
            if (instance.gardenStartupCoroutine != null)
                instance.StopCoroutine(instance.gardenStartupCoroutine);

            instance.gardenStartupCoroutine = instance.StartCoroutine(
                GardenStartupService.ProcessGarden(source)
            );
        }

        private void OnDestroy()
        {
            MainGame.OnGameStarted -= OnGameStarted;

            if (gardenStartupCoroutine != null)
            {
                StopCoroutine(gardenStartupCoroutine);
            }

            instance = null;

            harmony?.UnpatchSelf();
        }
    }
}
