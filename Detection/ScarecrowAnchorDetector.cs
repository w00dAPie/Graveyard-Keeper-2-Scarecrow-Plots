using System;
using System.Collections.Generic;
using GK2ScarecrowPlots.Helpers;
using GK2ScarecrowPlots.Logging;
using UnityEngine;

namespace GK2ScarecrowPlots.Detection
{
    internal static class ScarecrowAnchorDetector
    {
        private const float OccupancyTolerance = 0.10f;
        private static Transform cachedScarecrow;
        private static object cachedWorld;
        private static Transform cachedGardenRoot;

        internal sealed class Result
        {
            internal Vector3 MissingA;
            internal Vector3 MissingB;

            internal bool MissingAOccupied;
            internal bool MissingBOccupied;

            internal Vector3 ScarecrowPosition;
        }

        internal static bool TryDetect(List<WgoData> beds, Transform scarecrow, out Result result)
        {
            result = null;

            if (beds == null || beds.Count == 0)
            {
                if (ModLog.IsDebugEnabled)
                    ModLog.Debug("Scarecrow anchor detection failed: no garden beds.");

                return false;
            }

            if (scarecrow == null)
            {
                if (ModLog.IsDebugEnabled)
                    ModLog.Debug(
                        "Scarecrow anchor detection failed: " + "scarecrow root not found."
                    );

                return false;
            }

            Vector3 scarecrowPosition = scarecrow.position;

            if (ModLog.IsDebugEnabled)
                ModLog.Debug($"Scarecrow anchor found | " + $"Position={scarecrowPosition}");

            if (!TryFindNearestColumn(beds, scarecrowPosition.x, out float columnX))
            {
                if (ModLog.IsDebugEnabled)
                    ModLog.Debug(
                        "Scarecrow anchor detection failed: " + "garden column not found."
                    );

                return false;
            }

            if (
                !TryFindRows(beds, columnX, scarecrowPosition.z, out float upperZ, out float lowerZ)
            )
            {
                if (ModLog.IsDebugEnabled)
                    ModLog.Debug(
                        "Scarecrow anchor detection failed: " + "surrounding garden rows not found."
                    );

                return false;
            }

            float y = FindNearestBedY(beds, scarecrowPosition);

            Vector3 upper = new Vector3(columnX, y, upperZ);

            Vector3 lower = new Vector3(columnX, y, lowerZ);

            bool upperOccupied = HasGardenBed(beds, upper, OccupancyTolerance);

            bool lowerOccupied = HasGardenBed(beds, lower, OccupancyTolerance);

            result = new Result
            {
                MissingA = upper,
                MissingB = lower,

                MissingAOccupied = upperOccupied,
                MissingBOccupied = lowerOccupied,

                ScarecrowPosition = scarecrowPosition,
            };

            if (ModLog.IsDebugEnabled)
                ModLog.Debug(
                    $"Scarecrow anchor result | "
                        + $"ColumnX={columnX:F2} | "
                        + $"UpperZ={upperZ:F2} | "
                        + $"LowerZ={lowerZ:F2} | "
                        + $"TargetA={upper} | "
                        + $"OccupiedA={upperOccupied} | "
                        + $"TargetB={lower} | "
                        + $"OccupiedB={lowerOccupied}"
                );

            return true;
        }

        internal static bool TryGetScarecrowRoot(Transform gardenRoot, out Transform scarecrow)
        {
            WorldData world = GameWorldAccess.Current;
            scarecrow = null;
            if (world == null || !IsGardenRoot(gardenRoot))
            {
                cachedWorld = null;
                cachedGardenRoot = null;
                cachedScarecrow = null;
                return false;
            }

            if (
                ReferenceEquals(cachedWorld, world)
                && cachedGardenRoot == gardenRoot
                && TryGetScarecrowAnchor(cachedScarecrow, gardenRoot, out Transform cachedAnchor)
            )
            {
                scarecrow = cachedAnchor;
                return true;
            }

            bool rootChanged =
                !ReferenceEquals(cachedWorld, world) || cachedGardenRoot != gardenRoot;
            cachedWorld = world;
            cachedGardenRoot = gardenRoot;
            cachedScarecrow = null;
            Transform bestCandidate = null;
            int candidateCount = 0;
            foreach (Transform transform in gardenRoot.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name.StartsWith("scarecrow_on_stick", StringComparison.Ordinal))
                    candidateCount++;
                if (!TryGetScarecrowAnchor(transform, gardenRoot, out Transform candidate))
                    continue;
                if (
                    bestCandidate == null
                    || (
                        !bestCandidate.gameObject.activeInHierarchy
                        && candidate.gameObject.activeInHierarchy
                    )
                )
                    bestCandidate = candidate;
            }

            // Report the searched hierarchy even when no anchor was found, once per root.
            if (ModLog.IsDebugEnabled && (rootChanged || bestCandidate != null))
                ModLog.Debug(
                    $"Garden root found | Path={GetTransformPath(gardenRoot)} | ScarecrowCandidates={candidateCount} | AnchorFound={bestCandidate != null}"
                );

            if (bestCandidate == null)
                return false;

            cachedScarecrow = bestCandidate;
            scarecrow = bestCandidate;
            if (ModLog.IsDebugEnabled)
            {
                ModLog.Debug(
                    $"Scarecrow anchor selected | Path={GetTransformPath(bestCandidate)} | Position={bestCandidate.position}"
                );
            }
            return true;
        }

