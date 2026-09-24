using System.Collections.Generic;
using GK2ScarecrowPlots.Detection;
using GK2ScarecrowPlots.Helpers;
using HarmonyLib;
using UnityEngine;

namespace GK2ScarecrowPlots.Patches
{
    [HarmonyPatch(typeof(WorldZone), "AddWgosOnGameSceneStart")]
    internal static class WorldZonePatch
    {
        private const float BlockedPositionTolerance = 0.35f;

        [HarmonyPostfix]
        private static void Postfix(WorldZone __instance)
        {
            WorldZoneData zone = __instance.Data;

            if (zone == null || zone.id != "garden")
            {
                return;
            }

            if (!Plugin.Enabled.Value)
            {
                GardenPlotHelper.RemoveCreatedPlots();
                return;
            }

            List<WgoData> wgos = CollectWgos(zone);
            List<WgoData> beds = CollectGardenBeds(wgos);

            if (!HasGardenBuilder(wgos))
            {
                return;
            }

            if (!ScarecrowFieldDetector.TryDetect(beds, out ScarecrowFieldDetector.Result result))
            {
                return;
            }

            GameScene scene = MainGame.PlayerController?.CurrentGameScene;

            if (scene == null)
            {
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
                return;
            }

            WgoData blockingWgo = FindBlockingWgo(wgos, position);

            if (blockingWgo != null)
            {
                Plugin.Log.LogDebug(
                    $"Skipping garden plot at {position}: "
                        + $"blocked by {blockingWgo.Definition?.id ?? blockingWgo.id}"
                );

                return;
            }

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

                // Existing garden beds are handled separately by the detector.
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
                if (wgo.Definition == null || wgo.Definition.wgoGroup != "garden_bed")
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
                if (wgo.Definition != null && wgo.Definition.id == "builder_garden")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
