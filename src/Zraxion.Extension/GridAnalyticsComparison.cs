using System;

namespace Zraxion.Extension
{
    internal sealed class GridAnalyticsComparison
    {
        public GridAnalyticsSnapshot Left { get; }

        public GridAnalyticsSnapshot Right { get; }

        public decimal Difference { get; }

        public decimal AbsoluteDifference { get; }

        public decimal? DifferencePercent { get; }

        public bool IsMatch { get; }

        public GridComparisonMatchReason MatchReason { get; }

        public GridComparisonTolerancePolicy TolerancePolicy { get; }

        public GridAnalyticsComparison(
            GridAnalyticsSnapshot left,
            GridAnalyticsSnapshot right)
            : this(
                left,
                right,
                GridComparisonTolerancePolicy.Exact)
        {
        }

        public GridAnalyticsComparison(
            GridAnalyticsSnapshot left,
            GridAnalyticsSnapshot right,
            GridComparisonTolerancePolicy tolerancePolicy)
        {
            Left =
                left ??
                throw new ArgumentNullException(
                    nameof(left));

            Right =
                right ??
                throw new ArgumentNullException(
                    nameof(right));

            TolerancePolicy =
                tolerancePolicy ??
                throw new ArgumentNullException(
                    nameof(tolerancePolicy));

            var evaluation =
                GridComparisonEvaluator.Evaluate(
                    Left,
                    Right,
                    TolerancePolicy);

            Difference = evaluation.Difference;
            AbsoluteDifference = evaluation.AbsoluteDifference;
            DifferencePercent = evaluation.DifferencePercent;
            IsMatch = evaluation.IsMatch;
            MatchReason = evaluation.MatchReason;
        }
    }
}