        private static string GetTransformPath(Transform transform)
        {
            if (transform == null)
                return "<null>";

            string path = transform.name;
            Transform current = transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private static bool TryGetScarecrowAnchor(
            Transform transform,
            Transform gardenRoot,
            out Transform anchor
        )
        {
            anchor = null;
            if (
                transform == null
                || !transform.name.StartsWith("scarecrow_on_stick", StringComparison.Ordinal)
            )
                return false;

            // Resolve every nested mesh/shadow match to the placement root below Base.
            for (
                Transform current = transform;
                current != null && current != gardenRoot;
                current = current.parent
            )
            {
                Transform parent = current.parent;
                if (parent != null && parent.name == "Base" && parent.parent == gardenRoot)
                {
                    if (
                        !current.name.StartsWith("scarecrow_on_stick", StringComparison.Ordinal)
                        || current.name.StartsWith(
                            "scarecrow_on_stick_sh",
                            StringComparison.Ordinal
                        )
                    )
                        return false;
                    anchor = current;
                    return true;
                }
            }
            return false;
        }

        internal static bool IsGardenRoot(Transform transform)
        {
            if (transform == null)
                return false;

            const string prefix = "garden_t";
            const string suffix = "(Clone)";
            string name = transform.name;
            int tierEnd = name.Length - suffix.Length;
            if (
                tierEnd <= prefix.Length
                || !name.StartsWith(prefix, StringComparison.Ordinal)
                || !name.EndsWith(suffix, StringComparison.Ordinal)
            )
                return false;

            // garden_tablet_* crop signs share the prefix but are not garden tier prefabs.
            for (int i = prefix.Length; i < tierEnd; i++)
            {
                if (name[i] < '0' || name[i] > '9')
                    return false;
            }
            return true;
        }

        private static bool TryFindNearestColumn(
            List<WgoData> beds,
            float scarecrowX,
            out float columnX
        )
        {
            columnX = 0f;

            float bestDistance = float.MaxValue;

            bool found = false;

            foreach (WgoData bed in beds)
            {
                if (bed == null)
                {
                    continue;
                }

                float distance = Mathf.Abs(bed.Position.x - scarecrowX);

                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;

                columnX = bed.Position.x;

                found = true;
            }

            if (ModLog.IsDebugEnabled)
                ModLog.Debug(
                    $"Scarecrow nearest column | "
                        + $"ScarecrowX={scarecrowX:F2} | "
                        + $"ColumnX={columnX:F2} | "
                        + $"Distance={bestDistance:F2}"
                );

            return found;
        }

        private static bool TryFindRows(
            List<WgoData> beds,
            float columnX,
            float scarecrowZ,
            out float upperZ,
            out float lowerZ
        )
        {
            const float columnTolerance = 0.15f;

            upperZ = 0f;
            lowerZ = 0f;

            float upperDistance = float.MaxValue;

            float lowerDistance = float.MaxValue;

            bool upperFound = false;

            bool lowerFound = false;

            foreach (WgoData bed in beds)
            {
                if (bed == null)
                {
                    continue;
                }

                if (Mathf.Abs(bed.Position.x - columnX) > columnTolerance)
                {
                    continue;
                }

                float z = bed.Position.z;

                if (z > scarecrowZ)
                {
                    float distance = z - scarecrowZ;

                    if (distance < upperDistance)
                    {
                        upperDistance = distance;

                        upperZ = z;

                        upperFound = true;
                    }
                }
                else if (z < scarecrowZ)
                {
                    float distance = scarecrowZ - z;

                    if (distance < lowerDistance)
                    {
                        lowerDistance = distance;

                        lowerZ = z;

                        lowerFound = true;
                    }
                }
            }

            if (ModLog.IsDebugEnabled)
                ModLog.Debug(
                    $"Scarecrow surrounding rows | "
                        + $"ColumnX={columnX:F2} | "
                        + $"ScarecrowZ={scarecrowZ:F2} | "
                        + $"UpperZ={upperZ:F2} | "
                        + $"UpperDistance={upperDistance:F2} | "
                        + $"LowerZ={lowerZ:F2} | "
                        + $"LowerDistance={lowerDistance:F2}"
                );

            return upperFound && lowerFound;
        }

        private static float FindNearestBedY(List<WgoData> beds, Vector3 scarecrowPosition)
        {
            float bestDistance = float.MaxValue;

            float y = scarecrowPosition.y;

            Vector2 scarecrow = new Vector2(scarecrowPosition.x, scarecrowPosition.z);

            foreach (WgoData bed in beds)
            {
                if (bed == null)
                {
                    continue;
                }

                Vector2 bedPosition = new Vector2(bed.Position.x, bed.Position.z);

                float distance = Vector2.Distance(bedPosition, scarecrow);

                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;

                y = bed.Position.y;
            }

            return y;
        }

        private static bool HasGardenBed(List<WgoData> beds, Vector3 position, float tolerance)
        {
            Vector2 target = new Vector2(position.x, position.z);

            foreach (WgoData bed in beds)
            {
                if (bed == null)
                {
                    continue;
                }

                Vector2 actual = new Vector2(bed.Position.x, bed.Position.z);

                if (Vector2.Distance(actual, target) <= tolerance)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
