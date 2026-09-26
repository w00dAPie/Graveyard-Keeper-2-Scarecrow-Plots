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
        private static object completedWorld;
        private static string completedScene;
        private static string completedTier;
        private static string completedPlotIds;
        private static Vector3 completedAnchorPosition;

        internal static bool IsCompleted(Transform root, Transform scarecrow, string sceneId)
        {
            if (!ModConfig.Enabled.Value)
            {
                completedWorld = null;
                return false;
            }
            return GameWorldAccess.Current != null
                && ReferenceEquals(completedWorld, GameWorldAccess.Current)
                && root != null
                && scarecrow != null
                && completedScene == sceneId
                && completedTier == root.name
                && completedAnchorPosition.Equals(scarecrow.position)
                && completedPlotIds == ModConfig.CreatedPlotIds.Value;
        }

        // True means this attempt completed; callers may retry incomplete initialization.
        internal static bool Process(WorldZone zone, string source, bool allowGridFallback = false)
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

            if (!ModConfig.Enabled.Value)
            {
                completedWorld = null;
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

                if (ModConfig.HideScarecrow?.Value != true)
                    return true;
            }

            if (!GardenZoneRegistry.TryGetVisualRoot(zone, out Transform gardenRoot))
            {
                if (allowGridFallback && ModLog.IsDebugEnabled)
                    ModLog.Debug(
                        $"Garden processing incomplete | Reason=VisualRootUnavailable | ZoneId={zoneData.id} | SceneId={zoneData.gameSceneId} | RegisteredWGOs={zone.Wgos.Count}"
                    );
                return false;
            }

            bool hasAnchor = ScarecrowAnchorDetector.TryGetScarecrowRoot(
                gardenRoot,
                out Transform scarecrow
            );
            if (hasAnchor && ModConfig.HideScarecrow?.Value == true)
                ScarecrowVisualHelper.Hide(scarecrow);

            if (!ModConfig.Enabled.Value)
                return hasAnchor;

            if (IsCompleted(gardenRoot, scarecrow, zoneData.gameSceneId))
                return true;

            GameScene scene = MainGame.PlayerController?.CurrentGameScene;
            if (scene == null || scene.Id != zoneData.gameSceneId)
                return false;

            // Never infer grid positions merely because visuals have not loaded yet.
            // A final bounded attempt may use a fully initialized, anchorless garden.
            if (!hasAnchor && (!allowGridFallback || !scene.IsStartCompleted))
                return false;

            if (ModLog.IsDebugEnabled)
                ModLog.Debug($"Processing garden | Source={source} | ZoneId={zoneData.id}");

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

            bool anchorFound = ScarecrowAnchorDetector.TryDetect(
                beds,
                scarecrow,
                out ScarecrowAnchorDetector.Result anchorResult
            );
            // A real anchor takes precedence even if its surrounding beds are still loading.
            if (hasAnchor && !anchorFound)
                return false;
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

            bool completed;
            using (var batch = new GardenPlotHelper.PlotCreationBatch())
            {
                bool completedA = TrySpawnGardenPlot(scene, wgos, targetA, targetAOccupied, batch);
                bool completedB = TrySpawnGardenPlot(scene, wgos, targetB, targetBOccupied, batch);
                completed = completedA && completedB;
            }
            // Persist completion only after both plots succeeded and the batch saved IDs.
            // Visual instances can be recreated by chunk loading without changing the plots.
            // A grid result must never suppress a later authoritative anchor attempt.
            if (completed && anchorFound)
            {
                completedWorld = GameWorldAccess.Current;
                completedScene = zoneData.gameSceneId;
                completedTier = gardenRoot.name;
                completedAnchorPosition = scarecrow.position;
                completedPlotIds = ModConfig.CreatedPlotIds.Value;
            }
            return completed;
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
