using System.Collections.Generic;
using GK2ScarecrowPlots.Detection;
using UnityEngine;

namespace GK2ScarecrowPlots.Helpers
{
    internal static class GardenZoneProcessor
    {
        private const float BlockedPositionTolerance = 0.35f;

        internal static void Process(WorldZone zone, string source)
        {
            if (zone == null)
            {
                Plugin.DebugLog($"Garden processing skipped | Source={source} | Zone=null");

                return;
            }

            WorldZoneData zoneData = zone.Data;

            if (zoneData == null || zoneData.id != "garden")
            {
                return;
            }

            Plugin.DebugLog($"Processing garden | Source={source} | ZoneId={zoneData.id}");

            if (!Plugin.Enabled.Value)
            {
                Plugin.DebugLog("Scarecrow Plots disabled. Removing mod-created plots.");

                GardenPlotHelper.RemoveCreatedPlots();

                return;
            }

            List<WgoData> wgos = CollectWgos(zoneData);
            List<WgoData> beds = CollectGardenBeds(wgos);

            Plugin.DebugLog($"Garden scan | WGOs={wgos.Count} | GardenBeds={beds.Count}");

            bool hasGardenBuilder = HasGardenBuilder(wgos);

            Plugin.DebugLog($"builder_garden present={hasGardenBuilder}");

            if (!hasGardenBuilder)
            {
                return;
            }

            Plugin.DebugLog($"Starting scarecrow detection | Beds={beds.Count}");

            if (!ScarecrowFieldDetector.TryDetect(beds, out ScarecrowFieldDetector.Result result))
            {
                Plugin.DebugLog("Scarecrow detection failed.");

                return;
            }

            Plugin.DebugLog(
                $"Scarecrow detection succeeded | "
                    + $"TargetA={result.MissingA} | "
                    + $"OccupiedA={result.MissingAOccupied} | "
                    + $"TargetB={result.MissingB} | "
                    + $"OccupiedB={result.MissingBOccupied}"
            );

            GameScene scene = MainGame.PlayerController?.CurrentGameScene;

            if (scene == null)
            {
                Plugin.DebugLog(
                    $"Garden processing stopped | Source={source} | " + "CurrentGameScene=null"
                );

                return;
            }

            TrySpawnGardenPlot(scene, wgos, result.MissingA, result.MissingAOccupied);

            TrySpawnGardenPlot(scene, wgos, result.MissingB, result.MissingBOccupied);
        }

        private static void TrySpawnGardenPlot(
            GameScene scene,
            List<WgoData> wgos,
            Vector3 position,
            bool gardenBedOccupied
        )
        {
            if (gardenBedOccupied)
            {
                Plugin.DebugLog(
                    $"Skipping garden plot at {position}: " + "garden bed already exists."
                );

                return;
            }

            Plugin.DebugLog($"Checking target for blocking WGOs | Position={position}");

            WgoData blockingWgo = FindBlockingWgo(wgos, position);

            if (blockingWgo != null)
            {
                Plugin.DebugLog(
                    $"Target blocked | "
                        + $"Position={position} | "
                        + $"BlockingWgo="
                        + $"{blockingWgo.Definition?.id ?? blockingWgo.id} | "
                        + $"BlockingPosition={blockingWgo.Position}"
                );

                return;
            }

            Plugin.DebugLog($"Target clear | Position={position}");

            WgoData created = GardenPlotHelper.SpawnGardenPlot(scene, position);

            if (created != null)
            {
                wgos.Add(created);
            }
        }

        private static WgoData FindBlockingWgo(List<WgoData> wgos, Vector3 position)
        {
            Vector2 target = new Vector2(position.x, position.z);

            foreach (WgoData wgo in wgos)
            {
                if (wgo == null || wgo.Definition == null)
                {
                    continue;
                }

                if (wgo.Definition.wgoGroup == "garden_bed")
                {
                    continue;
                }

                Vector2 wgoPosition = new Vector2(wgo.Position.x, wgo.Position.z);

                if (Vector2.Distance(wgoPosition, target) <= BlockedPositionTolerance)
                {
                    return wgo;
                }
            }

            return null;
        }

        private static List<WgoData> CollectWgos(WorldZoneData zone)
        {
            List<WgoData> wgos = new List<WgoData>();

            foreach (SGuid guid in zone.wgoDataList)
            {
                WgoData wgo = MainGame.WorldData.GetWgoData(guid);

                if (wgo != null)
                {
                    wgos.Add(wgo);
                }
            }

            return wgos;
        }

        private static List<WgoData> CollectGardenBeds(List<WgoData> wgos)
        {
            List<WgoData> beds = new List<WgoData>();

            foreach (WgoData wgo in wgos)
            {
                if (wgo?.Definition == null || wgo.Definition.wgoGroup != "garden_bed")
                {
                    continue;
                }

                beds.Add(wgo);
            }

            return beds;
        }

        private static bool HasGardenBuilder(List<WgoData> wgos)
        {
            foreach (WgoData wgo in wgos)
            {
                if (wgo?.Definition != null && wgo.Definition.id == "builder_garden")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
