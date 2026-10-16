using Microsoft.SqlServer.Management.UI.Grid;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Zraxion.Extension
{
    internal static class GridAnalyticsBarManager
    {
        private sealed class HostState
        {
            public HostState(
                Control host,
                ZraxionAnalyticsBar bar,
                Padding originalPadding)
            {
                Host = host;
                Bar = bar;
                OriginalPadding = originalPadding;
            }

            public Control Host { get; }

            public ZraxionAnalyticsBar Bar { get; }

            public Padding OriginalPadding { get; }

            public GridControl? ActiveGrid { get; set; }

            public bool PaddingReserved { get; set; }
        }

        private static readonly HashSet<GridControl> subscribedGrids =
            new HashSet<GridControl>();

        private static readonly Dictionary<Control, HostState> hostStates =
            new Dictionary<Control, HostState>();

        private static HostState? activeHostState;

        public static void AttachToCurrentResults(
            bool showSelectionMetrics = true)
        {
            if (!TryActivateFocusedResults(
                    out var state,
                    out var grid))
            {
                return;
            }

            if (showSelectionMetrics)
            {
                state.Bar.ShowSelectionMetricsView();
            }

            SubscribeToResultGrids(grid);

            // SSMS may still be completing the Results layout during the first
            // attach. Refresh on the next UI turn so Result/metrics render fully.
            QueueUpdateFromGrid(grid);
        }

        private static bool TryActivateFocusedResults(
            out HostState state,
            out GridControl grid)
        {
            state = null!;
            grid = null!;

            var focusedGrid =
                ResultsGridProbe.GetFocusedResultsGridForUi();

            if (focusedGrid is null ||
                focusedGrid.IsDisposed)
            {
                return false;
            }

            var host =
                FindResultsHost(focusedGrid);

            if (host is null ||
                host.IsDisposed)
            {
                return false;
            }

            var hostState =
                EnsureHostState(host);

            hostState.ActiveGrid =
                focusedGrid;

            activeHostState =
                hostState;

            state = hostState;
            grid = focusedGrid;

            SubscribeToResultGrids(
                focusedGrid);

            return true;
        }

        private static Control? FindResultsHost(
            GridControl grid)
        {
            Control? current = grid;

            while (current is not null)
            {
                var typeName =
                    current.GetType().FullName;

                if (typeName ==
                    "Microsoft.SqlServer.Management.UI.VSIntegration.Editors.SqlScriptEditorControl+DisplayResultsUserControl")
                {
                    return current;
                }

                current = current.Parent;
            }

            return null;
        }

        internal static object? GetComparisonContextForGrid(
            GridControl grid)
        {
            if (grid is null)
            {
                throw new ArgumentNullException(
                    nameof(grid));
            }

            return FindResultsHost(grid);
        }

        internal static object? GetCurrentComparisonContext()
        {
            var grid =
                ResultsGridProbe.GetFocusedResultsGridForUi();

            return
                grid is null
                    ? null
                    : FindResultsHost(grid);
        }

        private static HostState EnsureHostState(
            Control host)
        {
            if (hostStates.TryGetValue(
                    host,
                    out var existingState))
            {
                if (!existingState.Bar.IsDisposed)
                {
                    PositionAnalyticsBar(
                        existingState);

                    return existingState;
                }

                hostStates.Remove(host);
            }

            var existingBar =
                host.Controls["ZraxionAnalyticsBar"]
                    as ZraxionAnalyticsBar;

            var bar =
                existingBar ??
                new ZraxionAnalyticsBar();

            if (existingBar is null)
            {
                host.Controls.Add(bar);
                bar.BringToFront();
            }

            var state =
                new HostState(
                    host,
                    bar,
                    host.Padding);

            hostStates[host] =
                state;

            bar.AutoRefreshChanged -=
                OnAutoRefreshChanged;
            bar.AutoRefreshChanged +=
                OnAutoRefreshChanged;

            bar.RefreshRequested -=
                OnRefreshRequested;
            bar.RefreshRequested +=
                OnRefreshRequested;

            ReserveHostPadding(state);

            host.Resize -=
                OnResultsHostResize;
            host.Resize +=
                OnResultsHostResize;

            host.Disposed -=
                OnResultsHostDisposed;
            host.Disposed +=
                OnResultsHostDisposed;

            PositionAnalyticsBar(state);
            host.PerformLayout();

            return state;
        }

        private static void ReserveHostPadding(
            HostState state)
        {
            if (state.PaddingReserved ||
                state.Host.IsDisposed ||
                state.Bar.IsDisposed)
            {
                return;
            }

            var original =
                state.OriginalPadding;

            state.Host.Padding =
                new Padding(
                    original.Left,
                    original.Top,
                    original.Right,
                    original.Bottom +
                    state.Bar.Height);

            state.PaddingReserved = true;
        }

        private static void OnResultsHostResize(
            object? sender,
            EventArgs e)
        {
            if (sender is not Control host ||
                !hostStates.TryGetValue(
                    host,
                    out var state))
            {
                return;
            }

            PositionAnalyticsBar(state);
        }

        private static void OnResultsHostDisposed(
            object? sender,
            EventArgs e)
        {
            if (sender is not Control host)
            {
                return;
            }

            host.Resize -=
                OnResultsHostResize;
            host.Disposed -=
                OnResultsHostDisposed;

            if (!hostStates.TryGetValue(
                    host,
                    out var state))
            {
                return;
            }

            state.Bar.AutoRefreshChanged -=
                OnAutoRefreshChanged;
            state.Bar.RefreshRequested -=
                OnRefreshRequested;

            hostStates.Remove(host);

            if (ReferenceEquals(
                    activeHostState,
                    state))
            {
                activeHostState = null;
            }
        }

        private static void OnAutoRefreshChanged(
            object? sender,
            EventArgs e)
        {
            var state =
                FindHostStateByBar(sender);

            if (state is null ||
                !state.Bar.AutoRefreshEnabled ||
                state.ActiveGrid is null ||
                state.ActiveGrid.IsDisposed)
            {
                return;
            }

            UpdateFromGrid(
                state,
                state.ActiveGrid);
        }

        private static void OnRefreshRequested(
            object? sender,
            EventArgs e)
        {
            var state =
                FindHostStateByBar(sender);

            if (state?.ActiveGrid is null ||
                state.ActiveGrid.IsDisposed)
            {
                return;
            }

            UpdateFromGrid(
                state,
                state.ActiveGrid);
        }

        private static HostState? FindHostStateByBar(
            object? sender)
        {
            if (sender is not ZraxionAnalyticsBar bar)
            {
                return null;
            }

            foreach (var state in hostStates.Values)
            {
                if (ReferenceEquals(
                        state.Bar,
                        bar))
                {
                    return state;
                }
            }

            return null;
        }

        private static void PositionAnalyticsBar(
            HostState state)
        {
            var host =
                state.Host;

            var bar =
                state.Bar;

            if (host.IsDisposed ||
                bar.IsDisposed)
            {
                return;
            }

            var original =
                state.OriginalPadding;

            var left =
                original.Left;

            var width =
                host.ClientSize.Width -
                original.Left -
                original.Right;

            var top =
                host.ClientSize.Height -
                original.Bottom -
                bar.Height;

            bar.SetBounds(
                left,
                top,
                Math.Max(0, width),
                bar.Height);

            bar.BringToFront();
        }

        private static void SubscribeToResultGrids(
            GridControl activeGrid)
        {
            var smartPanel =
                activeGrid.Parent;

            if (smartPanel is null)
            {
                Subscribe(activeGrid);
                return;
            }

            foreach (Control child in smartPanel.Controls)
            {
                if (child is GridControl grid)
                {
                    Subscribe(grid);
                }
            }
        }

        private static void Subscribe(
            GridControl grid)
        {
            if (!subscribedGrids.Add(grid))
            {
                return;
            }

            grid.SelectionChanged +=
                OnGridSelectionChanged;
            grid.GotFocus +=
                OnGridGotFocus;
            grid.Disposed +=
                OnGridDisposed;
        }

        private static void OnGridSelectionChanged(
            object sender,
            EventArgs e)
        {
            if (sender is not GridControl grid ||
                grid.IsDisposed)
            {
                return;
            }

            var state =
                TryGetHostStateForGrid(grid);

            if (state is null)
            {
                return;
            }

            state.ActiveGrid =
                grid;

            // A SelectionChanged event from a hidden/inactive Results host must
            // never steal the active host from the tab the user is working in.
            if (grid.Focused ||
                grid.ContainsFocus)
            {
                activeHostState =
                    state;
            }

            if (state.Bar.AutoRefreshEnabled)
            {
                UpdateFromGrid(
                    state,
                    grid);
            }
        }

        private static void OnGridGotFocus(
            object? sender,
            EventArgs e)
        {
            if (sender is not GridControl grid ||
                grid.IsDisposed)
            {
                return;
            }

            var host =
                FindResultsHost(grid);

            if (host is null ||
                host.IsDisposed)
            {
                return;
            }

            var state =
                EnsureHostState(host);

            state.ActiveGrid =
                grid;

            activeHostState =
                state;

            SubscribeToResultGrids(grid);

            if (state.Bar.AutoRefreshEnabled)
            {
                UpdateFromGrid(
                    state,
                    grid);
            }
            else
            {
                PositionAnalyticsBar(state);
            }
        }

        private static HostState? TryGetHostStateForGrid(
            GridControl grid)
        {
            var host =
                FindResultsHost(grid);

            if (host is null ||
                host.IsDisposed)
            {
                return null;
            }

            return
                hostStates.TryGetValue(
                    host,
                    out var state)
                    ? state
                    : null;
        }

        private static void OnGridDisposed(
            object? sender,
            EventArgs e)
        {
            if (sender is not GridControl grid)
            {
                return;
            }

            grid.SelectionChanged -=
                OnGridSelectionChanged;
            grid.GotFocus -=
                OnGridGotFocus;
            grid.Disposed -=
                OnGridDisposed;

            subscribedGrids.Remove(grid);

            foreach (var state in hostStates.Values)
            {
                if (ReferenceEquals(
                        state.ActiveGrid,
                        grid))
                {
                    state.ActiveGrid = null;
                }
            }
        }

        private static void QueueUpdateFromGrid(
            GridControl grid)
        {
            var state =
                TryGetHostStateForGrid(grid);

            if (state is null)
            {
                return;
            }

            var host =
                state.Host;

            if (host.IsDisposed ||
                !host.IsHandleCreated)
            {
                UpdateFromGrid(
                    state,
                    grid);

                return;
            }

            try
            {
                host.BeginInvoke(
                    new Action(
                        () =>
                        {
                            if (host.IsDisposed ||
                                grid.IsDisposed ||
                                state.Bar.IsDisposed)
                            {
                                return;
                            }

                            host.PerformLayout();
                            PositionAnalyticsBar(state);
                            UpdateFromGrid(
                                state,
                                grid);
                        }));
            }
            catch (InvalidOperationException)
            {
                // The host can be rebuilding while a query/results pane changes.
                if (!grid.IsDisposed)
                {
                    UpdateFromGrid(
                        state,
                        grid);
                }
            }
        }

        private static void UpdateFromGrid(
            HostState state,
            GridControl grid)
        {
            if (grid.IsDisposed ||
                state.Host.IsDisposed ||
                state.Bar.IsDisposed)
            {
                return;
            }

            state.ActiveGrid =
                grid;

            try
            {
                var summary =
                    GridSelectionAnalytics.Analyze(
                        grid);

                var resultIndex =
                    ResultsGridProbe
                        .GetResultGridIndexForUi(
                            grid);

                state.Bar.UpdateAnalytics(
                    resultIndex,
                    summary);
            }
            catch (InvalidOperationException)
            {
                state.Bar.Reset();
            }
        }

        internal static void ShowSelectionMetric(
            string metric)
        {
            if (string.IsNullOrWhiteSpace(metric))
            {
                throw new ArgumentException(
                    "A metric name is required.",
                    nameof(metric));
            }

            if (!TryActivateFocusedResults(
                    out var state,
                    out var grid))
            {
                return;
            }

            UpdateFromGrid(
                state,
                grid);

            state.Bar.ShowSelectionMetricView(
                metric);

            PositionAnalyticsBar(state);
        }

        internal static void ShowBaseline()
        {
            if (!TryActivateFocusedResults(
                    out var state,
                    out _))
            {
                return;
            }

            var baseline =
                GridComparisonSession.GetLeftSnapshot(
                    state.Host);

            if (baseline is null)
            {
                return;
            }

            var metadata =
                GridComparisonSession.GetLeftMetadata(
                    state.Host);

            var host =
                state.Host;

            var bar =
                state.Bar;

            if (host.IsHandleCreated)
            {
                try
                {
                    host.BeginInvoke(
                        new Action(
                            () =>
                            {
                                if (host.IsDisposed ||
                                    bar.IsDisposed)
                                {
                                    return;
                                }

                                bar.ShowBaseline(
                                    baseline,
                                    metadata);

                                PositionAnalyticsBar(
                                    state);
                            }));

                    return;
                }
                catch (InvalidOperationException)
                {
                    // Fall through to direct display.
                }
            }

            bar.ShowBaseline(
                baseline,
                metadata);

            PositionAnalyticsBar(state);
        }

        internal static void ClearComparisonView()
        {
            if (!TryActivateFocusedResults(
                    out var state,
                    out _))
            {
                return;
            }

            state.Bar.ClearComparison();
            PositionAnalyticsBar(state);
        }

        internal static void ShowComparison(
            GridAnalyticsComparison comparison)
        {
            if (comparison is null)
            {
                throw new ArgumentNullException(
                    nameof(comparison));
            }

            if (!TryActivateFocusedResults(
                    out var state,
                    out _))
            {
                return;
            }

            var host =
                state.Host;

            var bar =
                state.Bar;

            // Queue Compare after host activation/layout so a deferred selection
            // refresh cannot overwrite the comparison view.
            if (host.IsHandleCreated)
            {
                try
                {
                    host.BeginInvoke(
                        new Action(
                            () =>
                            {
                                if (host.IsDisposed ||
                                    bar.IsDisposed)
                                {
                                    return;
                                }

                                bar.ShowComparison(
                                    comparison);

                                PositionAnalyticsBar(
                                    state);
                            }));

                    return;
                }
                catch (InvalidOperationException)
                {
                    // Fall through to direct display.
                }
            }

            bar.ShowComparison(
                comparison);

            PositionAnalyticsBar(state);
        }
    }
}
