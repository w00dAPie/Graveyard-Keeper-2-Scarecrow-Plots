using GK2ScarecrowPlots.Detection;
using GK2ScarecrowPlots.Logging;
using UnityEngine;

namespace GK2ScarecrowPlots.Helpers
{
    internal static class GardenZoneRegistry
    {
        private static WorldZone garden;
        private static object world;
        private static Wgo visualOwner;
        private static WgoData visualData;
        private static object visualWorld;

        internal static void RememberVisual(Wgo owner)
        {
            if (
                owner == null
                || owner.Data == null
                || owner.MainWgoPart == null
                || !ScarecrowAnchorDetector.IsGardenRoot(owner.MainWgoPart.transform)
            )
                return;
            visualWorld = GameWorldAccess.Current;
            if (visualWorld == null)
                return;
            visualOwner = owner;
            visualData = owner.Data;
            if (ModLog.IsDebugEnabled)
                ModLog.Debug(
                    $"Garden visual registered | Name={owner.MainWgoPart.name} | WorldId={visualData.WorldId} | ZoneId={visualData.WorldZoneData?.id}"
                );
        }

        internal static void Remember(WorldZone zone)
        {
            if (zone == null || zone.Data == null || zone.Data.id != "garden")
                return;
            WorldData currentWorld = GameWorldAccess.Current;
            if (currentWorld == null)
            {
                garden = null;
                world = null;
                return;
            }
            garden = zone;
            world = currentWorld;
        }

        internal static bool TryGet(out WorldZone zone)
        {
            WorldData currentWorld = GameWorldAccess.Current;
            zone = null;
            if (currentWorld == null)
            {
                garden = null;
                world = null;
                return false;
            }
            // World identity prevents reusing a surviving component across saves.
            if (
                ReferenceEquals(world, currentWorld)
                && garden != null
                && garden.Data != null
                && garden.Data.id == "garden"
            )
            {
                zone = garden;
                return true;
            }
            garden = null;
            GameScene scene = MainGame.PlayerController?.CurrentGameScene;
            WorldZone candidate = scene != null ? scene.GetWorldZoneById("garden") : null;
            if (candidate == null || candidate.Data == null || candidate.Data.id != "garden")
                return false;
            Remember(candidate);
            zone = candidate;
            return true;
        }

        internal static bool TryGetVisualRoot(WorldZone zone, out Transform root)
        {
            root = null;
            if (zone == null || zone.Data == null || zone.Data.id != "garden")
                return false;

            // The visual WGO can belong to a different zone's registry. Keep the exact
            // owner supplied by the native load callback, but never reuse it across saves.
            if (
                !ReferenceEquals(visualWorld, GameWorldAccess.Current)
                || visualOwner == null
                || !ReferenceEquals(visualOwner.Data, visualData)
            )
            {
                visualOwner = null;
                visualData = null;
                visualWorld = null;
            }
            if (
                visualOwner != null
                && visualData.WorldId == zone.Data.gameSceneId
                && visualOwner.MainWgoPart != null
                && ScarecrowAnchorDetector.IsGardenRoot(visualOwner.MainWgoPart.transform)
            )
            {
                root = visualOwner.MainWgoPart.transform;
                return true;
            }

            // Garden tiers are loaded WGO parts; the WorldZone itself is a separate object.
            // Use its existing WGO registry, including inactive/prewarmed visual parts.
            foreach (Wgo wgo in zone.Wgos)
            {
                if (wgo == null || wgo.MainWgoPart == null)
                    continue;
                Transform candidate = wgo.MainWgoPart.transform;
                if (ScarecrowAnchorDetector.IsGardenRoot(candidate))
                {
                    root = candidate;
                    return true;
                }
            }

            // Content-based variants may parent the zone below the garden prefab.
            for (Transform parent = zone.transform; parent != null; parent = parent.parent)
            {
                if (ScarecrowAnchorDetector.IsGardenRoot(parent))
                {
                    root = parent;
                    return true;
                }
            }

            GameScene scene = MainGame.PlayerController?.CurrentGameScene;
            if (
                scene == null
                || scene.Id != zone.Data.gameSceneId
                || string.IsNullOrEmpty(zone.Data.contentPartName)
            )
                return false;
            GameSceneConfig config = scene.GameSceneConfig;
            if (
                config == null
                || !config.TryGetLoadedContent(zone.Data.contentPartName, out GameObject content)
                || content == null
            )
                return false;

            foreach (Transform candidate in content.GetComponentsInChildren<Transform>(true))
            {
                if (ScarecrowAnchorDetector.IsGardenRoot(candidate))
                {
                    root = candidate;
                    return true;
                }
            }
            return false;
        }
    }
}
