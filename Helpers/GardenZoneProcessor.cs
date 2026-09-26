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

        private static object blockedWorld;
        private static string blockedScene;
        private static readonly HashSet<Vector3> blockedTargets = new HashSet<Vector3>();

        internal enum ProcessingResult
        {
            Retry,
            Complete,
            Blocked,
        }

        private enum TargetResult
        {
            Complete,
            Blocked,
            Failed,
        }

        internal static void InvalidateCompletion()
        {
            completedWorld = null;
        }

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

        // Only initialization/creation failures need bounded retries in the same event.
        internal static ProcessingResult Process(
            WorldZone zone,
            string source,
            bool allowGridFallback = false
        )
        {
            if (zone == null)
            {
                if (ModLog.IsDebugEnabled)
                    ModLog.Debug($"Garden processing skipped | Source={source} | Zone=null");

                return ProcessingResult.Retry;
            }

            if (GameWorldAccess.Current == null)
                return ProcessingResult.Retry;

            WorldZoneData zoneData = zone.Data;

            if (zoneData == null || zoneData.id != "garden")
            {
                return ProcessingResult.Retry;
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
                    return ProcessingResult.Complete;
            }

            if (!GardenZoneRegistry.TryGetVisualRoot(zone, out Transform gardenRoot))
            {
                if (allowGridFallback && ModLog.IsDebugEnabled)
                    ModLog.Debug(
                        $"Garden processing incomplete | Reason=VisualRootUnavailable | ZoneId={zoneData.id} | SceneId={zoneData.gameSceneId} | RegisteredWGOs={zone.Wgos.Count}"
                    );
                return ProcessingResult.Retry;
            }

            bool hasAnchor = ScarecrowAnchorDetector.TryGetScarecrowRoot(
                gardenRoot,
                out Transform scarecrow
            );
            if (hasAnchor && ModConfig.HideScarecrow?.Value == true)
                ScarecrowVisualHelper.Hide(scarecrow);

            if (!ModConfig.Enabled.Value)
                return hasAnchor ? ProcessingResult.Complete : ProcessingResult.Retry;

            if (IsCompleted(gardenRoot, scarecrow, zoneData.gameSceneId))
                return ProcessingResult.Complete;

            GameScene scene = MainGame.PlayerController?.CurrentGameScene;
            if (scene == null || scene.Id != zoneData.gameSceneId)
                return ProcessingResult.Retry;

            // Never infer grid positions merely because visuals have not loaded yet.
            // A final bounded attempt may use a fully initialized, anchorless garden.
            if (!hasAnchor && (!allowGridFallback || !scene.IsStartCompleted))
                return ProcessingResult.Retry;

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
                return ProcessingResult.Retry;
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
                return ProcessingResult.Retry;
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

                    return ProcessingResult.Retry;
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

            if (!ReferenceEquals(blockedWorld, GameWorldAccess.Current) || blockedScene != scene.Id)
            {
                blockedTargets.Clear();
                blockedWorld = GameWorldAccess.Current;
                blockedScene = scene.Id;
            }
            blockedTargets.RemoveWhere(position =>
                !position.Equals(targetA) && !position.Equals(targetB)
            );

            bool completed;
            bool blocked;
            using (var batch = new GardenPlotHelper.PlotCreationBatch())
            {
                TargetResult resultA = TrySpawnGardenPlot(
                    scene,
                    wgos,
                    targetA,
                    targetAOccupied,
                    batch
                );
                TargetResult resultB = TrySpawnGardenPlot(
                    scene,
                    wgos,
                    targetB,
                    targetBOccupied,
                    batch
                );
                completed = resultA == TargetResult.Complete && resultB == TargetResult.Complete;
                blocked = resultA == TargetResult.Blocked || resultB == TargetResult.Blocked;
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
            if (!completed)
            {
                InvalidateCompletion();
                if (ModLog.IsDebugEnabled)
                    ModLog.Debug(
                        $"Garden processing incomplete | Reason={(blocked ? "BlockedTarget" : "PlotCreationFailed")}"
                    );
            }
            return completed ? ProcessingResult.Complete
                : blocked ? ProcessingResult.Blocked
                : ProcessingResult.Retry;
        }

        private static TargetResult TrySpawnGardenPlot(
            GameScene scene,
            List<WgoData> wgos,
            Vector3 position,
            bool gardenBedOccupied,
            GardenPlotHelper.PlotCreationBatch batch
        )
        {
            if (gardenBedOccupied)
            {
                blockedTargets.Remove(position);
                if (ModLog.IsDebugEnabled)
                    ModLog.Debug(
                        $"Skipping garden plot at {position}: " + "garden bed already exists."
                    );

                return TargetResult.Complete;
            }

            if (ModLog.IsDebugEnabled)
                ModLog.Debug($"Checking target for blocking WGOs | Position={position}");

            WgoData blockingWgo = FindBlockingWgo(wgos, position);

            if (blockingWgo != null)
            {
                blockedTargets.Add(position);
                if (ModLog.IsDebugEnabled)
                    ModLog.Debug(
                        $"Target blocked temporarily | "
                            + $"Position={position} | "
                            + $"BlockingWgo="
                            + $"{blockingWgo.Definition?.id ?? blockingWgo.id} | "
                            + $"BlockingPosition={blockingWgo.Position}"
                    );

                return TargetResult.Blocked;
            }

            bool previouslyBlocked = blockedTargets.Remove(position);
            if (ModLog.IsDebugEnabled)
                ModLog.Debug(
                    $"{(previouslyBlocked ? "Previously blocked target now clear" : "Target clear")} | Position={position}"
                );

            WgoData created = GardenPlotHelper.SpawnGardenPlot(scene, position, batch);

            if (created != null)
            {
                wgos.Add(created);
            }
            return created != null ? TargetResult.Complete : TargetResult.Failed;
        }

        private static WgoData FindBlockingWgo(List<WgoData> wgos, Vector3 position)
        {
            Vector2 target = new Vector2(position.x, position.z);

            foreach (WgoData wgo in wgos)
            {
                if (wgo == null)
                {
                    continue;
                }

                if (wgo.Definition?.wgoGroup == "garden_bed")
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
