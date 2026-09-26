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

            return found
                && TryGetColumnSpacing(UniqueCoordinates(beds, true), out float spacing)
                && bestDistance <= spacing * 0.5f;
        }

        private static bool TryFindRows(
            List<WgoData> beds,
            float columnX,
            float scarecrowZ,
            out float upperZ,
            out float lowerZ
        )
        {
            upperZ = 0f;
            lowerZ = 0f;

            // The scarecrow column can have BOTH target beds missing. Use the
            // neighboring columns as row evidence, not just that column's beds.
            var columns = UniqueCoordinates(beds, true);
            var rows = UniqueCoordinates(beds, false);
            if (ModLog.IsDebugEnabled)
                ModLog.Debug(
                    $"Garden grid | UniqueRows=[{string.Join(", ", rows.ConvertAll(z => z.ToString("F2")))}]"
                );

            if (!TryGetColumnSpacing(columns, out float columnSpacing))
                return false;

            var nearby = beds.FindAll(bed =>
                bed != null
                && Mathf.Abs(bed.Position.x - columnX) <= columnSpacing * 2f + OccupancyTolerance
            );
            rows = UniqueCoordinates(nearby, false);
            // A row needs evidence from at least two distinct columns.
            rows.RemoveAll(z => CountRowColumns(nearby, z) < 2);
            bool upperFound = false;
            bool lowerFound = false;
            foreach (float z in rows)
            {
                if (z > scarecrowZ && (!upperFound || z < upperZ))
                {
                    upperZ = z;
                    upperFound = true;
                }
                else if (z < scarecrowZ && (!lowerFound || z > lowerZ))
                {
                    lowerZ = z;
                    lowerFound = true;
                }
            }

            // Paired rows have a smaller gap than the aisle between pairs.
            // Use the smallest observed adjacent gap supported by >=3 shared
            // columns; averaging the gaps would put plots between actual rows.
            float rowSpacing = float.MaxValue;
            for (int i = 1; i < rows.Count; i++)
            {
                float gap = rows[i] - rows[i - 1];
                int sharedColumns = 0;
                foreach (float x in columns)
                {
                    if (Mathf.Abs(x - columnX) > columnSpacing * 2f + OccupancyTolerance)
                        continue;
                    if (HasBedAtXZ(nearby, x, rows[i - 1]) && HasBedAtXZ(nearby, x, rows[i]))
                        sharedColumns++;
                }
                if (sharedColumns >= 3 && gap < rowSpacing)
                    rowSpacing = gap;
            }

            if (ModLog.IsDebugEnabled)
                ModLog.Debug(
                    $"Garden grid spacing | ColumnSpacing={columnSpacing:F2} | RowSpacing={(rowSpacing == float.MaxValue ? "unreliable" : rowSpacing.ToString("F2"))}"
                );

            bool upperDirect = upperFound;
            bool lowerDirect = lowerFound;
            // Two rows alone cannot distinguish a bed-pair gap from an aisle.
            // Require a third row before extending the observed pattern.
            if (upperFound != lowerFound && rows.Count >= 3 && rowSpacing != float.MaxValue)
            {
                // Infer only ONE adjacent row, never jump across an unseen row
                // or extrapolate from a distant bed such as Z=22.20 at Z=20.00.
                if (upperFound && upperZ - scarecrowZ < rowSpacing - OccupancyTolerance)
                {
                    lowerZ = upperZ - rowSpacing;
                    lowerFound = true;
                }
                else if (lowerFound && scarecrowZ - lowerZ < rowSpacing - OccupancyTolerance)
                {
                    upperZ = lowerZ + rowSpacing;
                    upperFound = true;
                }
            }

            if (ModLog.IsDebugEnabled)
                ModLog.Debug(
                    $"Scarecrow surrounding rows | ColumnX={columnX:F2} | ScarecrowZ={scarecrowZ:F2} | UpperZ={upperZ:F2} | UpperSource={(upperDirect ? "Direct" : upperFound ? "Inferred" : "Missing")} | LowerZ={lowerZ:F2} | LowerSource={(lowerDirect ? "Direct" : lowerFound ? "Inferred" : "Missing")}"
                );

            return upperFound && lowerFound;
        }

        private static List<float> UniqueCoordinates(List<WgoData> beds, bool columns)
        {
            var values = new List<float>();
            foreach (WgoData bed in beds)
                if (bed != null)
                    values.Add(columns ? bed.Position.x : bed.Position.z);
            values.Sort();
            var unique = new List<float>();
            foreach (float value in values)
                if (unique.Count == 0 || value - unique[unique.Count - 1] > OccupancyTolerance)
                    unique.Add(value);
            return unique;
        }

        private static bool TryGetColumnSpacing(List<float> columns, out float spacing)
        {
            spacing = float.MaxValue;
            for (int i = 1; i < columns.Count; i++)
                spacing = Mathf.Min(spacing, columns[i] - columns[i - 1]);
            int matches = 0;
            for (int i = 1; i < columns.Count; i++)
                if (Mathf.Abs(columns[i] - columns[i - 1] - spacing) <= OccupancyTolerance)
                    matches++;
            // Three columns, not one arbitrary pair, must support the pitch.
            return matches >= 2;
        }

        private static int CountRowColumns(List<WgoData> beds, float z)
        {
            return UniqueCoordinates(
                beds.FindAll(bed => Mathf.Abs(bed.Position.z - z) <= OccupancyTolerance),
                true
            ).Count;
        }

        private static bool HasBedAtXZ(List<WgoData> beds, float x, float z)
        {
            return beds.Exists(bed =>
                Mathf.Abs(bed.Position.x - x) <= OccupancyTolerance
                && Mathf.Abs(bed.Position.z - z) <= OccupancyTolerance
            );
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
