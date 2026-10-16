using System;

namespace Zraxion.Extension
{
    internal static class GridComparisonEvaluator
    {
        public static GridComparisonEvaluation Evaluate(
            GridAnalyticsSnapshot left,
            GridAnalyticsSnapshot right,
            GridComparisonTolerancePolicy tolerancePolicy)
        {
            if (left is null)
            {
                throw new ArgumentNullException(
                    nameof(left));
            }

            if (right is null)
            {
                throw new ArgumentNullException(
                    nameof(right));
            }

            if (tolerancePolicy is null)
            {
                throw new ArgumentNullException(
                    nameof(tolerancePolicy));
            }

            decimal difference;

            checked
            {
                difference =
                    right.Sum -
                    left.Sum;
            }

            var absoluteDifference =
                Math.Abs(difference);

            decimal? differencePercent = null;

            // Percentage difference is undefined when A == 0.
            // For non-zero baselines, use |A| as the denominator so the
            // direction remains driven by B - A even when A is negative.
            if (left.Sum != 0m)
            {
                var absoluteBaseline =
                    Math.Abs(left.Sum);

                checked
                {
                    differencePercent =
                        (difference / absoluteBaseline) *
                        100m;
                }
            }

            if (difference == 0m)
            {
                return CreateResult(
                    difference,
                    absoluteDifference,
                    differencePercent,
                    isMatch: true,
                    GridComparisonMatchReason.ExactMatch);
            }

            var withinAbsoluteTolerance =
                tolerancePolicy.AbsoluteTolerance.HasValue &&
                absoluteDifference <=
                    tolerancePolicy.AbsoluteTolerance.Value;

            var withinPercentageTolerance =
                tolerancePolicy.PercentageTolerance.HasValue &&
                differencePercent.HasValue &&
                Math.Abs(differencePercent.Value) <=
                    tolerancePolicy.PercentageTolerance.Value;

            if (withinAbsoluteTolerance &&
                withinPercentageTolerance)
            {
                return CreateResult(
                    difference,
                    absoluteDifference,
                    differencePercent,
                    isMatch: true,
                    GridComparisonMatchReason.WithinCombinedTolerance);
            }

            if (withinAbsoluteTolerance)
            {
                return CreateResult(
                    difference,
                    absoluteDifference,
                    differencePercent,
                    isMatch: true,
                    GridComparisonMatchReason.WithinAbsoluteTolerance);
            }

            if (withinPercentageTolerance)
            {
                return CreateResult(
                    difference,
                    absoluteDifference,
                    differencePercent,
                    isMatch: true,
                    GridComparisonMatchReason.WithinPercentageTolerance);
            }

            // Zero-baseline rule:
            // - A = 0, B = 0 is already ExactMatch above.
            // - A = 0, B != 0 has no meaningful percentage difference.
            // - Absolute tolerance may still produce a valid match.
            // - Percentage-only tolerance cannot match a non-zero B.
            return CreateResult(
                difference,
                absoluteDifference,
                differencePercent,
                isMatch: false,
                GridComparisonMatchReason.DifferenceDetected);
        }

        private static GridComparisonEvaluation CreateResult(
            decimal difference,
            decimal absoluteDifference,
            decimal? differencePercent,
            bool isMatch,
            GridComparisonMatchReason matchReason)
        {
            return new GridComparisonEvaluation
            {
                Difference = difference,
                AbsoluteDifference = absoluteDifference,
                DifferencePercent = differencePercent,
                IsMatch = isMatch,
                MatchReason = matchReason
            };
        }
    }
}
