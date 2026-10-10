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

        public GridAnalyticsComparison(
            GridAnalyticsSnapshot left,
            GridAnalyticsSnapshot right)
        {
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));

            Difference = Right.Sum - Left.Sum;
            AbsoluteDifference = Math.Abs(Difference);

            DifferencePercent =
                Left.Sum == 0m
                    ? null
                    : (Difference / Left.Sum) * 100m;

            IsMatch = Difference == 0m;
        }
    }
}