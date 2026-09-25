using GK2ScarecrowPlots.Logging;
using UnityEngine;

namespace GK2ScarecrowPlots.Helpers
{
    internal static class ScarecrowVisualHelper
    {
        internal static void Hide(Transform scarecrow)
        {
            if (scarecrow == null)
            {
                return;
            }

            DisableRenderers(scarecrow);

            DisableColliders(scarecrow);
        }

        private static void DisableRenderers(Transform scarecrow)
        {
            Renderer[] renderers = scarecrow.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
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
                if (collider == null)
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
