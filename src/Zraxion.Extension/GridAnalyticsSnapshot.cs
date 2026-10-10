using System;

namespace Zraxion.Extension
{
    internal sealed class GridAnalyticsSnapshot
    {
        public string Label { get; set; } = string.Empty;

        public decimal Sum { get; set; }

        public decimal? Average { get; set; }

        public decimal? Min { get; set; }

        public decimal? Max { get; set; }

        public long NumericCount { get; set; }

        public long SelectedCount { get; set; }

        public long NullOrEmptyCount { get; set; }

        public long NonNumericCount { get; set; }

        public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    }
}