using System.Collections;
using GK2ScarecrowPlots.Helpers;
using GK2ScarecrowPlots.Logging;
using UnityEngine;

namespace GK2ScarecrowPlots.Services
{
    internal static class GardenStartupService
    {
        internal static IEnumerator ProcessGarden(string source)
        {
            const int maxAttempts = 50;
            const float delaySeconds = 0.1f;
            var delay = new WaitForSecondsRealtime(delaySeconds);
            WorldData world = GameWorldAccess.Current;

            // Let the native initialization/upgrade callback finish before touching plots.
            yield return null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                WorldData currentWorld = GameWorldAccess.Current;
                if (world == null)
                    world = currentWorld;
                else if (!ReferenceEquals(world, currentWorld))
                    yield break;

                if (GardenZoneRegistry.TryGet(out WorldZone zone))
                {
                    GardenZoneProcessor.ProcessingResult result = GardenZoneProcessor.Process(
                        zone,
                        source,
                        allowGridFallback: attempt == maxAttempts
                    );
                    if (result != GardenZoneProcessor.ProcessingResult.Retry)
                    {
                        if (ModLog.IsDebugEnabled)
                            ModLog.Debug(
                                result == GardenZoneProcessor.ProcessingResult.Complete
                                    ? $"Scarecrow startup processing completed. | Source={source} | Attempt={attempt}"
                                    : $"Scarecrow startup processing deferred. | Source={source} | Reason=BlockedTarget | Waiting for the next garden initialization event."
                            );
                        yield break;
                    }
                }

                yield return delay;
            }

            if (ModLog.IsDebugEnabled)
                ModLog.Debug(
                    $"Scarecrow startup processing incomplete after {maxAttempts * delaySeconds:0.0} seconds. | Source={source} | Waiting for the next garden initialization event."
                );
        }
    }
}
