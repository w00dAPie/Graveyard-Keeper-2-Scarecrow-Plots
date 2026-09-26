using GK2ScarecrowPlots.Configuration;
using GK2ScarecrowPlots.Detection;
using GK2ScarecrowPlots.Helpers;
using HarmonyLib;
using UnityEngine;

namespace GK2ScarecrowPlots.Patches
{
    // Fired after native visual loading, including upgrades and chunk reloads.
    [HarmonyPatch(typeof(Wgo), "CompleteVisualPartsLoad")]
    internal static class GardenVisualPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Wgo __instance)
        {
            if (
                __instance == null
                || __instance.MainWgoPart == null
                || !ScarecrowAnchorDetector.IsGardenRoot(__instance.MainWgoPart.transform)
            )
                return;
            GardenZoneRegistry.RememberVisual(__instance);

            // Hide synchronously when the visual is created, even hours after startup
            // and before the garden zone/plot processing is ready. Do not yield a frame.
            bool hasAnchor = ScarecrowAnchorDetector.TryGetScarecrowRoot(
                __instance.MainWgoPart.transform,
                out Transform scarecrow
            );
            if (hasAnchor && ModConfig.HideScarecrow?.Value == true)
                ScarecrowVisualHelper.Hide(scarecrow);

            if (
                hasAnchor
                && GardenZoneProcessor.IsCompleted(
                    __instance.MainWgoPart.transform,
                    scarecrow,
                    __instance.Data?.WorldId
                )
            )
                return;

            Plugin.RequestGardenProcessing("GardenVisualLoaded");
        }
    }
}
