using System.Collections;
using GK2ScarecrowPlots.Helpers;
using GK2ScarecrowPlots.Logging;
using UnityEngine;

namespace GK2ScarecrowPlots.Services
{
    internal static class GardenStartupService
    {
        internal static IEnumerator ProcessGarden()
        {
            const int maxAttempts = 50;
            const float delaySeconds = 0.1f;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                ModLog.Debug($"GameStart fallback attempt={attempt}/{maxAttempts}");

                WorldZone[] zones = Object.FindObjectsByType<WorldZone>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

                ModLog.Debug($"WorldZones found={zones.Length}");

                foreach (WorldZone zone in zones)
                {
                    if (zone?.Data == null || zone.Data.id != "garden")
                    {
                        continue;
                    }

                    ModLog.Debug(
                        $"Garden zone found by GameStart fallback | " + $"Attempt={attempt}"
                    );

                    GardenZoneProcessor.Process(zone, "MainGame.OnGameStarted");

                    yield break;
                }

                yield return new WaitForSecondsRealtime(delaySeconds);
            }

            ModLog.Warning(
                $"Garden zone was not found after " + $"{maxAttempts * delaySeconds:0.0} seconds."
            );
        }
    }
}
