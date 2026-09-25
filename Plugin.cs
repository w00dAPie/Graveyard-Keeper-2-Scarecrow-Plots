using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GK2ScarecrowPlots.Helpers;
using HarmonyLib;
using UnityEngine;

namespace GK2ScarecrowPlots
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "de.w00dst0ckOo.gk2.scarecrowplots";
        public const string PluginName = "Graveyard Keeper 2 - Scarecrow Plots";
        public const string PluginVersion = "0.1.3";

        internal static ManualLogSource Log;
        internal static ConfigFile ConfigFile;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<string> CreatedPlotIds;
        internal static ConfigEntry<bool> DebugLogging;

        private Harmony harmony;
        private Coroutine gameStartCoroutine;

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

            DebugLogging = Config.Bind(
                "Debug",
                "EnableDebugLogging",
                false,
                "Enables detailed diagnostic logging for garden detection and plot creation."
            );

            Logger.LogInfo($"{PluginName} {PluginVersion} loading...");

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll();

            MainGame.OnGameStarted += OnGameStarted;

            Logger.LogInfo($"{PluginName} loaded.");
        }

        private void OnGameStarted()
        {
            DebugLog("Game started. Waiting for garden zone...");

            if (gameStartCoroutine != null)
            {
                StopCoroutine(gameStartCoroutine);
            }

            gameStartCoroutine = StartCoroutine(ProcessGardenAfterGameStart());
        }

        private IEnumerator ProcessGardenAfterGameStart()
        {
            const int maxAttempts = 50;
            const float delaySeconds = 0.1f;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                DebugLog($"GameStart fallback attempt={attempt}/{maxAttempts}");

                WorldZone[] zones = UnityEngine.Object.FindObjectsByType<WorldZone>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

                DebugLog($"WorldZones found={zones.Length}");

                foreach (WorldZone zone in zones)
                {
                    if (zone?.Data == null || zone.Data.id != "garden")
                    {
                        continue;
                    }

                    DebugLog($"Garden zone found by GameStart fallback | Attempt={attempt}");

                    GardenZoneProcessor.Process(zone, "MainGame.OnGameStarted");

                    gameStartCoroutine = null;

                    yield break;
                }

                yield return new WaitForSecondsRealtime(delaySeconds);
            }

            Log.LogWarning(
                $"Garden zone was not found after " + $"{maxAttempts * delaySeconds:0.0} seconds."
            );

            gameStartCoroutine = null;
        }

        internal static void DebugLog(string message)
        {
            if (DebugLogging?.Value == true)
            {
                Log.LogInfo($"[DEBUG] {message}");
            }
        }

        private void OnDestroy()
        {
            MainGame.OnGameStarted -= OnGameStarted;

            if (gameStartCoroutine != null)
            {
                StopCoroutine(gameStartCoroutine);
                gameStartCoroutine = null;
            }

            harmony?.UnpatchSelf();
        }
    }
}
