using Microsoft.SqlServer.Management.UI.Grid;
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Zraxion.Extension
{
    internal static class GridColumnAnalyticsFooterManager
    {
        private const int GeometrySyncIntervalMs = 100;

        private static GridControl? activeGrid;
        private static GridColumnAnalyticsFooter? footer;
        private static Timer? geometrySyncTimer;

        private static int activeUiColumn = -1;
        private static long geometryRow = -1;

        private static bool suppressSelectionChanged;
        private static bool footerDependsOnSelection = true;
        private static bool useFirstVisibleRowForGeometry;

        internal static bool TryShowMetricForCurrentSelection(
            string metric,
            out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(metric))
            {
                error = "A metric name is required.";
                return false;
            }

            var normalizedMetric =
                metric.Trim().ToUpperInvariant();

            if (normalizedMetric != "SUM" &&
                normalizedMetric != "AVG" &&
                normalizedMetric != "COUNT" &&
                normalizedMetric != "MIN" &&
                normalizedMetric != "MAX")
            {
                error =
                    $"Unsupported selection metric '{normalizedMetric}'.";

                return false;
            }

            var grid =
                ResultsGridProbe.GetFocusedResultsGridForUi();

            if (grid is null)
            {
                error =
                    "No focused SSMS Results Grid was detected.";

                return false;
            }

            if (!TryGetSingleSelectedUiColumn(
                    grid,
                    out var uiColumn))
            {
                error =
                    "Select cells from exactly one Results Grid column.";

                Hide();
                return false;
            }

            GridSelectionAnalytics.NumericSelectionSummary summary;

            try
            {
                summary =
                    GridSelectionAnalytics.Analyze(grid);
            }
            catch (OverflowException)
            {
                error =
                    $"{normalizedMetric} could not be calculated because " +
                    "the selected numeric values exceed the supported decimal range.";

                Hide();
                return false;
            }
            catch (InvalidOperationException ex)
            {
                error = ex.Message;

                Hide();
                return false;
            }

            if (summary.SelectedCount == 0)
            {
                error =
                    "No cells are currently selected.";

                Hide();
                return false;
            }

            if (!TryFormatMetric(
                    normalizedMetric,
                    summary,
                    "the current selection",
                    out var formattedValue,
                    out var rawCopyValue,
                    out error))
            {
                Hide();
                return false;
            }

            var selectedBlocks =
                grid.SelectedCells;

            if (selectedBlocks is null ||
                selectedBlocks.Count == 0)
            {
                error =
                    "No cells are currently selected.";

                Hide();
                return false;
            }

            EnsureFooter(grid);

            if (footer is null)
            {
                error =
                    "Zraxion could not create the column analytics footer.";

                return false;
            }

            activeUiColumn = uiColumn;
            geometryRow = selectedBlocks[0].Y;
            footerDependsOnSelection = true;
            useFirstVisibleRowForGeometry = false;

            footer.SetValue(
                normalizedMetric,
                formattedValue,
                rawCopyValue);

            suppressSelectionChanged = true;

            try
            {
                if (!TrySynchronizeFooterGeometry())
                {
                    error =
                        "The selected column is not currently visible.";

                    Hide();
                    return false;
                }

                footer.Visible = true;
                footer.BringToFront();

                StartGeometrySync();

                return true;
            }
            finally
            {
                suppressSelectionChanged = false;
            footerDependsOnSelection = true;
            useFirstVisibleRowForGeometry = false;
            }
        }

        internal static bool TryShowMetricForCurrentColumn(
            string metric,
            out string error)
        {
            error = string.Empty;

            var grid =
                ResultsGridProbe.GetFocusedResultsGridForUi();

            if (grid is null)
            {
                error =
                    "No focused SSMS Results Grid was detected.";

                return false;
            }

            var selectedBlocks =
                grid.SelectedCells;

            if (selectedBlocks is null ||
                selectedBlocks.Count == 0)
            {
                error =
                    "Right-click a cell in the Results Grid column you want to analyze.";

                return false;
            }

            var uiColumn =
                selectedBlocks[0].X;

            foreach (BlockOfCells block in selectedBlocks)
            {
                if (block.X != uiColumn ||
                    block.Right != uiColumn)
                {
                    error =
                        "Column Analytics requires the current selection to be in one column.";

                    return false;
                }
            }

            return TryShowMetricForWholeColumn(
                grid,
                uiColumn,
                metric,
                out error);
        }

        internal static bool TryShowMetricForWholeColumn(
            GridControl grid,
            int uiColumn,
            string metric,
            out string error)
        {
            error = string.Empty;

            if (grid is null)
            {
                error =
                    "No SSMS Results Grid was available.";

                return false;
            }

            if (grid.IsDisposed)
            {
                error =
                    "The SSMS Results Grid is no longer available.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(metric))
            {
                error =
                    "A metric name is required.";

                return false;
            }

            var normalizedMetric =
                metric.Trim().ToUpperInvariant();

            if (!IsSupportedMetric(
                    normalizedMetric))
            {
                error =
                    $"Unsupported column metric '{normalizedMetric}'.";

                return false;
            }

            GridSelectionAnalytics.NumericSelectionSummary summary;

            try
            {
                summary =
                    GridSelectionAnalytics.AnalyzeColumn(
                        grid,
                        uiColumn);
            }
            catch (OverflowException)
            {
                error =
                    $"{normalizedMetric} could not be calculated because " +
                    "the column's numeric values exceed the supported decimal range.";

                Hide();
                return false;
            }
            catch (Exception ex)
                when (ex is InvalidOperationException ||
                      ex is ArgumentOutOfRangeException)
            {
                error = ex.Message;

                Hide();
                return false;
            }

            if (summary.SelectedCount == 0)
            {
                error =
                    "The selected Results Grid column contains no rows.";

                Hide();
                return false;
            }

            if (!TryFormatMetric(
                    normalizedMetric,
                    summary,
                    "the selected column",
                    out var formattedValue,
                    out var rawCopyValue,
                    out error))
            {
                Hide();
                return false;
            }

            EnsureFooter(grid);

            if (footer is null)
            {
                error =
                    "Zraxion could not create the column analytics footer.";

                return false;
            }

            activeUiColumn = uiColumn;
            geometryRow = 0;
            footerDependsOnSelection = false;
            useFirstVisibleRowForGeometry = true;

            footer.SetValue(
                normalizedMetric,
                formattedValue,
                rawCopyValue);

            if (!TrySynchronizeFooterGeometry())
            {
                error =
                    "The selected column is not currently visible.";

                Hide();
                return false;
            }

            footer.Visible = true;
            footer.BringToFront();

            StartGeometrySync();

            return true;
        }

        private static bool IsSupportedMetric(
            string normalizedMetric)
        {
            return normalizedMetric == "SUM" ||
                   normalizedMetric == "AVG" ||
                   normalizedMetric == "COUNT" ||
                   normalizedMetric == "MIN" ||
                   normalizedMetric == "MAX";
        }

        private static bool TryFormatMetric(
            string normalizedMetric,
            GridSelectionAnalytics.NumericSelectionSummary summary,
            string scopeDescription,
            out string formattedValue,
            out string rawCopyValue,
            out string error)
        {
            formattedValue = string.Empty;
            rawCopyValue = string.Empty;
            error = string.Empty;

            if (!IsSupportedMetric(
                    normalizedMetric))
            {
                error =
                    $"Unsupported metric '{normalizedMetric}'.";

                return false;
            }

            if (normalizedMetric == "COUNT")
            {
                formattedValue =
                    summary.NumericCount.ToString(
                        "N0",
                        CultureInfo.CurrentCulture);

                rawCopyValue =
                    summary.NumericCount.ToString(
                        CultureInfo.InvariantCulture);

                return true;
            }

            if (summary.NumericCount == 0)
            {
                error =
                    $"No numeric values were found in {scopeDescription}.";

                return false;
            }

            decimal? value =
                normalizedMetric == "SUM"
                    ? summary.Sum
                    : normalizedMetric == "AVG"
                        ? summary.Average
                        : normalizedMetric == "MIN"
                            ? summary.Min
                            : summary.Max;

            if (!value.HasValue)
            {
                error =
                    $"{normalizedMetric} could not be calculated for {scopeDescription}.";

                return false;
            }

            formattedValue =
                value.Value.ToString(
                    "#,##0.############################",
                    CultureInfo.CurrentCulture);

            rawCopyValue =
                value.Value.ToString(
                    "G29",
                    CultureInfo.InvariantCulture);

            return true;
        }

        internal static void Hide()
        {
            StopGeometrySync();

            if (footer is null ||
                footer.IsDisposed)
            {
                return;
            }

            footer.Visible = false;
        }

        private static bool TryGetSingleSelectedUiColumn(
            GridControl grid,
            out int uiColumn)
        {
            uiColumn = -1;

            var selectedBlocks =
                grid.SelectedCells;

            if (selectedBlocks is null ||
                selectedBlocks.Count == 0)
            {
                return false;
            }

            foreach (BlockOfCells block in selectedBlocks)
            {
                if (block.Width != 1)
                {
                    return false;
                }

                if (uiColumn < 0)
                {
                    uiColumn = block.X;
                    continue;
                }

                if (block.X != uiColumn)
                {
                    return false;
                }
            }

            return uiColumn >= 0;
        }

        private static void EnsureFooter(
            GridControl grid)
        {
            if (ReferenceEquals(activeGrid, grid) &&
                footer is not null &&
                !footer.IsDisposed)
            {
                return;
            }

            Detach();

            activeGrid = grid;
            footer = new GridColumnAnalyticsFooter();

            grid.Controls.Add(footer);

            grid.SelectionChanged +=
                OnGridSelectionChanged;

            grid.SizeChanged +=
                OnGridGeometryChanged;

            grid.Layout +=
                OnGridLayout;

            grid.MouseWheel +=
                OnGridMouseWheel;

            grid.Disposed +=
                OnGridDisposed;

            geometrySyncTimer =
                new Timer
                {
                    Interval = GeometrySyncIntervalMs
                };

            geometrySyncTimer.Tick +=
                OnGeometrySyncTimerTick;
        }

        private static void StartGeometrySync()
        {
            if (geometrySyncTimer is null)
            {
                return;
            }

            geometrySyncTimer.Start();
        }

        private static void StopGeometrySync()
        {
            geometrySyncTimer?.Stop();
        }

        private static void OnGridSelectionChanged(
            object sender,
            EventArgs e)
        {
            if (suppressSelectionChanged)
            {
                return;
            }

            if (!ReferenceEquals(
                    sender,
                    activeGrid))
            {
                return;
            }

            if (!footerDependsOnSelection)
            {
                return;
            }

            // A selection footer is a snapshot of the selection at the time the
            // metric command was executed. Never leave a stale value visible.
            Hide();
        }

        private static void OnGridGeometryChanged(
            object? sender,
            EventArgs e)
        {
            SynchronizeFooterGeometry();
        }

        private static void OnGridLayout(
            object? sender,
            LayoutEventArgs e)
        {
            SynchronizeFooterGeometry();
        }

        private static void OnGridMouseWheel(
            object? sender,
            MouseEventArgs e)
        {
            QueueGeometrySync();
        }

        private static void OnGeometrySyncTimerTick(
            object? sender,
            EventArgs e)
        {
            SynchronizeFooterGeometry();
        }

        private static void QueueGeometrySync()
        {
            var grid = activeGrid;

            if (grid is null ||
                grid.IsDisposed ||
                !grid.IsHandleCreated)
            {
                return;
            }

            try
            {
                grid.BeginInvoke(
                    new Action(
                        SynchronizeFooterGeometry));
            }
            catch (InvalidOperationException)
            {
                // SSMS may be rebuilding the Results surface.
            }
        }

        private static void SynchronizeFooterGeometry()
        {
            if (footer is null ||
                footer.IsDisposed)
            {
                return;
            }

            if (!TrySynchronizeFooterGeometry())
            {
                footer.Visible = false;
                return;
            }

            footer.Visible = true;
            footer.BringToFront();
        }

        private static bool TrySynchronizeFooterGeometry()
        {
            var grid = activeGrid;
            var currentFooter = footer;

            if (grid is null ||
                grid.IsDisposed ||
                currentFooter is null ||
                currentFooter.IsDisposed ||
                activeUiColumn < 0 ||
                geometryRow < 0)
            {
                return false;
            }

            var effectiveGeometryRow =
                geometryRow;

            if (useFirstVisibleRowForGeometry)
            {
                var storage =
                    grid.GridStorage;

                var rowCount =
                    storage?.NumRows() ?? 0;

                if (rowCount <= 0)
                {
                    return false;
                }

                effectiveGeometryRow =
                    Math.Min(
                        (long)grid.FirstScrollableRow,
                        rowCount - 1);
            }

            Rectangle cellRectangle;

            try
            {
                cellRectangle =
                    grid.GetVisibleCellRectangle(
                        effectiveGeometryRow,
                        activeUiColumn);
            }
            catch
            {
                return false;
            }

            if (cellRectangle.Width <= 0 ||
                cellRectangle.Height <= 0)
            {
                return false;
            }

            var clientRectangle =
                grid.ClientRectangle;

            var visibleColumnLeft =
                Math.Max(
                    clientRectangle.Left,
                    cellRectangle.Left);

            var visibleColumnRight =
                Math.Min(
                    clientRectangle.Right,
                    cellRectangle.Right);

            if (visibleColumnRight <=
                visibleColumnLeft)
            {
                return false;
            }

            PositionFooter(
                grid,
                cellRectangle.Left,
                cellRectangle.Width);

            return true;
        }

        private static void PositionFooter(
            GridControl grid,
            int columnLeft,
            int columnWidth)
        {
            if (footer is null ||
                footer.IsDisposed)
            {
                return;
            }

            var clientWidth =
                grid.ClientSize.Width;

            if (clientWidth <= 0)
            {
                footer.Visible = false;
                return;
            }

            var desiredWidth =
                Math.Max(
                    columnWidth,
                    footer.PreferredContentWidth);

            var width =
                Math.Min(
                    desiredWidth,
                    clientWidth);

            var left =
                Math.Max(
                    0,
                    columnLeft);

            if (left + width >
                clientWidth)
            {
                left =
                    Math.Max(
                        0,
                        clientWidth - width);
            }

            var top =
                Math.Max(
                    0,
                    grid.ClientSize.Height -
                    footer.Height);

            footer.SetBounds(
                left,
                top,
                width,
                footer.Height);
        }

        private static void OnGridDisposed(
            object? sender,
            EventArgs e)
        {
            Detach();
        }

        private static void Detach()
        {
            StopGeometrySync();

            if (activeGrid is not null &&
                !activeGrid.IsDisposed)
            {
                activeGrid.SelectionChanged -=
                    OnGridSelectionChanged;

                activeGrid.SizeChanged -=
                    OnGridGeometryChanged;

                activeGrid.Layout -=
                    OnGridLayout;

                activeGrid.MouseWheel -=
                    OnGridMouseWheel;

                activeGrid.Disposed -=
                    OnGridDisposed;
            }

            if (geometrySyncTimer is not null)
            {
                geometrySyncTimer.Tick -=
                    OnGeometrySyncTimerTick;

                geometrySyncTimer.Dispose();
                geometrySyncTimer = null;
            }

            if (footer is not null &&
                !footer.IsDisposed)
            {
                footer.Dispose();
            }

            footer = null;
            activeGrid = null;

            activeUiColumn = -1;
            geometryRow = -1;

            suppressSelectionChanged = false;
        }
    }
}
