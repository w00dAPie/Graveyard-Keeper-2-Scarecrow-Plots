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

        // True means this attempt completed; callers may retry incomplete initialization.
        internal static bool Process(WorldZone zone, string source, Transform scarecrow = null)
        {
            if (zone == null)
            {
                if (ModLog.IsDebugEnabled)
                    ModLog.Debug($"Garden processing skipped | Source={source} | Zone=null");

                return false;
            }

            if (GameWorldAccess.Current == null)
                return false;

            WorldZoneData zoneData = zone.Data;

            if (zoneData == null || zoneData.id != "garden")
            {
                return false;
            }

            GardenZoneRegistry.Remember(zone);
            if (ModLog.IsDebugEnabled)
                ModLog.Debug($"Processing garden | Source={source} | ZoneId={zoneData.id}");

            if (!ModConfig.Enabled.Value)
            {
                if (ModLog.IsDebugEnabled)
                    ModLog.Debug("Scarecrow Plots disabled. Removing mod-created plots.");

                if (string.IsNullOrWhiteSpace(ModConfig.CreatedPlotIds.Value))
                {
                    if (ModLog.IsDebugEnabled)
                        ModLog.Debug(
                            "No stored plot IDs found. "
                                + "Existing plots may need to be removed manually in-game."
                        );
                }

                GardenPlotHelper.RemoveCreatedPlots();

                return true;
            }

            CollectGarden(
                zoneData,
                out List<WgoData> wgos,
                out List<WgoData> beds,
                out bool hasGardenBuilder
            );

            if (ModLog.IsDebugEnabled)
                ModLog.Debug($"Garden scan | WGOs={wgos.Count} | GardenBeds={beds.Count}");

            if (ModLog.IsDebugEnabled)
                ModLog.Debug($"builder_garden present={hasGardenBuilder}");

            if (!hasGardenBuilder)
            {
                return false;
            }

            if (ModLog.IsDebugEnabled)
                ModLog.Debug($"Starting scarecrow detection | " + $"Beds={beds.Count}");

            Vector3 targetA;
            Vector3 targetB;

            bool targetAOccupied;
            bool targetBOccupied;

            ScarecrowAnchorDetector.Result anchorResult;
            bool anchorFound =
                scarecrow != null
                    ? ScarecrowAnchorDetector.TryDetect(beds, scarecrow, out anchorResult)
                    : ScarecrowAnchorDetector.TryDetect(beds, out anchorResult);
            if (anchorFound)
            {
                if (ModLog.IsDebugEnabled)
                    ModLog.Debug("Scarecrow detection method=Anchor");

                targetA = anchorResult.MissingA;

                targetB = anchorResult.MissingB;

                targetAOccupied = anchorResult.MissingAOccupied;

                targetBOccupied = anchorResult.MissingBOccupied;
            }
            else
            {
                if (ModLog.IsDebugEnabled)
                    ModLog.Debug(
                        "Scarecrow anchor detection unavailable. "
                            + "Falling back to grid detection."
                    );

                if (
                    !ScarecrowFieldDetector.TryDetect(
                        beds,
                        out ScarecrowFieldDetector.Result gridResult
                    )
                )
                {
                    if (ModLog.IsDebugEnabled)
                        ModLog.Debug("Scarecrow detection failed.");

                    return false;
                }

                if (ModLog.IsDebugEnabled)
                    ModLog.Debug("Scarecrow detection method=GridFallback");

                targetA = gridResult.MissingA;

                targetB = gridResult.MissingB;

                targetAOccupied = gridResult.MissingAOccupied;

                targetBOccupied = gridResult.MissingBOccupied;
            }

            if (ModLog.IsDebugEnabled)
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
                if (ModLog.IsDebugEnabled)
                    ModLog.Debug(
                        $"Garden processing stopped | Source={source} | " + "CurrentGameScene=null"
                    );

                return false;
            }

            using (var batch = new GardenPlotHelper.PlotCreationBatch())
            {
                bool completedA = TrySpawnGardenPlot(scene, wgos, targetA, targetAOccupied, batch);
                bool completedB = TrySpawnGardenPlot(scene, wgos, targetB, targetBOccupied, batch);
                return completedA && completedB;
            }
        }

        private static bool TrySpawnGardenPlot(
            GameScene scene,
            List<WgoData> wgos,
            Vector3 position,
            bool gardenBedOccupied,
            GardenPlotHelper.PlotCreationBatch batch
        )
        {
            if (gardenBedOccupied)
            {
                if (ModLog.IsDebugEnabled)
                    ModLog.Debug(
                        $"Skipping garden plot at {position}: " + "garden bed already exists."
                    );

                return true;
            }

            if (ModLog.IsDebugEnabled)
                ModLog.Debug($"Checking target for blocking WGOs | Position={position}");

            WgoData blockingWgo = FindBlockingWgo(wgos, position);

            if (blockingWgo != null)
            {
                if (ModLog.IsDebugEnabled)
                    ModLog.Debug(
                        $"Target blocked | "
                            + $"Position={position} | "
                            + $"BlockingWgo="
                            + $"{blockingWgo.Definition?.id ?? blockingWgo.id} | "
                            + $"BlockingPosition={blockingWgo.Position}"
                    );

                return true;
            }

            if (ModLog.IsDebugEnabled)
                ModLog.Debug($"Target clear | Position={position}");

            WgoData created = GardenPlotHelper.SpawnGardenPlot(scene, position, batch);

            if (created != null)
            {
                wgos.Add(created);
            }
            return created != null;
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

        private static void CollectGarden(
            WorldZoneData zone,
            out List<WgoData> wgos,
            out List<WgoData> beds,
            out bool hasGardenBuilder
        )
        {
            wgos = new List<WgoData>(zone.wgoDataList.Count + 2);
            beds = new List<WgoData>(zone.wgoDataList.Count);
            hasGardenBuilder = false;

            foreach (SGuid guid in zone.wgoDataList)
            {
                WgoData wgo = MainGame.WorldData.GetWgoData(guid);

                if (wgo != null)
                {
                    wgos.Add(wgo);
                    var definition = wgo.Definition;
                    if (definition != null)
                    {
                        if (definition.wgoGroup == "garden_bed")
                            beds.Add(wgo);
                        if (definition.id == "builder_garden")
                            hasGardenBuilder = true;
                    }
                }
            }
        }
    }
}
