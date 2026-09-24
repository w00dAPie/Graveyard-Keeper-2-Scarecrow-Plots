using System.Collections.Generic;
using UnityEngine;

namespace GK2ScarecrowPlots.Detection
{
    internal static class ScarecrowFieldDetector
    {
        private const float ColumnSpacing = 0.96f;
        private const float RowSpacingOuter = 1.20f;
        private const float RowSpacingMiddle = 1.50f;
        private const float Tolerance = 0.10f;

        internal sealed class Result
        {
            public Vector3 MissingA;
            public Vector3 MissingB;

            public bool MissingAOccupied;
            public bool MissingBOccupied;
        }

        internal static bool TryDetect(List<WgoData> beds, out Result result)
        {
            result = null;

            if (beds == null || beds.Count < 18)
                return false;

            foreach (WgoData startBed in beds)
            {
                Vector2 start = ToXZ(startBed.Position);

                foreach (WgoData neighbourBed in beds)
                {
                    if (ReferenceEquals(startBed, neighbourBed))
                    {
                        continue;
                    }

                    Vector2 neighbour = ToXZ(neighbourBed.Position);

                    Vector2 delta = neighbour - start;

                    if (Mathf.Abs(delta.magnitude - ColumnSpacing) > Tolerance)
                    {
                        continue;
                    }

                    Vector2 columnDirection = delta.normalized;

                    if (!HasFiveBedRow(beds, start, columnDirection))
                    {
                        continue;
                    }

                    Vector2 perpendicular = new Vector2(-columnDirection.y, columnDirection.x);

                    if (TryMatch(beds, start, columnDirection, perpendicular, out result))
                    {
                        return true;
                    }

                    if (TryMatch(beds, start, columnDirection, -perpendicular, out result))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryMatch(
            List<WgoData> beds,
            Vector2 fullRowStart,
            Vector2 columnDirection,
            Vector2 rowDirection,
            out Result result
        )
        {
            result = null;

            /*
             * Expected field layout:
             *
             * ■ ■ ■ ■ ■
             *     1.20
             * ■ ■ ■ ■ ■
             *     1.50
             * ■ ■ X ■ ■
             *     1.20
             * ■ ■ X ■ ■
             *
             * X may be missing or already occupied.
             */

            Vector2 row0 = fullRowStart;

            Vector2 row1 = row0 + rowDirection * RowSpacingOuter;

            Vector2 row2 = row1 + rowDirection * RowSpacingMiddle;

            Vector2 row3 = row2 + rowDirection * RowSpacingOuter;

            if (!HasFiveBedRow(beds, row0, columnDirection))
            {
                return false;
            }

            if (!HasFiveBedRow(beds, row1, columnDirection))
            {
                return false;
            }

            if (!HasScarecrowRow(beds, row2, columnDirection))
            {
                return false;
            }

            if (!HasScarecrowRow(beds, row3, columnDirection))
            {
                return false;
            }

            Vector2 targetA = row2 + columnDirection * (ColumnSpacing * 2f);

            Vector2 targetB = row3 + columnDirection * (ColumnSpacing * 2f);

            float y = FindFieldY(beds, row0);

            result = new Result
            {
                MissingA = ToXYZ(targetA, y),

                MissingB = ToXYZ(targetB, y),

                MissingAOccupied = HasBedAt(beds, targetA),

                MissingBOccupied = HasBedAt(beds, targetB),
            };

            return true;
        }

        private static bool HasFiveBedRow(List<WgoData> beds, Vector2 start, Vector2 direction)
        {
            for (int column = 0; column < 5; column++)
            {
                Vector2 expected = start + direction * (ColumnSpacing * column);

                if (!HasBedAt(beds, expected))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasScarecrowRow(List<WgoData> beds, Vector2 start, Vector2 direction)
        {
            int[] requiredColumns = { 0, 1, 3, 4 };

            foreach (int column in requiredColumns)
            {
                Vector2 expected = start + direction * (ColumnSpacing * column);

                if (!HasBedAt(beds, expected))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasBedAt(List<WgoData> beds, Vector2 position)
        {
            foreach (WgoData bed in beds)
            {
                if (Vector2.Distance(ToXZ(bed.Position), position) <= Tolerance)
                {
                    return true;
                }
            }

            return false;
        }

        private static float FindFieldY(List<WgoData> beds, Vector2 reference)
        {
            float bestDistance = float.MaxValue;

            float y = 0f;

            foreach (WgoData bed in beds)
            {
                float distance = Vector2.Distance(ToXZ(bed.Position), reference);

                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                y = bed.Position.y;
            }

            return y;
        }

        private static Vector2 ToXZ(Vector3 position)
        {
            return new Vector2(position.x, position.z);
        }

        private static Vector3 ToXYZ(Vector2 position, float y)
        {
            return new Vector3(position.x, y, position.y);
        }
    }
}
