using System.Collections.Generic;
using GK2ScarecrowPlots.Detection;
using GK2ScarecrowPlots.Helpers;
using HarmonyLib;

namespace GK2ScarecrowPlots.Patches
{
    [HarmonyPatch(typeof(WorldZone), "AddWgosOnGameSceneStart")]
    internal static class WorldZonePatch
    {
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

            List<WgoData> beds = CollectGardenBeds(zone);

            if (!ScarecrowFieldDetector.TryDetect(beds, out ScarecrowFieldDetector.Result result))
            {
                return;
            }

            GameScene scene = MainGame.PlayerController?.CurrentGameScene;

            if (scene == null)
                return;

            if (!result.MissingAOccupied)
            {
                GardenPlotHelper.SpawnGardenPlot(scene, result.MissingA);
            }

            if (!result.MissingBOccupied)
            {
                GardenPlotHelper.SpawnGardenPlot(scene, result.MissingB);
            }
        }

        private static List<WgoData> CollectGardenBeds(WorldZoneData zone)
        {
            List<WgoData> beds = new List<WgoData>();

            foreach (SGuid guid in zone.wgoDataList)
            {
                WgoData wgo = MainGame.WorldData.GetWgoData(guid);

                if (
                    wgo == null
                    || wgo.Definition == null
                    || wgo.Definition.wgoGroup != "garden_bed"
                )
                {
                    continue;
                }

                beds.Add(wgo);
            }

            return beds;
        }
    }
}
