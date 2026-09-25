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
            GardenZoneProcessor.Process(__instance, "AddWgosOnGameSceneStart");
        }
    }
}
