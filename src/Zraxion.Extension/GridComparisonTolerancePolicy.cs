using System;

namespace Zraxion.Extension
{
    internal sealed class GridComparisonTolerancePolicy
    {
        public static GridComparisonTolerancePolicy Exact { get; } =
            new GridComparisonTolerancePolicy(
                absoluteTolerance: null,
                percentageTolerance: null);

        public decimal? AbsoluteTolerance { get; }

        public decimal? PercentageTolerance { get; }

        public bool IsExact =>
            !AbsoluteTolerance.HasValue &&
            !PercentageTolerance.HasValue;

        public GridComparisonTolerancePolicy(
            decimal? absoluteTolerance,
            decimal? percentageTolerance)
        {
            if (absoluteTolerance.HasValue &&
                absoluteTolerance.Value < 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(absoluteTolerance),
                    "Absolute tolerance cannot be negative.");
            }

            if (percentageTolerance.HasValue &&
                percentageTolerance.Value < 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(percentageTolerance),
                    "Percentage tolerance cannot be negative.");
            }

            AbsoluteTolerance = absoluteTolerance;
            PercentageTolerance = percentageTolerance;
        }
    }
}
