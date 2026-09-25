using System.Collections.Generic;
using GK2ScarecrowPlots.Configuration;
using GK2ScarecrowPlots.Detection;
using GK2ScarecrowPlots.Logging;
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
                ModLog.Debug($"Garden processing skipped | Source={source} | Zone=null");

                return;
            }

            WorldZoneData zoneData = zone.Data;

            if (zoneData == null || zoneData.id != "garden")
            {
                return;
            }

            ModLog.Debug($"Processing garden | Source={source} | ZoneId={zoneData.id}");

            if (!ModConfig.Enabled.Value)
            {
                ModLog.Debug("Scarecrow Plots disabled. Removing mod-created plots.");

                if (string.IsNullOrWhiteSpace(ModConfig.CreatedPlotIds.Value))
                {
                    ModLog.Debug(
                        "No stored plot IDs found. "
                            + "Existing plots may need to be removed manually in-game."
                    );
                }

                GardenPlotHelper.RemoveCreatedPlots();

                return;
            }

            List<WgoData> wgos = CollectWgos(zoneData);
            List<WgoData> beds = CollectGardenBeds(wgos);

            ModLog.Debug($"Garden scan | WGOs={wgos.Count} | GardenBeds={beds.Count}");

            bool hasGardenBuilder = HasGardenBuilder(wgos);

            ModLog.Debug($"builder_garden present={hasGardenBuilder}");

            if (!hasGardenBuilder)
            {
                return;
            }

            ModLog.Debug($"Starting scarecrow detection | " + $"Beds={beds.Count}");

            Vector3 targetA;
            Vector3 targetB;

            bool targetAOccupied;
            bool targetBOccupied;

            if (
                ScarecrowAnchorDetector.TryDetect(
                    beds,
                    out ScarecrowAnchorDetector.Result anchorResult
                )
            )
            {
                ModLog.Debug("Scarecrow detection method=Anchor");

                targetA = anchorResult.MissingA;

                targetB = anchorResult.MissingB;

                targetAOccupied = anchorResult.MissingAOccupied;

                targetBOccupied = anchorResult.MissingBOccupied;
            }
            else
            {
                ModLog.Debug(
                    "Scarecrow anchor detection unavailable. " + "Falling back to grid detection."
                );

                if (
                    !ScarecrowFieldDetector.TryDetect(
                        beds,
                        out ScarecrowFieldDetector.Result gridResult
                    )
                )
                {
                    ModLog.Debug("Scarecrow detection failed.");

                    return;
                }

                ModLog.Debug("Scarecrow detection method=GridFallback");

                targetA = gridResult.MissingA;

                targetB = gridResult.MissingB;

                targetAOccupied = gridResult.MissingAOccupied;

                targetBOccupied = gridResult.MissingBOccupied;
            }

            ModLog.Debug(
                $"Scarecrow detection succeeded | "
                    + $"TargetA={targetA} | "
                    + $"OccupiedA={targetAOccupied} | "
                    + $"TargetB={targetB} | "
                    + $"OccupiedB={targetBOccupied}"
            );

            GameScene scene = MainGame.PlayerController?.CurrentGameScene;

            if (scene == null)
            {
                ModLog.Debug(
                    $"Garden processing stopped | Source={source} | " + "CurrentGameScene=null"
                );

                return;
            }

            TrySpawnGardenPlot(scene, wgos, targetA, targetAOccupied);

            TrySpawnGardenPlot(scene, wgos, targetB, targetBOccupied);
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
                ModLog.Debug(
                    $"Skipping garden plot at {position}: " + "garden bed already exists."
                );

                return;
            }

            ModLog.Debug($"Checking target for blocking WGOs | Position={position}");

            WgoData blockingWgo = FindBlockingWgo(wgos, position);

            if (blockingWgo != null)
            {
                ModLog.Debug(
                    $"Target blocked | "
                        + $"Position={position} | "
                        + $"BlockingWgo="
                        + $"{blockingWgo.Definition?.id ?? blockingWgo.id} | "
                        + $"BlockingPosition={blockingWgo.Position}"
                );

                return;
            }

            ModLog.Debug($"Target clear | Position={position}");

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
