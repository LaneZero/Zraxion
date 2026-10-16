using System;

namespace Zraxion.Extension
{
    internal static class GridComparisonSession
    {
        internal sealed class BaselineMetadata
        {
            public int ResultGridIndex { get; set; }

            public int? UiColumnIndex { get; set; }

            public DateTime CapturedAtUtc { get; set; }
        }

        public static GridAnalyticsSnapshot? LeftSnapshot
        {
            get;
            private set;
        }

        public static BaselineMetadata? LeftMetadata
        {
            get;
            private set;
        }

        public static bool HasBaseline =>
            LeftSnapshot is not null;

        public static void SetLeft(
            GridAnalyticsSnapshot snapshot)
        {
            SetLeft(
                snapshot,
                resultGridIndex: -1,
                uiColumnIndex: null);
        }

        public static void SetLeft(
            GridAnalyticsSnapshot snapshot,
            int resultGridIndex,
            int? uiColumnIndex)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(
                    nameof(snapshot));
            }

            // Commit snapshot + metadata together so a failed replacement can
            // never leave the comparison session half-updated.
            LeftSnapshot = snapshot;

            LeftMetadata =
                new BaselineMetadata
                {
                    ResultGridIndex = resultGridIndex,
                    UiColumnIndex = uiColumnIndex,
                    CapturedAtUtc = snapshot.CapturedAtUtc
                };
        }

        public static void Clear()
        {
            LeftSnapshot = null;
            LeftMetadata = null;
        }
    }
}
