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
            if (__instance == null || __instance.Data == null || __instance.Data.id != "garden")
                return;
            GardenZoneRegistry.Remember(__instance);
            Plugin.RequestGardenProcessing("AddWgosOnGameSceneStart");
        }
    }
}
