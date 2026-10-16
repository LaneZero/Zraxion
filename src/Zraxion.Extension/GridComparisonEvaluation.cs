namespace Zraxion.Extension
{
    internal enum GridComparisonMatchReason
    {
        ExactMatch = 0,
        DifferenceDetected = 1,
        WithinAbsoluteTolerance = 2,
        WithinPercentageTolerance = 3,
        WithinCombinedTolerance = 4
    }

    internal sealed class GridComparisonEvaluation
    {
        public decimal Difference { get; set; }

        public decimal AbsoluteDifference { get; set; }

        public decimal? DifferencePercent { get; set; }

        public bool IsMatch { get; set; }

        public GridComparisonMatchReason MatchReason { get; set; }
    }
}
