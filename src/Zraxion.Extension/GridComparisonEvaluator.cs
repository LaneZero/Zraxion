using System;

namespace Zraxion.Extension
{
    internal static class GridComparisonEvaluator
    {
        // SQL Server DECIMAL/NUMERIC supports up to 38 digits of precision.
        // A one-unit difference against a 38-digit baseline can produce a
        // percentage as small as roughly 1E-36, so 18 fractional digits can
        // incorrectly display/copy a non-zero difference as 0%.
        private const int DifferencePercentFractionalDigits = 38;

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

            var difference =
                right.Sum -
                left.Sum;

            var absoluteDifference =
                difference.Abs();

            ExactDecimal? differencePercent =
                null;

            // Percentage difference is undefined when A == 0.
            // For non-zero baselines use |A|, preserving B - A direction.
            if (!left.Sum.IsZero)
            {
                differencePercent =
                    difference
                        .Divide(
                            left.Sum.Abs(),
                            DifferencePercentFractionalDigits)
                        .Multiply(
                            100);
            }

            if (difference.IsZero)
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
                    ExactDecimal.FromDecimal(
                        tolerancePolicy.AbsoluteTolerance.Value);

            // Evaluate the tolerance boundary without using the rounded
            // DifferencePercent display value:
            //
            // |B-A| / |A| * 100 <= pctTolerance
            // => |B-A| * 100 <= |A| * pctTolerance
            var withinPercentageTolerance =
                tolerancePolicy.PercentageTolerance.HasValue &&
                !left.Sum.IsZero &&
                absoluteDifference.Multiply(100) <=
                    left.Sum
                        .Abs()
                        .Multiply(
                            ExactDecimal.FromDecimal(
                                tolerancePolicy
                                    .PercentageTolerance
                                    .Value));

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

            return CreateResult(
                difference,
                absoluteDifference,
                differencePercent,
                isMatch: false,
                GridComparisonMatchReason.DifferenceDetected);
        }

        private static GridComparisonEvaluation CreateResult(
            ExactDecimal difference,
            ExactDecimal absoluteDifference,
            ExactDecimal? differencePercent,
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
