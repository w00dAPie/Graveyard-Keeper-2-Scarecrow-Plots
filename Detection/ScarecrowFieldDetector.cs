using System.Collections.Generic;
using GK2ScarecrowPlots.Logging;
using UnityEngine;

namespace GK2ScarecrowPlots.Detection
{
    internal static class ScarecrowFieldDetector
    {
        private const float ColumnSpacing = 0.96f;
        private const float RowSpacingOuter = 1.20f;
        private const float RowSpacingMiddle = 1.50f;
        private const float Tolerance = 0.10f;

        private const int MinimumMatches = 8;

        internal sealed class Result
        {
            public Vector3 MissingA;
            public Vector3 MissingB;

            public bool MissingAOccupied;
            public bool MissingBOccupied;
        }

        private sealed class Candidate
        {
            public Vector2 Origin;
            public Vector2 ColumnDirection;
            public Vector2 RowDirection;
            public int Matches;
        }

        internal static bool TryDetect(List<WgoData> beds, out Result result)
        {
            result = null;

            if (beds == null)
            {
                ModLog.Debug("Detector stopped: beds list is null.");

                return false;
            }

            ModLog.Debug(
                $"Detector started | Beds={beds.Count} | " + $"MinimumMatches={MinimumMatches}"
            );

            if (beds.Count < MinimumMatches)
            {
                ModLog.Debug(
                    $"Detector stopped: not enough garden beds | "
                        + $"Beds={beds.Count} | "
                        + $"Minimum={MinimumMatches}"
                );

                return false;
            }

            Candidate best = null;

            foreach (WgoData firstBed in beds)
            {
                Vector2 first = ToXZ(firstBed.Position);

                foreach (WgoData secondBed in beds)
                {
                    if (ReferenceEquals(firstBed, secondBed))
                    {
                        continue;
                    }

                    Vector2 second = ToXZ(secondBed.Position);

                    Vector2 delta = second - first;

                    if (Mathf.Abs(delta.magnitude - ColumnSpacing) > Tolerance)
                    {
                        continue;
                    }

                    Vector2 columnDirection = delta.normalized;

                    Vector2 perpendicular = new Vector2(-columnDirection.y, columnDirection.x);

                    TestCandidates(beds, first, columnDirection, perpendicular, ref best);

                    TestCandidates(beds, first, columnDirection, -perpendicular, ref best);
                }
            }

            ModLog.Debug(
                $"Detector best candidate | "
                    + $"Matches={best?.Matches ?? 0} | "
                    + $"Origin={best?.Origin} | "
                    + $"ColumnDirection={best?.ColumnDirection} | "
                    + $"RowDirection={best?.RowDirection}"
            );

            if (best == null || best.Matches < MinimumMatches)
            {
                ModLog.Debug("Detector stopped: no valid scarecrow field candidate.");

                return false;
            }

            Vector2 row0 = best.Origin;

            Vector2 row1 = row0 + best.RowDirection * RowSpacingOuter;

            Vector2 row2 = row1 + best.RowDirection * RowSpacingMiddle;

            Vector2 row3 = row2 + best.RowDirection * RowSpacingOuter;

            Vector2 targetA = row2 + best.ColumnDirection * (ColumnSpacing * 2f);

            Vector2 targetB = row3 + best.ColumnDirection * (ColumnSpacing * 2f);

            float y = FindFieldY(beds, best.Origin);

            result = new Result
            {
                MissingA = ToXYZ(targetA, y),

                MissingB = ToXYZ(targetB, y),

                MissingAOccupied = HasBedAt(beds, targetA),

                MissingBOccupied = HasBedAt(beds, targetB),
            };

            ModLog.Debug(
                $"Detector result | "
                    + $"MissingA={result.MissingA} | "
                    + $"OccupiedA={result.MissingAOccupied} | "
                    + $"MissingB={result.MissingB} | "
                    + $"OccupiedB={result.MissingBOccupied}"
            );

            return true;
        }

        private static void TestCandidates(
            List<WgoData> beds,
            Vector2 knownBed,
            Vector2 columnDirection,
            Vector2 rowDirection,
            ref Candidate best
        )
        {
            float[] rowOffsets =
            {
                0f,
                RowSpacingOuter,
                RowSpacingOuter + RowSpacingMiddle,
                RowSpacingOuter + RowSpacingMiddle + RowSpacingOuter,
            };

            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 5; column++)
                {
                    Vector2 origin =
                        knownBed
                        - columnDirection * (ColumnSpacing * column)
                        - rowDirection * rowOffsets[row];

                    int matches = CountMatches(beds, origin, columnDirection, rowDirection);

                    if (best == null || matches > best.Matches)
                    {
                        best = new Candidate
                        {
                            Origin = origin,
                            ColumnDirection = columnDirection,
                            RowDirection = rowDirection,
                            Matches = matches,
                        };
                    }
                }
            }
        }

        private static int CountMatches(
            List<WgoData> beds,
            Vector2 origin,
            Vector2 columnDirection,
            Vector2 rowDirection
        )
        {
            int matches = 0;

            float[] rowOffsets =
            {
                0f,
                RowSpacingOuter,
                RowSpacingOuter + RowSpacingMiddle,
                RowSpacingOuter + RowSpacingMiddle + RowSpacingOuter,
            };

            for (int row = 0; row < 4; row++)
            {
                Vector2 rowStart = origin + rowDirection * rowOffsets[row];

                for (int column = 0; column < 5; column++)
                {
                    Vector2 expected = rowStart + columnDirection * (ColumnSpacing * column);

                    if (HasBedAt(beds, expected))
                    {
                        matches++;
                    }
                }
            }

            return matches;
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
                {
                    continue;
                }

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
