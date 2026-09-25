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

        public const string PluginVersion = "0.1.5";

        private Harmony harmony;

        private Coroutine gardenStartupCoroutine;
        private Coroutine scarecrowWatcherCoroutine;

        private ScarecrowWatcherService scarecrowWatcherService;

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

            harmony = new Harmony(PluginGuid);

            harmony.PatchAll();

            MainGame.OnGameStarted += OnGameStarted;

            scarecrowWatcherService = new ScarecrowWatcherService();

            scarecrowWatcherCoroutine = StartCoroutine(scarecrowWatcherService.Watch());

            ModLog.Info($"{PluginName} loaded.");
        }

        private void OnGameStarted()
        {
            ModLog.Debug("Game started. Waiting for garden zone...");

            if (gardenStartupCoroutine != null)
            {
                StopCoroutine(gardenStartupCoroutine);
            }

            gardenStartupCoroutine = StartCoroutine(GardenStartupService.ProcessGarden());
        }

        private void OnDestroy()
        {
            MainGame.OnGameStarted -= OnGameStarted;

            if (gardenStartupCoroutine != null)
            {
                StopCoroutine(gardenStartupCoroutine);
            }

            if (scarecrowWatcherCoroutine != null)
            {
                StopCoroutine(scarecrowWatcherCoroutine);
            }

            harmony?.UnpatchSelf();
        }
    }
}
