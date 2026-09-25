using System.Collections.Generic;
using GK2ScarecrowPlots.Logging;
using UnityEngine;

namespace GK2ScarecrowPlots.Detection
{
    internal static class ScarecrowAnchorDetector
    {
        private const float OccupancyTolerance = 0.10f;

        internal sealed class Result
        {
            internal Vector3 MissingA;
            internal Vector3 MissingB;

            internal bool MissingAOccupied;
            internal bool MissingBOccupied;

            internal Vector3 ScarecrowPosition;
        }

        internal static bool TryDetect(List<WgoData> beds, out Result result)
        {
            result = null;

            if (beds == null || beds.Count == 0)
            {
                ModLog.Debug("Scarecrow anchor detection failed: no garden beds.");

                return false;
            }

            if (!TryGetScarecrowRoot(out Transform scarecrow))
            {
                ModLog.Debug("Scarecrow anchor detection failed: " + "scarecrow root not found.");

                return false;
            }

            Vector3 scarecrowPosition = scarecrow.position;

            ModLog.Debug($"Scarecrow anchor found | " + $"Position={scarecrowPosition}");

            if (!TryFindNearestColumn(beds, scarecrowPosition.x, out float columnX))
            {
                ModLog.Debug("Scarecrow anchor detection failed: " + "garden column not found.");

                return false;
            }

            if (
                !TryFindRows(beds, columnX, scarecrowPosition.z, out float upperZ, out float lowerZ)
            )
            {
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

        internal static bool TryGetScarecrowRoot(out Transform scarecrow)
        {
            scarecrow = null;

            Transform[] transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (Transform transform in transforms)
            {
                if (transform == null)
                {
                    continue;
                }

                if (transform.name != "scarecrow_on_stick")
                {
                    continue;
                }

                Transform parent = transform.parent;

                if (parent == null || parent.name != "Base")
                {
                    continue;
                }

                Transform gardenRoot = parent.parent;

                if (gardenRoot == null || gardenRoot.name != "garden_t1(Clone)")
                {
                    continue;
                }

                scarecrow = transform;

                return true;
            }

            return false;
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
