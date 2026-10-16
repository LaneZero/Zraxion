using System;
using System.Runtime.CompilerServices;

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

        private sealed class ComparisonState
        {
            public GridAnalyticsSnapshot? LeftSnapshot { get; set; }

            public BaselineMetadata? LeftMetadata { get; set; }

            public GridComparisonTolerancePolicy TolerancePolicy { get; set; } =
                GridComparisonTolerancePolicy.Exact;
        }

        private static readonly ConditionalWeakTable<object, ComparisonState> states =
            new ConditionalWeakTable<object, ComparisonState>();

        public static bool HasBaseline(
            object? context)
        {
            return
                GetState(
                    context,
                    create: false)?
                    .LeftSnapshot is not null;
        }

        public static GridAnalyticsSnapshot? GetLeftSnapshot(
            object? context)
        {
            return
                GetState(
                    context,
                    create: false)?
                    .LeftSnapshot;
        }

        public static BaselineMetadata? GetLeftMetadata(
            object? context)
        {
            return
                GetState(
                    context,
                    create: false)?
                    .LeftMetadata;
        }

        public static GridComparisonTolerancePolicy GetTolerancePolicy(
            object? context)
        {
            return
                GetState(
                    context,
                    create: false)?
                    .TolerancePolicy
                ?? GridComparisonTolerancePolicy.Exact;
        }

        public static void SetLeft(
            object context,
            GridAnalyticsSnapshot snapshot,
            int resultGridIndex,
            int? uiColumnIndex)
        {
            if (context is null)
            {
                throw new ArgumentNullException(
                    nameof(context));
            }

            if (snapshot is null)
            {
                throw new ArgumentNullException(
                    nameof(snapshot));
            }

            var state =
                GetState(
                    context,
                    create: true)!;

            // Commit snapshot + metadata together so a failed replacement can
            // never leave this Results host half-updated.
            state.LeftSnapshot =
                snapshot;

            state.LeftMetadata =
                new BaselineMetadata
                {
                    ResultGridIndex =
                        resultGridIndex,
                    UiColumnIndex =
                        uiColumnIndex,
                    CapturedAtUtc =
                        snapshot.CapturedAtUtc
                };
        }

        public static void SetTolerancePolicy(
            object context,
            GridComparisonTolerancePolicy tolerancePolicy)
        {
            if (context is null)
            {
                throw new ArgumentNullException(
                    nameof(context));
            }

            if (tolerancePolicy is null)
            {
                throw new ArgumentNullException(
                    nameof(tolerancePolicy));
            }

            GetState(
                context,
                create: true)!
                .TolerancePolicy =
                    tolerancePolicy;
        }

        public static void Clear(
            object? context)
        {
            var state =
                GetState(
                    context,
                    create: false);

            if (state is null)
            {
                return;
            }

            // Clearing A/Compare intentionally preserves this host's tolerance.
            state.LeftSnapshot = null;
            state.LeftMetadata = null;
        }

        private static ComparisonState? GetState(
            object? context,
            bool create)
        {
            if (context is null)
            {
                return null;
            }

            if (states.TryGetValue(
                    context,
                    out var existing))
            {
                return existing;
            }

            if (!create)
            {
                return null;
            }

            return
                states.GetValue(
                    context,
                    _ => new ComparisonState());
        }
    }
}
