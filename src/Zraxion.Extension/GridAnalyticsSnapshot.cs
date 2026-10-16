using System;

namespace Zraxion.Extension
{
    internal sealed class GridAnalyticsSnapshot
    {
        public string Label { get; set; } = string.Empty;

        public ExactDecimal Sum { get; set; } = ExactDecimal.Zero;

        public ExactDecimal? Average { get; set; }

        public ExactDecimal? Min { get; set; }

        public ExactDecimal? Max { get; set; }

        public long NumericCount { get; set; }

        public long SelectedCount { get; set; }

        public long NullOrEmptyCount { get; set; }

        public long NonNumericCount { get; set; }

        public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    }
}