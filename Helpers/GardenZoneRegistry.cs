using UnityEngine;

namespace GK2ScarecrowPlots.Helpers
{
    internal static class GardenZoneRegistry
    {
        private static WorldZone garden;
        private static object world;

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
            foreach (
                WorldZone candidate in Object.FindObjectsByType<WorldZone>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                )
            )
            {
                if (candidate == null || candidate.Data == null || candidate.Data.id != "garden")
                    continue;
                Remember(candidate);
                zone = candidate;
                return true;
            }
            zone = null;
            return false;
        }
    }
}
