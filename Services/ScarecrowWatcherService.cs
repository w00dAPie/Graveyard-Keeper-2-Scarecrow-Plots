using System.Collections;
using GK2ScarecrowPlots.Configuration;
using GK2ScarecrowPlots.Detection;
using GK2ScarecrowPlots.Helpers;
using GK2ScarecrowPlots.Logging;
using UnityEngine;

namespace GK2ScarecrowPlots.Services
{
    internal sealed class ScarecrowWatcherService
    {
        private Transform currentScarecrow;
        private object currentWorld;
        private bool gardenProcessed;

        internal IEnumerator Watch()
        {
            const float searchInterval = 2f;
            var delay = new WaitForSecondsRealtime(searchInterval);
            while (true)
            {
                WorldData world = GameWorldAccess.Current;
                if (world == null)
                {
                    currentWorld = null;
                    currentScarecrow = null;
                    gardenProcessed = false;
                    yield return delay;
                    continue;
                }
                if (!ReferenceEquals(currentWorld, world))
                {
                    currentWorld = world;
                    currentScarecrow = null;
                    gardenProcessed = false;
                }
                if (currentScarecrow == null)
                    FindScarecrow();

                // Retry delayed zone/scene initialization or a failed spawn using the known anchor.
                if (currentScarecrow != null && !gardenProcessed)
                {
                    if (GardenZoneRegistry.TryGet(out WorldZone zone))
                        gardenProcessed = GardenZoneProcessor.Process(
                            zone,
                            "ScarecrowWatcher",
                            currentScarecrow
                        );
                }
                yield return delay;
            }
        }

        private void FindScarecrow()
        {
            gardenProcessed = false;
            if (!ScarecrowAnchorDetector.TryGetScarecrowRoot(out Transform scarecrow))
                return;
            currentScarecrow = scarecrow;
            if (ModLog.IsDebugEnabled)
                ModLog.Debug(
                    $"New scarecrow instance detected | InstanceId={scarecrow.gameObject.GetInstanceID()} | Position={scarecrow.position}"
                );

            if (ModConfig.HideScarecrow?.Value == true)
                ScarecrowVisualHelper.Hide(scarecrow);
        }
    }
}
