using System.Collections.Generic;
using GK2ScarecrowPlots.Logging;
using UnityEngine;

namespace GK2ScarecrowPlots.Helpers
{
    internal static class ScarecrowVisualHelper
    {
        private static readonly HashSet<Transform> hiddenScarecrows = new HashSet<Transform>();
        private static object hiddenWorld;

        internal static void Hide(Transform scarecrow)
        {
            if (scarecrow == null)
            {
                return;
            }

            WorldData world = GameWorldAccess.Current;
            if (!ReferenceEquals(hiddenWorld, world))
            {
                hiddenScarecrows.Clear();
                hiddenWorld = world;
            }

            // Pooled garden reloads keep the same anchor and its disabled components.
            // New anchors (including upgrades) and new worlds must still be handled.
            if (world == null || hiddenScarecrows.Contains(scarecrow))
                return;

            DisableRenderers(scarecrow);

            DisableColliders(scarecrow);

            hiddenScarecrows.Add(scarecrow);
        }

        private static void DisableRenderers(Transform scarecrow)
        {
            Renderer[] renderers = scarecrow.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                renderer.enabled = false;

                if (ModLog.IsDebugEnabled)
                    ModLog.Debug(
                        $"Scarecrow renderer disabled | " + $"Name={renderer.gameObject.name}"
                    );
            }
        }

        private static void DisableColliders(Transform scarecrow)
        {
            Collider[] colliders = scarecrow.GetComponentsInChildren<Collider>(true);

            foreach (Collider collider in colliders)
            {
                if (collider == null || !collider.enabled)
                {
                    continue;
                }

                collider.enabled = false;

                if (ModLog.IsDebugEnabled)
                    ModLog.Debug(
                        $"Scarecrow collider disabled | "
                            + $"Name={collider.gameObject.name} | "
                            + $"Type={collider.GetType().Name}"
                    );
            }
        }
    }
}
