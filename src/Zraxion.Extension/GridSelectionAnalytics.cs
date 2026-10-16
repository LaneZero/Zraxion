using Microsoft.SqlServer.Management.UI.Grid;
using System;
using System.Globalization;

namespace Zraxion.Extension
{
    internal static class GridSelectionAnalytics
    {
        internal sealed class NumericSelectionSummary
        {
            public decimal Sum { get; set; }

            public decimal? Min { get; set; }

            public decimal? Max { get; set; }

            public long SelectedCount { get; set; }

            public long NumericCount { get; set; }

            public long NullOrEmptyCount { get; set; }

            public long NonNumericCount { get; set; }

            public decimal? Average =>
                NumericCount > 0
                    ? Sum / NumericCount
                    : null;
        }

        public static NumericSelectionSummary Analyze(
            GridControl grid)
        {
            if (grid is null)
            {
                throw new ArgumentNullException(
                    nameof(grid));
            }

            var selectedBlocks =
                grid.SelectedCells;

            var storage =
                grid.GridStorage;

            if (storage is null)
            {
                throw new InvalidOperationException(
                    "The active Results Grid does not expose grid storage.");
            }

            var result =
                new NumericSelectionSummary();

            if (selectedBlocks is null ||
                selectedBlocks.Count == 0)
            {
                return result;
            }

            foreach (BlockOfCells block in selectedBlocks)
            {
                for (long row = block.Y;
                     row <= block.Bottom;
                     row++)
                {
                    for (var uiColumn = block.X;
                         uiColumn <= block.Right;
                         uiColumn++)
                    {
                        result.SelectedCount++;

                        var storageColumn =
                            grid.GetStorageColumnIndexByUIIndex(
                                uiColumn);

                        if (storageColumn < 0)
                        {
                            result.NonNumericCount++;
                            continue;
                        }

                        var rawValue =
                            storage.GetCellDataAsString(
                                row,
                                storageColumn);

                        AccumulateRawValue(
                            result,
                            rawValue);
                    }
                }
            }

            return result;
        }

        public static NumericSelectionSummary AnalyzeColumn(
            GridControl grid,
            int uiColumn)
        {
            if (grid is null)
            {
                throw new ArgumentNullException(
                    nameof(grid));
            }

            if (uiColumn < 0 ||
                uiColumn >= grid.ColumnsNumber)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(uiColumn));
            }

            var storage =
                grid.GridStorage;

            if (storage is null)
            {
                throw new InvalidOperationException(
                    "The active Results Grid does not expose grid storage.");
            }

            var storageColumn =
                grid.GetStorageColumnIndexByUIIndex(
                    uiColumn);

            if (storageColumn < 0)
            {
                throw new InvalidOperationException(
                    "The selected Results Grid column does not map to storage.");
            }

            var result =
                new NumericSelectionSummary();

            var rowCount =
                storage.NumRows();

            for (long row = 0;
                 row < rowCount;
                 row++)
            {
                result.SelectedCount++;

                var rawValue =
                    storage.GetCellDataAsString(
                        row,
                        storageColumn);

                AccumulateRawValue(
                    result,
                    rawValue);
            }

            return result;
        }

        private static void AccumulateRawValue(
            NumericSelectionSummary result,
            string? rawValue)
        {
            var normalizedValue =
                rawValue?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedValue) ||
                string.Equals(
                    normalizedValue,
                    "NULL",
                    StringComparison.OrdinalIgnoreCase))
            {
                result.NullOrEmptyCount++;
                return;
            }

            var styles =
                NumberStyles.Number |
                NumberStyles.AllowExponent |
                NumberStyles.AllowCurrencySymbol;

            decimal value;

            var parsed =
                decimal.TryParse(
                    normalizedValue,
                    styles,
                    CultureInfo.CurrentCulture,
                    out value)
                ||
                decimal.TryParse(
                    normalizedValue,
                    styles,
                    CultureInfo.InvariantCulture,
                    out value);

            if (!parsed)
            {
                result.NonNumericCount++;
                return;
            }

            checked
            {
                result.Sum += value;
            }

            if (!result.Min.HasValue ||
                value < result.Min.Value)
            {
                result.Min = value;
            }

            if (!result.Max.HasValue ||
                value > result.Max.Value)
            {
                result.Max = value;
            }

            result.NumericCount++;
        }

        public static GridAnalyticsSnapshot CreateSnapshot(
            GridControl grid,
            string label)
        {
            var summary =
                Analyze(grid);

            return new GridAnalyticsSnapshot
            {
                Label = label ?? string.Empty,
                Sum = summary.Sum,
                Average = summary.Average,
                Min = summary.Min,
                Max = summary.Max,
                NumericCount = summary.NumericCount,
                SelectedCount = summary.SelectedCount,
                NullOrEmptyCount = summary.NullOrEmptyCount,
                NonNumericCount = summary.NonNumericCount,
                CapturedAtUtc = DateTime.UtcNow
            };
        }
    }
}
