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
            if (ModConfig.HideScarecrow?.Value != true)
            {
                lastScarecrowInstanceId = 0;

                return;
            }

            if (!ScarecrowAnchorDetector.TryGetScarecrowRoot(out Transform scarecrow))
            {
                return;
            }

            int instanceId = scarecrow.gameObject.GetInstanceID();

            if (instanceId == lastScarecrowInstanceId)
            {
                return;
            }

            ModLog.Debug(
                $"New scarecrow instance detected | "
                    + $"InstanceId={instanceId} | "
                    + $"Position={scarecrow.position}"
            );

            ScarecrowVisualHelper.Hide(scarecrow);

            lastScarecrowInstanceId = instanceId;
        }
    }
}
