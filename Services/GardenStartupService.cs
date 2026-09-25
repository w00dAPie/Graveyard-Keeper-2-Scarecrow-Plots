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
            var delay = new WaitForSecondsRealtime(delaySeconds);

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (ModLog.IsDebugEnabled)
                    ModLog.Debug($"GameStart fallback attempt={attempt}/{maxAttempts}");

                if (GardenZoneRegistry.TryGet(out WorldZone zone))
                {
                    if (ModLog.IsDebugEnabled)
                        ModLog.Debug(
                            $"Garden zone found by GameStart fallback | " + $"Attempt={attempt}"
                        );

                    GardenZoneProcessor.Process(zone, "MainGame.OnGameStarted");
                    // Anchor availability and incomplete work are retried by the two-second
                    // watcher; do not rerun grid detection at the discovery polling rate.
                    yield break;
                }

                yield return delay;
            }

            ModLog.Warning(
                $"Garden zone was not found after " + $"{maxAttempts * delaySeconds:0.0} seconds."
            );
        }
    }
}
