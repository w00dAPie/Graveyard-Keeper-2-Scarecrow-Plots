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
        private int lastScarecrowInstanceId;

        internal IEnumerator Watch()
        {
            const float checkInterval = 0.5f;

            while (true)
            {
                ProcessScarecrow();

                yield return new WaitForSecondsRealtime(checkInterval);
            }
        }

        private void ProcessScarecrow()
        {
            if (!ScarecrowAnchorDetector.TryGetScarecrowRoot(out Transform scarecrow))
            {
                return;
            }

            int instanceId = scarecrow.gameObject.GetInstanceID();

            if (instanceId == lastScarecrowInstanceId)
            {
                return;
            }

            lastScarecrowInstanceId = instanceId;

            ModLog.Debug(
                $"New scarecrow instance detected | "
                    + $"InstanceId={instanceId} | "
                    + $"Position={scarecrow.position}"
            );

            ProcessGardenWithAnchor();

            if (ModConfig.HideScarecrow?.Value == true)
            {
                ScarecrowVisualHelper.Hide(scarecrow);
            }
        }

        private static void ProcessGardenWithAnchor()
        {
            WorldZone[] zones = Object.FindObjectsByType<WorldZone>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (WorldZone zone in zones)
            {
                if (zone?.Data == null || zone.Data.id != "garden")
                {
                    continue;
                }

                ModLog.Debug("Reprocessing garden after scarecrow instance became available.");

                GardenZoneProcessor.Process(zone, "ScarecrowWatcher");

                return;
            }

            ModLog.Debug("Garden zone not found while processing scarecrow instance.");
        }
    }
}
