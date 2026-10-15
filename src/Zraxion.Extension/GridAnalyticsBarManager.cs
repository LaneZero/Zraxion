using Microsoft.SqlServer.Management.UI.Grid;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Zraxion.Extension
{
    internal static class GridAnalyticsBarManager
    {
        private static readonly HashSet<GridControl> subscribedGrids =
            new HashSet<GridControl>();

        private static ZraxionAnalyticsBar? analyticsBar;
        private static Control? analyticsHost;
        private static GridControl? activeGrid;

        private static Padding originalHostPadding;
        private static bool hostPaddingCaptured;

        public static void AttachToCurrentResults(
            bool showSelectionMetrics = true)
        {
            var grid =
                ResultsGridProbe.GetFocusedResultsGridForUi();

            if (grid is null)
            {
                return;
            }

            // Do not shadow the static activeGrid field.
            // The manager must know the active grid immediately on first attach.
            activeGrid = grid;

            var host = FindResultsHost(grid);

            if (host is null)
            {
                return;
            }

            EnsureAnalyticsBar(host);

            if (showSelectionMetrics &&
                analyticsBar is not null &&
                !analyticsBar.IsDisposed)
            {
                analyticsBar.ShowSelectionMetricsView();
            }

            SubscribeToResultGrids(grid);

            // SSMS may still be completing the Results layout during the first
            // attach. Refresh on the next UI turn so Result/metrics render fully.
            QueueUpdateFromGrid(grid);
        }

        private static Control? FindResultsHost(GridControl grid)
        {
            Control? current = grid;

            while (current is not null)
            {
                var typeName = current.GetType().FullName;

                if (typeName ==
                    "Microsoft.SqlServer.Management.UI.VSIntegration.Editors.SqlScriptEditorControl+DisplayResultsUserControl")
                {
                    return current;
                }

                current = current.Parent;
            }

            return null;
        }

        private static void EnsureAnalyticsBar(Control host)
        {
            if (ReferenceEquals(analyticsHost, host) &&
                analyticsBar is not null &&
                !analyticsBar.IsDisposed)
            {
                PositionAnalyticsBar(host);
                return;
            }

            // If we previously attached to another Results host,
            // restore its original layout first.
            if (analyticsHost is not null &&
                hostPaddingCaptured &&
                !analyticsHost.IsDisposed)
            {
                analyticsHost.Padding = originalHostPadding;
            }

            analyticsHost = host;

            var existing =
                host.Controls["ZraxionAnalyticsBar"]
                    as ZraxionAnalyticsBar;

            if (existing is not null)
            {
                analyticsBar = existing;
            }
            else
            {
                analyticsBar = new ZraxionAnalyticsBar();
                host.Controls.Add(analyticsBar);
                analyticsBar.BringToFront();
            }

            analyticsBar.AutoRefreshChanged -= OnAutoRefreshChanged;
            analyticsBar.AutoRefreshChanged += OnAutoRefreshChanged;

            analyticsBar.RefreshRequested -= OnRefreshRequested;
            analyticsBar.RefreshRequested += OnRefreshRequested;

            originalHostPadding = host.Padding;
            hostPaddingCaptured = true;

            // Reserve real space below the SSMS Results control.
            host.Padding = new Padding(
                originalHostPadding.Left,
                originalHostPadding.Top,
                originalHostPadding.Right,
                originalHostPadding.Bottom + analyticsBar.Height);

            PositionAnalyticsBar(host);

            host.Resize -= OnResultsHostResize;
            host.Resize += OnResultsHostResize;

            host.PerformLayout();
        }

        private static void OnResultsHostResize(
            object? sender,
            EventArgs e)
        {
            if (sender is Control host)
            {
                PositionAnalyticsBar(host);
            }
        }

        private static void OnAutoRefreshChanged(
            object? sender,
            EventArgs e)
        {
            if (analyticsBar?.AutoRefreshEnabled == true &&
                activeGrid is not null)
            {
                UpdateFromGrid(activeGrid);
            }
        }

        private static void OnRefreshRequested(
            object? sender,
            EventArgs e)
        {
            if (activeGrid is not null)
            {
                UpdateFromGrid(activeGrid);
            }
        }

        private static void PositionAnalyticsBar(Control host)
        {
            if (analyticsBar is null ||
                analyticsBar.IsDisposed)
            {
                return;
            }

            var left = originalHostPadding.Left;

            var width =
                host.ClientSize.Width -
                originalHostPadding.Left -
                originalHostPadding.Right;

            var top =
                host.ClientSize.Height -
                originalHostPadding.Bottom -
                analyticsBar.Height;

            analyticsBar.SetBounds(
                left,
                top,
                Math.Max(0, width),
                analyticsBar.Height);

            analyticsBar.BringToFront();
        }

        private static void SubscribeToResultGrids(
            GridControl activeGrid)
        {
            var smartPanel = activeGrid.Parent;

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

        private static void Subscribe(GridControl grid)
        {
            if (!subscribedGrids.Add(grid))
            {
                return;
            }

            grid.SelectionChanged += OnGridSelectionChanged;
            grid.GotFocus += OnGridGotFocus;
            grid.Disposed += OnGridDisposed;
        }

        private static void OnGridSelectionChanged(
            object sender,
            EventArgs e)
        {
            if (sender is not GridControl grid)
            {
                return;
            }

            activeGrid = grid;

            if (analyticsBar?.AutoRefreshEnabled == true)
            {
                UpdateFromGrid(grid);
            }
        }

        private static void OnGridGotFocus(
            object? sender,
            EventArgs e)
        {
            if (sender is not GridControl grid)
            {
                return;
            }

            activeGrid = grid;

            if (analyticsBar?.AutoRefreshEnabled == true)
            {
                UpdateFromGrid(grid);
            }
        }

        private static void OnGridDisposed(
            object? sender,
            EventArgs e)
        {
            if (sender is not GridControl grid)
            {
                return;
            }

            grid.SelectionChanged -= OnGridSelectionChanged;
            grid.GotFocus -= OnGridGotFocus;
            grid.Disposed -= OnGridDisposed;

            subscribedGrids.Remove(grid);

            if (ReferenceEquals(activeGrid, grid))
            {
                activeGrid = null;
            }
        }

        private static void QueueUpdateFromGrid(GridControl grid)
        {
            var host = analyticsHost;

            if (host is null ||
                host.IsDisposed ||
                !host.IsHandleCreated)
            {
                UpdateFromGrid(grid);
                return;
            }

            try
            {
                host.BeginInvoke(
                    new Action(
                        () =>
                        {
                            if (host.IsDisposed ||
                                grid.IsDisposed)
                            {
                                return;
                            }

                            host.PerformLayout();
                            PositionAnalyticsBar(host);
                            UpdateFromGrid(grid);
                        }));
            }
            catch (InvalidOperationException)
            {
                // The host can be rebuilding while a query/results pane changes.
                // Fall back to an immediate refresh rather than losing the bar.
                if (!grid.IsDisposed)
                {
                    UpdateFromGrid(grid);
                }
            }
        }

        private static void UpdateFromGrid(GridControl grid)
        {
            activeGrid = grid;

            if (analyticsBar is null ||
                analyticsBar.IsDisposed)
            {
                return;
            }

            try
            {
                var summary =
                    GridSelectionAnalytics.Analyze(grid);

                var resultIndex =
                    ResultsGridProbe.GetResultGridIndexForUi(grid);

                analyticsBar.UpdateAnalytics(
                    resultIndex,
                    summary);
            }
            catch (OverflowException)
            {
                analyticsBar.Reset();
            }
            catch (InvalidOperationException)
            {
                analyticsBar.Reset();
            }
        }

        internal static void ShowComparison(
            GridAnalyticsComparison comparison)
        {
            if (comparison == null)
            {
                throw new ArgumentNullException(
                    nameof(comparison));
            }

            // Compare must work even when the user has never invoked
            // "Inspect Results Grid". Attach the bar on demand.
            if (analyticsBar is null ||
                analyticsBar.IsDisposed)
            {
                AttachToCurrentResults(
                    showSelectionMetrics: false);
            }

            var bar = analyticsBar;
            var host = analyticsHost;

            if (bar is null ||
                bar.IsDisposed)
            {
                return;
            }

            // If AttachToCurrentResults just queued the first analytics refresh,
            // queue Compare after it. This prevents that refresh from immediately
            // hiding the comparison and also lets the first layout finish.
            if (host is not null &&
                !host.IsDisposed &&
                host.IsHandleCreated)
            {
                try
                {
                    host.BeginInvoke(
                        new Action(
                            () =>
                            {
                                if (bar.IsDisposed)
                                {
                                    return;
                                }

                                bar.ShowComparison(comparison);
                                PositionAnalyticsBar(host);
                            }));

                    return;
                }
                catch (InvalidOperationException)
                {
                    // Fall through to direct display.
                }
            }

            bar.ShowComparison(comparison);
        }
    }
}
