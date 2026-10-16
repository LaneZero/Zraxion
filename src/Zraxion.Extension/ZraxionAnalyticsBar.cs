using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Zraxion.Extension
{
    internal sealed class ZraxionAnalyticsBar : UserControl
    {
        private readonly Label resultLabel;
        private readonly Label sumLabel;
        private readonly Label countLabel;
        private readonly Label averageLabel;
        private readonly Label compareLabel;
        private readonly Label minLabel;
        private readonly Label maxLabel;
        private readonly Label nullLabel;
        private readonly Label nonNumericLabel;

        private readonly Button metricsButton;
        private readonly ContextMenuStrip metricsMenu;
        private readonly ToolStripMenuItem showBaselineMenuItem;
        private readonly ToolStripMenuItem showComparisonMenuItem;

        private readonly CheckBox autoRefreshCheckBox;
        private readonly Button refreshButton;
        private readonly Button copyButton;
        private readonly ToolTip actionToolTip;
        private readonly ContextMenuStrip comparisonCopyMenu;
        private readonly ToolStripMenuItem copyAItem;
        private readonly ToolStripMenuItem copyBItem;
        private readonly ToolStripMenuItem copyDifferenceItem;
        private readonly ToolStripMenuItem copyDifferencePercentItem;
        private readonly ToolStripMenuItem copySummaryItem;
        private readonly GridAnalyticsDisplayOptions displayOptions;

        private GridSelectionAnalytics.NumericSelectionSummary? lastSelectionSummary;
        private int lastSelectionResultIndex;

        private GridAnalyticsSnapshot? lastBaseline;
        private GridComparisonSession.BaselineMetadata? lastBaselineMetadata;
        private GridAnalyticsComparison? lastComparison;

        private bool baselineMode;
        private bool comparisonMode;
        private bool clearedMode;
        private string? focusedMetric;

        private ZraxionThemePalette themePalette;


        private readonly Control sumSeparator;
        private readonly Control countSeparator;
        private readonly Control averageSeparator;
        private readonly Control compareSeparator;
        private readonly Control minSeparator;
        private readonly Control maxSeparator;
        private readonly Control nullSeparator;
        private readonly Control nonNumericSeparator;


        public ZraxionAnalyticsBar()
        {
            displayOptions = GridAnalyticsSettingsStore.Load();

            actionToolTip = new ToolTip
            {
                ShowAlways = true,
                InitialDelay = 350,
                ReshowDelay = 100,
                AutoPopDelay = 8000
            };

            Name = "ZraxionAnalyticsBar";

            Height = 34;
            Dock = DockStyle.None;

            Anchor =
                AnchorStyles.Left |
                AnchorStyles.Right |
                AnchorStyles.Bottom;

            themePalette =
                ZraxionTheme.Resolve(
                    null);

            BackColor =
                themePalette.Background;

            ForeColor =
                themePalette.Foreground;

            BorderStyle = BorderStyle.FixedSingle;

            Padding = new Padding(8, 0, 6, 0);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = BackColor
            };

            rootLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));

            rootLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.AutoSize));

            var metricsLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = BackColor
            };

            resultLabel = CreatePrimaryLabel();
            sumLabel = CreateMetricLabel();
            countLabel = CreateMetricLabel();
            averageLabel = CreateMetricLabel();
            compareLabel = CreateMetricLabel();
            minLabel = CreateMetricLabel();
            maxLabel = CreateMetricLabel();
            nullLabel = CreateMetricLabel();
            nonNumericLabel = CreateMetricLabel();
            sumSeparator = CreateSeparator();
            countSeparator = CreateSeparator();
            averageSeparator = CreateSeparator();
            compareSeparator = CreateSeparator();
            minSeparator = CreateSeparator();
            maxSeparator = CreateSeparator();
            nullSeparator = CreateSeparator();
            nonNumericSeparator = CreateSeparator();

            comparisonCopyMenu =
                new ContextMenuStrip
                {
                    ShowItemToolTips = true
                };

            copyAItem =
                new ToolStripMenuItem("Copy A")
                {
                    ToolTipText =
                        "Copy the exact invariant SUM value stored in baseline A."
                };

            copyBItem =
                new ToolStripMenuItem("Copy B")
                {
                    ToolTipText =
                        "Copy the exact invariant SUM value from comparison selection B."
                };

            copyDifferenceItem =
                new ToolStripMenuItem("Copy Difference")
                {
                    ToolTipText =
                        "Copy the exact invariant difference using B - A."
                };

            copyDifferencePercentItem =
                new ToolStripMenuItem("Copy Difference %")
                {
                    ToolTipText =
                        "Copy the invariant percentage difference without display formatting."
                };

            copySummaryItem =
                new ToolStripMenuItem("Copy Summary")
                {
                    ToolTipText =
                        "Copy an exact invariant Baseline or Compare summary."
                };

            copyAItem.Click +=
                (_, __) => CopyComparisonValueAfterMenuCloses("A");

            copyBItem.Click +=
                (_, __) => CopyComparisonValueAfterMenuCloses("B");

            copyDifferenceItem.Click +=
                (_, __) => CopyComparisonValueAfterMenuCloses("DIFF");

            copyDifferencePercentItem.Click +=
                (_, __) => CopyComparisonValueAfterMenuCloses("PERCENT");

            copySummaryItem.Click +=
                (_, __) => CopySummaryAfterMenuCloses();

            comparisonCopyMenu.Items.Add(copyAItem);
            comparisonCopyMenu.Items.Add(copyBItem);
            comparisonCopyMenu.Items.Add(
                new ToolStripSeparator());
            comparisonCopyMenu.Items.Add(copyDifferenceItem);
            comparisonCopyMenu.Items.Add(copyDifferencePercentItem);
            comparisonCopyMenu.Items.Add(
                new ToolStripSeparator());
            comparisonCopyMenu.Items.Add(copySummaryItem);

            comparisonCopyMenu.Opening +=
                (_, __) => UpdateComparisonCopyMenuState();

            compareLabel.ContextMenuStrip =
                comparisonCopyMenu;

            minLabel.Visible = displayOptions.ShowMin;
            maxLabel.Visible = displayOptions.ShowMax;
            nullLabel.Visible = displayOptions.ShowNullOrEmpty;
            nonNumericLabel.Visible = displayOptions.ShowNonNumeric;
            compareLabel.Visible = false;

            sumSeparator.Visible = displayOptions.ShowSum;
            countSeparator.Visible = displayOptions.ShowCount;
            averageSeparator.Visible = displayOptions.ShowAverage;
            compareSeparator.Visible = false;
            minSeparator.Visible = displayOptions.ShowMin;
            maxSeparator.Visible = displayOptions.ShowMax;
            nullSeparator.Visible = displayOptions.ShowNullOrEmpty;
            nonNumericSeparator.Visible = displayOptions.ShowNonNumeric;


            metricsLayout.Controls.Add(resultLabel);

            metricsLayout.Controls.Add(sumSeparator);
            metricsLayout.Controls.Add(sumLabel);

            metricsLayout.Controls.Add(countSeparator);
            metricsLayout.Controls.Add(countLabel);

            metricsLayout.Controls.Add(averageSeparator);
            metricsLayout.Controls.Add(averageLabel);

            metricsLayout.Controls.Add(compareSeparator);
            metricsLayout.Controls.Add(compareLabel);

            metricsLayout.Controls.Add(minSeparator);
            metricsLayout.Controls.Add(minLabel);

            metricsLayout.Controls.Add(maxSeparator);
            metricsLayout.Controls.Add(maxLabel);

            metricsLayout.Controls.Add(nullSeparator);
            metricsLayout.Controls.Add(nullLabel);

            metricsLayout.Controls.Add(nonNumericSeparator);
            metricsLayout.Controls.Add(nonNumericLabel);

            var actionsLayout = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = BackColor
            };

            autoRefreshCheckBox = new CheckBox
            {
                AutoSize = true,
                Text = "Auto",
                Checked = displayOptions.AutoRefresh,
                Margin = new Padding(6, 6, 8, 0)
            };

            actionToolTip.SetToolTip(
                autoRefreshCheckBox,
                "Automatically refresh selection metrics when the active Results Grid selection changes.");

            autoRefreshCheckBox.CheckedChanged +=
                OnAutoRefreshCheckedChanged;

            refreshButton = new Button
            {
                AutoSize = true,
                Text = "Refresh",
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 4, 0, 0),
                Padding = new Padding(5, 0, 5, 0),
                Height = 25
            };



            refreshButton.FlatAppearance.BorderSize = 0;


            actionToolTip.SetToolTip(
                refreshButton,
                "Recalculate the analytics bar for the current Results Grid selection.");

            refreshButton.Click +=
                OnRefreshButtonClick;

            copyButton = new Button
            {
                AutoSize = true,
                Text = "Copy",
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 4, 0, 0),
                Padding = new Padding(5, 0, 5, 0),
                Height = 25
            };

            copyButton.FlatAppearance.BorderSize = 0;

            actionToolTip.SetToolTip(
                copyButton,
                "Copy the current Analytics Bar view using exact invariant numeric values.");

            copyButton.Click += (_, __) =>
            {
                var summary =
                    BuildCurrentViewCopySummary();

                if (!string.IsNullOrWhiteSpace(
                        summary))
                {
                    CopyTextAfterMenuCloses(
                        summary);
                }
            };

            var newMetricsMenu = new ContextMenuStrip
            {
                // Built-in ToolStrip tooltips can overlap the menu item that
                // the user is trying to click. We render them outside the
                // drop-down instead.
                ShowItemToolTips = false
            };

            var metricsMenuToolTip = new ToolTip
            {
                InitialDelay = 350,
                ReshowDelay = 100,
                AutoPopDelay = 6000,
                ShowAlways = true
            };

            var metricsToolTipHideTimer = new System.Windows.Forms.Timer
            {
                Interval = 140
            };

            metricsToolTipHideTimer.Tick += (_, __) =>
            {
                metricsToolTipHideTimer.Stop();
                metricsMenuToolTip.Hide(this);
            };

            void ShowMenuItemToolTip(
                ToolStripItem item)
            {
                metricsToolTipHideTimer.Stop();

                if (string.IsNullOrWhiteSpace(
                        item.ToolTipText) ||
                    IsDisposed ||
                    !IsHandleCreated)
                {
                    return;
                }

                metricsMenuToolTip.Hide(
                    this);

                // The View menu normally opens near the right edge of SSMS.
                // Put the tooltip well to the LEFT of the menu so it never
                // covers the item being selected.
                var menuTopLeft =
                    newMetricsMenu.PointToScreen(
                        Point.Empty);

                var workingArea =
                    Screen.FromControl(
                        newMetricsMenu)
                    .WorkingArea;

                var tooltipX =
                    Math.Max(
                        workingArea.Left + 8,
                        menuTopLeft.X - 360);

                var tooltipY =
                    menuTopLeft.Y +
                    Math.Max(
                        0,
                        item.Bounds.Top);

                var localPoint =
                    PointToClient(
                        new Point(
                            tooltipX,
                            tooltipY));

                metricsMenuToolTip.Show(
                    item.ToolTipText,
                    this,
                    localPoint,
                    6000);
            }

            void HideMenuItemToolTip()
            {
                metricsToolTipHideTimer.Stop();
                metricsMenuToolTip.Hide(
                    this);
            }

            void ScheduleMenuItemToolTipHide()
            {
                metricsToolTipHideTimer.Stop();
                metricsToolTipHideTimer.Start();
            }

            newMetricsMenu.Closed += (_, __) =>
            {
                HideMenuItemToolTip();
            };

            var showSelectionMetricsItem =
                new ToolStripMenuItem("Selection metrics")
                {
                    ToolTipText =
                        "Show analytics for the cells currently selected in the Results Grid."
                };

            showSelectionMetricsItem.MouseEnter +=
                (_, __) => ShowMenuItemToolTip(
                    showSelectionMetricsItem);

            showSelectionMetricsItem.MouseLeave +=
                (_, __) => ScheduleMenuItemToolTipHide();

            showSelectionMetricsItem.Click += (_, __) =>
            {
                clearedMode = false;
                baselineMode = false;
                comparisonMode = false;
                focusedMetric = null;
                ApplyCurrentView();
            };

            newMetricsMenu.Items.Add(showSelectionMetricsItem);

            showBaselineMenuItem =
                new ToolStripMenuItem("Baseline A")
                {
                    Enabled = false,
                    ToolTipText =
                        "Show the saved comparison baseline A without changing or recalculating it."
                };

            showBaselineMenuItem.MouseEnter +=
                (_, __) => ShowMenuItemToolTip(
                    showBaselineMenuItem);

            showBaselineMenuItem.MouseLeave +=
                (_, __) => ScheduleMenuItemToolTipHide();

            showBaselineMenuItem.Click += (_, __) =>
            {
                if (lastBaseline is null)
                {
                    return;
                }

                clearedMode = false;
                baselineMode = true;
                comparisonMode = false;
                focusedMetric = null;
                ApplyCurrentView();
            };

            newMetricsMenu.Items.Add(showBaselineMenuItem);

            showComparisonMenuItem =
                new ToolStripMenuItem("Last comparison")
                {
                    Enabled = false,
                    ToolTipText =
                        "Show the most recent A-versus-B comparison result."
                };

            showComparisonMenuItem.MouseEnter +=
                (_, __) => ShowMenuItemToolTip(
                    showComparisonMenuItem);

            showComparisonMenuItem.MouseLeave +=
                (_, __) => ScheduleMenuItemToolTipHide();

            showComparisonMenuItem.Click += (_, __) =>
            {
                if (lastComparison is null)
                {
                    return;
                }

                clearedMode = false;
                baselineMode = false;
                comparisonMode = true;
                focusedMetric = null;
                ApplyCurrentView();
            };

            newMetricsMenu.Items.Add(showComparisonMenuItem);
            newMetricsMenu.Items.Add(new ToolStripSeparator());

            newMetricsMenu.Items.Add(
                new ToolStripMenuItem("Visible selection metrics")
                {
                    Enabled = false
                });

            var sumItem =
                CreateMetricMenuItem(
                    "SUM",
                    displayOptions.ShowSum,
                    (_, __) => { });

            sumLabel.Visible =
                displayOptions.ShowSum;
            sumSeparator.Visible =
                displayOptions.ShowSum;

            sumItem.CheckedChanged += (_, __) =>
            {
                displayOptions.ShowSum = sumItem.Checked;
                ApplyCurrentView();

                GridAnalyticsSettingsStore.Save(
                    displayOptions);
            };

            newMetricsMenu.Items.Add(sumItem);

            var countItem =
    CreateMetricMenuItem(
        "COUNT",
        displayOptions.ShowCount,
        (_, __) => { });

            countLabel.Visible =
                displayOptions.ShowCount;
            countSeparator.Visible =
                displayOptions.ShowCount;

            countItem.CheckedChanged += (_, __) =>
            {
                displayOptions.ShowCount = countItem.Checked;
                ApplyCurrentView();

                GridAnalyticsSettingsStore.Save(
                    displayOptions);
            };

            newMetricsMenu.Items.Add(countItem);



            var averageItem = CreateMetricMenuItem("AVG", displayOptions.ShowAverage, (_, __) => { });

            averageLabel.Visible =
                displayOptions.ShowAverage;
            averageSeparator.Visible =
                displayOptions.ShowAverage;

            averageItem.CheckedChanged += (_, __) =>
            {
                displayOptions.ShowAverage = averageItem.Checked;
                ApplyCurrentView();

                GridAnalyticsSettingsStore.Save(
                    displayOptions);
            };

            newMetricsMenu.Items.Add(averageItem);

            newMetricsMenu.Items.Add(
                new ToolStripSeparator());

            var minItem =
         CreateMetricMenuItem(
        "MIN",
        displayOptions.ShowMin,
        (_, __) => { });

            minLabel.Visible =
                displayOptions.ShowMin;
            minSeparator.Visible =
                displayOptions.ShowMin;

            minItem.CheckedChanged += (_, __) =>
            {
                displayOptions.ShowMin = minItem.Checked;
                ApplyCurrentView();

                GridAnalyticsSettingsStore.Save(
                    displayOptions);
            };

            newMetricsMenu.Items.Add(minItem);

            var maxItem =
    CreateMetricMenuItem(
        "MAX",
        displayOptions.ShowMax,
        (_, __) => { });

            maxLabel.Visible =
                displayOptions.ShowMax;
            maxSeparator.Visible =
                displayOptions.ShowMax;

            maxItem.CheckedChanged += (_, __) =>
            {
                displayOptions.ShowMax = maxItem.Checked;
                ApplyCurrentView();

                GridAnalyticsSettingsStore.Save(
                    displayOptions);
            };

            newMetricsMenu.Items.Add(maxItem);

            var nullItem =
    CreateMetricMenuItem(
        "NULL / Empty",
        displayOptions.ShowNullOrEmpty,
        (_, __) => { });

            nullLabel.Visible =
                displayOptions.ShowNullOrEmpty;
            nullSeparator.Visible =
                displayOptions.ShowNullOrEmpty;

            nullItem.CheckedChanged += (_, __) =>
            {
                displayOptions.ShowNullOrEmpty =
                    nullItem.Checked;

                ApplyCurrentView();

                GridAnalyticsSettingsStore.Save(
                    displayOptions);
            };

            newMetricsMenu.Items.Add(nullItem);
            var nonNumericItem =
    CreateMetricMenuItem(
        "Non-numeric",
        displayOptions.ShowNonNumeric,
        (_, __) => { });

            nonNumericLabel.Visible =
                displayOptions.ShowNonNumeric;
            nonNumericSeparator.Visible =
                displayOptions.ShowNonNumeric;

            nonNumericItem.CheckedChanged += (_, __) =>
            {
                displayOptions.ShowNonNumeric =
                    nonNumericItem.Checked;

                ApplyCurrentView();

                GridAnalyticsSettingsStore.Save(
                    displayOptions);
            };

            newMetricsMenu.Items.Add(nonNumericItem);

            metricsMenu = newMetricsMenu;

            var newMetricsButton = new Button
            {
                AutoSize = true,
                Text = "View",
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 4, 0, 0),
                Padding = new Padding(5, 0, 5, 0),
                Height = 25
            };

            newMetricsButton.FlatAppearance.BorderSize = 0;

            actionToolTip.SetToolTip(
                newMetricsButton,
                "Switch between Selection metrics, Baseline A, and Last comparison, or choose which selection metrics are visible.");

            newMetricsButton.Click += (_, __) =>
            {
                newMetricsMenu.Show(
                    newMetricsButton,
                    new Point(
                        0,
                        newMetricsButton.Height));
            };

            metricsButton = newMetricsButton;





            actionsLayout.Controls.Add(autoRefreshCheckBox);
            actionsLayout.Controls.Add(refreshButton);
            actionsLayout.Controls.Add(copyButton);
            actionsLayout.Controls.Add(metricsButton);

            rootLayout.Controls.Add(metricsLayout, 0, 0);
            rootLayout.Controls.Add(actionsLayout, 1, 0);

            Controls.Add(rootLayout);

            ApplyThemeFromParent();
            Reset();
        }

        public event EventHandler? AutoRefreshChanged;

        public event EventHandler? RefreshRequested;

        public bool AutoRefreshEnabled =>
            autoRefreshCheckBox.Checked;

        protected override void OnParentChanged(
            EventArgs e)
        {
            base.OnParentChanged(e);
            ApplyThemeFromParent();
        }

        protected override void OnSystemColorsChanged(
            EventArgs e)
        {
            base.OnSystemColorsChanged(e);
            ApplyThemeFromParent();
        }

        private void ApplyThemeFromParent()
        {
            themePalette =
                ZraxionTheme.Resolve(
                    Parent);

            ZraxionTheme.ApplyToControlTree(
                this,
                themePalette);

            foreach (var separator in GetSeparators())
            {
                separator.BackColor =
                    themePalette.Background;

                separator.ForeColor =
                    themePalette.MutedForeground;
            }

            ZraxionTheme.ApplyToMenu(
                metricsMenu,
                themePalette);

            ZraxionTheme.ApplyToMenu(
                comparisonCopyMenu,
                themePalette);

            Invalidate();
        }

        private Control[] GetSeparators()
        {
            return new[]
            {
                sumSeparator,
                countSeparator,
                averageSeparator,
                compareSeparator,
                minSeparator,
                maxSeparator,
                nullSeparator,
                nonNumericSeparator
            };
        }


        public void UpdateAnalytics(
            int resultIndex,
            GridSelectionAnalytics.NumericSelectionSummary summary)
        {
            if (summary is null)
            {
                throw new ArgumentNullException(nameof(summary));
            }

            lastSelectionResultIndex =
                resultIndex;

            lastSelectionSummary =
                summary;

            resultLabel.Text = $"Result {resultIndex}";

            if (summary.SelectedCount == 0 ||
                summary.NumericCount == 0)
            {
                sumLabel.Text = "SUM —";
                countLabel.Text = "COUNT 0";
                averageLabel.Text = "AVG —";
                minLabel.Text = "MIN —";
                maxLabel.Text = "MAX —";
                nullLabel.Text =
                    $"NULL  {summary.NullOrEmptyCount:N0}";
                nonNumericLabel.Text =
                    $"NON-NUMERIC  {summary.NonNumericCount:N0}";

                ApplyCurrentView();
                return;
            }

            sumLabel.Text =
                $"SUM  {FormatNumber(summary.Sum)}";

            countLabel.Text =
                $"COUNT  {summary.NumericCount:N0}";

            averageLabel.Text =
                summary.Average.HasValue
                    ? $"AVG  {FormatNumber(summary.Average.Value)}"
                    : "AVG —";

            minLabel.Text =
                summary.Min.HasValue
                    ? $"MIN  {FormatNumber(summary.Min.Value)}"
                    : "MIN —";

            maxLabel.Text =
                summary.Max.HasValue
                    ? $"MAX  {FormatNumber(summary.Max.Value)}"
                    : "MAX —";

            nullLabel.Text =
                $"NULL  {summary.NullOrEmptyCount:N0}";

            nonNumericLabel.Text =
                $"NON-NUMERIC  {summary.NonNumericCount:N0}";

            ApplyCurrentView();
        }

        public void ShowBaseline(
            GridAnalyticsSnapshot baseline,
            GridComparisonSession.BaselineMetadata? metadata)
        {
            if (baseline is null)
            {
                throw new ArgumentNullException(
                    nameof(baseline));
            }

            lastBaseline = baseline;
            lastBaselineMetadata = metadata;

            clearedMode = false;
            baselineMode = true;
            comparisonMode = false;
            focusedMetric = null;

            showBaselineMenuItem.Enabled = true;

            var contextParts =
                new System.Collections.Generic.List<string>();

            if (metadata is not null)
            {
                if (metadata.ResultGridIndex >= 0)
                {
                    contextParts.Add(
                        $"Result {metadata.ResultGridIndex}");
                }

                if (metadata.UiColumnIndex.HasValue)
                {
                    contextParts.Add(
                        $"Column {metadata.UiColumnIndex.Value + 1}");
                }

                contextParts.Add(
                    metadata.CapturedAtUtc
                        .ToLocalTime()
                        .ToString(
                            "HH:mm:ss",
                            CultureInfo.CurrentCulture));
            }

            var context =
                contextParts.Count > 0
                    ? "   |   " +
                      string.Join(
                          "   |   ",
                          contextParts)
                    : string.Empty;

            compareLabel.Text =
                $"BASELINE A   SUM {FormatNumber(baseline.Sum)}" +
                $"   COUNT {baseline.NumericCount:N0}" +
                context;

            ApplyCurrentView();
        }

        public void ShowComparison(
            GridAnalyticsComparison comparison)
        {
            if (comparison is null)
            {
                throw new ArgumentNullException(nameof(comparison));
            }

            lastComparison = comparison;

            if (lastBaseline is null)
            {
                lastBaseline = comparison.Left;
            }

            clearedMode = false;
            baselineMode = false;
            comparisonMode = true;
            focusedMetric = null;

            showBaselineMenuItem.Enabled =
                lastBaseline is not null;

            showComparisonMenuItem.Enabled = true;

            var percent =
                comparison.DifferencePercent.HasValue
                    ? FormatSignedPercent(
                        comparison.DifferencePercent.Value)
                    : "N/A";

            var status =
                comparison.IsMatch
                    ? "✓ MATCH"
                    : "⚠ MISMATCH";

            var tolerance =
                FormatTolerance(
                    comparison.TolerancePolicy);

            var reason =
                FormatMatchReason(
                    comparison.MatchReason);

            compareLabel.Text =
                $"COMPARE   A {FormatNumber(comparison.Left.Sum)}" +
                $"  →  B {FormatNumber(comparison.Right.Sum)}" +
                $"   Δ {FormatSignedNumber(comparison.Difference)}" +
                $" ({percent})" +
                $"   TOL {tolerance}" +
                $"   {status} · {reason}";

            ApplyCurrentView();
        }

        public void ShowSelectionMetricsView()
        {
            clearedMode = false;
            baselineMode = false;
            comparisonMode = false;
            focusedMetric = null;
            ApplyCurrentView();
        }

        public void ShowSelectionMetricView(
            string metric)
        {
            if (string.IsNullOrWhiteSpace(metric))
            {
                throw new ArgumentException(
                    "A metric name is required.",
                    nameof(metric));
            }

            clearedMode = false;
            baselineMode = false;
            comparisonMode = false;
            focusedMetric = metric.Trim().ToUpperInvariant();
            ApplyCurrentView();
        }

        public void ClearComparison()
        {
            lastBaseline = null;
            lastBaselineMetadata = null;
            lastComparison = null;

            // Stay in an explicitly cleared state until the user chooses
            // another view or starts a new baseline/compare action.
            clearedMode = true;
            baselineMode = false;
            comparisonMode = false;
            focusedMetric = null;

            showBaselineMenuItem.Enabled = false;
            showComparisonMenuItem.Enabled = false;

            compareLabel.Text = string.Empty;

            ApplyCurrentView();
        }

        public void Reset()
        {
            resultLabel.Text = "Result —";
            sumLabel.Text = "SUM —";
            countLabel.Text = "COUNT 0";
            averageLabel.Text = "AVG —";
            minLabel.Text = "MIN —";
            maxLabel.Text = "MAX —";
            nullLabel.Text = "NULL  0";
            nonNumericLabel.Text = "NON-NUMERIC  0";

            compareLabel.Text = string.Empty;

            lastSelectionSummary = null;
            lastSelectionResultIndex = 0;

            lastBaseline = null;
            lastBaselineMetadata = null;
            lastComparison = null;

            baselineMode = false;
            comparisonMode = false;
            focusedMetric = null;

            showBaselineMenuItem.Enabled = false;
            showComparisonMenuItem.Enabled = false;

            ApplyCurrentView();
        }

        private void ApplyCurrentView()
        {
            if (clearedMode)
            {
                resultLabel.Visible = false;

                sumLabel.Visible = false;
                countLabel.Visible = false;
                averageLabel.Visible = false;
                minLabel.Visible = false;
                maxLabel.Visible = false;
                nullLabel.Visible = false;
                nonNumericLabel.Visible = false;

                sumSeparator.Visible = false;
                countSeparator.Visible = false;
                averageSeparator.Visible = false;
                minSeparator.Visible = false;
                maxSeparator.Visible = false;
                nullSeparator.Visible = false;
                nonNumericSeparator.Visible = false;

                compareSeparator.Visible = false;
                compareLabel.Visible = false;

                return;
            }

            if (baselineMode &&
                lastBaseline is not null)
            {
                resultLabel.Visible = false;

                sumLabel.Visible = false;
                countLabel.Visible = false;
                averageLabel.Visible = false;
                minLabel.Visible = false;
                maxLabel.Visible = false;
                nullLabel.Visible = false;
                nonNumericLabel.Visible = false;

                sumSeparator.Visible = false;
                countSeparator.Visible = false;
                averageSeparator.Visible = false;
                minSeparator.Visible = false;
                maxSeparator.Visible = false;
                nullSeparator.Visible = false;
                nonNumericSeparator.Visible = false;

                compareSeparator.Visible = false;
                compareLabel.Visible = true;

                return;
            }

            if (comparisonMode &&
                lastComparison is not null)
            {
                // Compare mode is intentionally minimal:
                // only the comparison result stays on the bar.
                resultLabel.Visible = false;

                sumLabel.Visible = false;
                countLabel.Visible = false;
                averageLabel.Visible = false;
                minLabel.Visible = false;
                maxLabel.Visible = false;
                nullLabel.Visible = false;
                nonNumericLabel.Visible = false;

                sumSeparator.Visible = false;
                countSeparator.Visible = false;
                averageSeparator.Visible = false;
                minSeparator.Visible = false;
                maxSeparator.Visible = false;
                nullSeparator.Visible = false;
                nonNumericSeparator.Visible = false;

                compareSeparator.Visible = false;
                compareLabel.Visible = true;

                return;
            }

            // A context-menu metric action intentionally focuses the bar on that
            // metric without changing the user's persisted metric preferences.
            if (!string.IsNullOrEmpty(focusedMetric))
            {
                resultLabel.Visible = true;

                sumLabel.Visible = focusedMetric == "SUM";
                countLabel.Visible = focusedMetric == "COUNT";
                averageLabel.Visible = focusedMetric == "AVG";
                minLabel.Visible = focusedMetric == "MIN";
                maxLabel.Visible = focusedMetric == "MAX";
                nullLabel.Visible = false;
                nonNumericLabel.Visible = false;

                sumSeparator.Visible = sumLabel.Visible;
                countSeparator.Visible = countLabel.Visible;
                averageSeparator.Visible = averageLabel.Visible;
                minSeparator.Visible = minLabel.Visible;
                maxSeparator.Visible = maxLabel.Visible;
                nullSeparator.Visible = false;
                nonNumericSeparator.Visible = false;

                compareLabel.Visible = false;
                compareSeparator.Visible = false;

                return;
            }

            // Selection metrics view. Visibility follows the user's saved choices.
            resultLabel.Visible = true;

            sumLabel.Visible = displayOptions.ShowSum;
            countLabel.Visible = displayOptions.ShowCount;
            averageLabel.Visible = displayOptions.ShowAverage;
            minLabel.Visible = displayOptions.ShowMin;
            maxLabel.Visible = displayOptions.ShowMax;
            nullLabel.Visible = displayOptions.ShowNullOrEmpty;
            nonNumericLabel.Visible = displayOptions.ShowNonNumeric;

            sumSeparator.Visible = displayOptions.ShowSum;
            countSeparator.Visible = displayOptions.ShowCount;
            averageSeparator.Visible = displayOptions.ShowAverage;
            minSeparator.Visible = displayOptions.ShowMin;
            maxSeparator.Visible = displayOptions.ShowMax;
            nullSeparator.Visible = displayOptions.ShowNullOrEmpty;
            nonNumericSeparator.Visible = displayOptions.ShowNonNumeric;

            compareLabel.Visible = false;
            compareSeparator.Visible = false;
        }

        private void OnAutoRefreshCheckedChanged(
            object? sender,
            EventArgs e)
        {
            displayOptions.AutoRefresh = autoRefreshCheckBox.Checked;

            GridAnalyticsSettingsStore.Save(
                displayOptions);

            AutoRefreshChanged?.Invoke(
                this,
                EventArgs.Empty);
        }

        private void OnRefreshButtonClick(
            object? sender,
            EventArgs e)
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        private static Label CreatePrimaryLabel()
        {
            return new Label
            {
                AutoSize = true,
                Font = new Font(
                    SystemFonts.MessageBoxFont,
                    FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(2, 8, 6, 0)
            };
        }

        private static Label CreateMetricLabel()
        {
            return new Label
            {
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(6, 8, 6, 0)
            };
        }

        private static Label CreateSeparator()
        {
            return new Label
            {
                AutoSize = true,
                Text = "·",
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(1, 8, 1, 0)
            };
        }

        private static string FormatSignedNumber(
            ExactDecimal value)
        {
            if (value.Sign > 0)
            {
                return
                    "+" +
                    FormatNumber(
                        value);
            }

            return
                FormatNumber(
                    value);
        }

        private static string FormatSignedPercent(
            ExactDecimal value)
        {
            var formatted =
                ExactDecimalFormatter.Format(
                    value,
                    CultureInfo.CurrentCulture) +
                "%";

            return
                value.Sign > 0
                    ? "+" + formatted
                    : formatted;
        }

        private static string FormatNumber(
            ExactDecimal value)
        {
            return
                ExactDecimalFormatter.Format(
                    value,
                    CultureInfo.CurrentCulture);
        }

        private static string FormatNumber(
            decimal value)
        {
            return value.ToString(
                "#,##0.############################",
                CultureInfo.CurrentCulture);
        }

        private static ToolStripMenuItem CreateMetricMenuItem(
            string name,
            bool isChecked,
            EventHandler onClick)
        {
            var item = new ToolStripMenuItem(name)
            {
                Name = name,
                Checked = isChecked,
                CheckOnClick = true,
                ToolTipText = GetMetricToolTip(name)
            };

            item.Click += onClick;

            return item;
        }

        private static string GetMetricToolTip(
            string name)
        {
            switch (name?.Trim().ToUpperInvariant())
            {
                case "SUM":
                    return "Show or hide the sum of numeric cells in Selection metrics view.";

                case "COUNT":
                    return "Show or hide the count of numeric cells. NULL, empty, and non-numeric cells are excluded.";

                case "AVG":
                    return "Show or hide the average of numeric cells in Selection metrics view.";

                case "MIN":
                    return "Show or hide the minimum numeric value in Selection metrics view.";

                case "MAX":
                    return "Show or hide the maximum numeric value in Selection metrics view.";

                case "NULL / EMPTY":
                case "NULL":
                    return "Show or hide the number of NULL or empty cells in the current selection.";

                case "NON-NUMERIC":
                    return "Show or hide the number of selected cells that could not be parsed as numbers.";

                default:
                    return "Show or hide this value in Selection metrics view.";
            }
        }


        private static string FormatTolerance(
            GridComparisonTolerancePolicy tolerancePolicy)
        {
            if (tolerancePolicy.IsExact)
            {
                return "EXACT";
            }

            var absolute =
                tolerancePolicy.AbsoluteTolerance;

            var percentage =
                tolerancePolicy.PercentageTolerance;

            if (absolute.HasValue &&
                percentage.HasValue)
            {
                return
                    $"±{FormatNumber(absolute.Value)} OR " +
                    $"±{FormatNumber(percentage.Value)}%";
            }

            if (absolute.HasValue)
            {
                return
                    $"±{FormatNumber(absolute.Value)}";
            }

            return
                $"±{FormatNumber(percentage!.Value)}%";
        }

        private static string FormatMatchReason(
            GridComparisonMatchReason matchReason)
        {
            switch (matchReason)
            {
                case GridComparisonMatchReason.ExactMatch:
                    return "EXACT";

                case GridComparisonMatchReason.WithinAbsoluteTolerance:
                    return "ABS TOL";

                case GridComparisonMatchReason.WithinPercentageTolerance:
                    return "% TOL";

                case GridComparisonMatchReason.WithinCombinedTolerance:
                    return "ABS + % TOL";

                default:
                    return "DIFFERENCE";
            }
        }

        private void UpdateComparisonCopyMenuState()
        {
            var hasBaseline =
                lastBaseline is not null;

            var hasComparison =
                lastComparison is not null &&
                comparisonMode;

            copyAItem.Enabled =
                hasBaseline || hasComparison;

            copyBItem.Enabled =
                hasComparison;

            copyDifferenceItem.Enabled =
                hasComparison;

            copyDifferencePercentItem.Enabled =
                hasComparison &&
                lastComparison!.DifferencePercent.HasValue;

            copySummaryItem.Enabled =
                !string.IsNullOrWhiteSpace(
                    compareLabel.Text);
        }

        private string BuildCurrentViewCopySummary()
        {
            if (comparisonMode ||
                baselineMode)
            {
                return
                    BuildExactCopySummary();
            }

            var summary =
                lastSelectionSummary;

            if (summary is null)
            {
                return string.Empty;
            }

            var average =
                summary.Average.HasValue
                    ? ExactDecimalFormatter
                        .FormatInvariant(
                            summary.Average.Value)
                    : "N/A";

            var minimum =
                summary.Min.HasValue
                    ? ExactDecimalFormatter
                        .FormatInvariant(
                            summary.Min.Value)
                    : "N/A";

            var maximum =
                summary.Max.HasValue
                    ? ExactDecimalFormatter
                        .FormatInvariant(
                            summary.Max.Value)
                    : "N/A";

            return
                "SELECTION" +
                " | Result=" +
                lastSelectionResultIndex
                    .ToString(
                        CultureInfo.InvariantCulture) +
                " | SUM=" +
                ExactDecimalFormatter
                    .FormatInvariant(
                        summary.Sum) +
                " | COUNT=" +
                summary.NumericCount
                    .ToString(
                        CultureInfo.InvariantCulture) +
                " | AVG=" +
                average +
                " | MIN=" +
                minimum +
                " | MAX=" +
                maximum +
                " | NULL=" +
                summary.NullOrEmptyCount
                    .ToString(
                        CultureInfo.InvariantCulture) +
                " | NON-NUMERIC=" +
                summary.NonNumericCount
                    .ToString(
                        CultureInfo.InvariantCulture);
        }

        private void CopySummaryAfterMenuCloses()
        {
            var summary =
                BuildExactCopySummary();

            if (string.IsNullOrWhiteSpace(
                    summary))
            {
                return;
            }

            CopyTextAfterMenuCloses(
                summary);
        }

        private string BuildExactCopySummary()
        {
            if (comparisonMode &&
                lastComparison is not null)
            {
                var percent =
                    lastComparison
                        .DifferencePercent
                        .HasValue
                        ? ExactDecimalFormatter
                            .FormatInvariant(
                                lastComparison
                                    .DifferencePercent
                                    .Value)
                        : "N/A";

                var status =
                    lastComparison.IsMatch
                        ? "MATCH"
                        : "MISMATCH";

                return
                    "COMPARE" +
                    " | A=" +
                    ExactDecimalFormatter
                        .FormatInvariant(
                            lastComparison.Left.Sum) +
                    " | B=" +
                    ExactDecimalFormatter
                        .FormatInvariant(
                            lastComparison.Right.Sum) +
                    " | Difference=" +
                    ExactDecimalFormatter
                        .FormatInvariant(
                            lastComparison.Difference) +
                    " | DifferencePercent=" +
                    percent +
                    " | Tolerance=" +
                    FormatToleranceInvariant(
                        lastComparison.TolerancePolicy) +
                    " | Status=" +
                    status +
                    " | Reason=" +
                    FormatMatchReason(
                        lastComparison.MatchReason);
            }

            if (baselineMode &&
                lastBaseline is not null)
            {
                return
                    "BASELINE A" +
                    " | SUM=" +
                    ExactDecimalFormatter
                        .FormatInvariant(
                            lastBaseline.Sum) +
                    " | COUNT=" +
                    lastBaseline.NumericCount
                        .ToString(
                            CultureInfo.InvariantCulture);
            }

            return
                compareLabel.Text;
        }

        private static string FormatToleranceInvariant(
            GridComparisonTolerancePolicy tolerancePolicy)
        {
            if (tolerancePolicy.IsExact)
            {
                return "EXACT";
            }

            var absolute =
                tolerancePolicy.AbsoluteTolerance;

            var percentage =
                tolerancePolicy.PercentageTolerance;

            if (absolute.HasValue &&
                percentage.HasValue)
            {
                return
                    "±" +
                    absolute.Value.ToString(
                        "G29",
                        CultureInfo.InvariantCulture) +
                    " OR ±" +
                    percentage.Value.ToString(
                        "G29",
                        CultureInfo.InvariantCulture) +
                    "%";
            }

            if (absolute.HasValue)
            {
                return
                    "±" +
                    absolute.Value.ToString(
                        "G29",
                        CultureInfo.InvariantCulture);
            }

            return
                "±" +
                percentage!.Value.ToString(
                    "G29",
                    CultureInfo.InvariantCulture) +
                "%";
        }

        private void CopyComparisonValueAfterMenuCloses(
            string valueKind)
        {
            var value =
                GetComparisonCopyValue(
                    valueKind);

            if (string.IsNullOrEmpty(
                    value))
            {
                return;
            }

            CopyTextAfterMenuCloses(
                value);
        }

        private void CopyTextAfterMenuCloses(
            string? text)
        {
            if (string.IsNullOrEmpty(
                    text))
            {
                return;
            }

            // ContextMenuStrip can still own the clipboard/UI transaction on
            // the click that closes it. Queue every comparison copy to the next
            // UI turn, not just Copy Summary.
            if (IsHandleCreated &&
                !IsDisposed)
            {
                try
                {
                    BeginInvoke(
                        new Action(
                            () =>
                            {
                                if (!IsDisposed)
                                {
                                    TryCopyToClipboard(
                                        text);
                                }
                            }));

                    return;
                }
                catch (InvalidOperationException)
                {
                    // Fall through to the direct retry path.
                }
            }

            TryCopyToClipboard(
                text);
        }


        private string? GetComparisonCopyValue(
            string valueKind)
        {
            var normalizedKind =
                valueKind?.Trim().ToUpperInvariant();

            string? value = null;

            if (normalizedKind == "A")
            {
                var baseline =
                    comparisonMode &&
                    lastComparison is not null
                        ? lastComparison.Left
                        : lastBaseline;

                if (baseline is not null)
                {
                    value =
                        ExactDecimalFormatter.FormatInvariant(
                            baseline.Sum);
                }
            }
            else if (
                comparisonMode &&
                lastComparison is not null)
            {
                switch (normalizedKind)
                {
                    case "B":
                        value =
                            ExactDecimalFormatter.FormatInvariant(
                                lastComparison.Right.Sum);
                        break;

                    case "DIFF":
                        value =
                            ExactDecimalFormatter.FormatInvariant(
                                lastComparison.Difference);
                        break;

                    case "PERCENT":
                        if (lastComparison.DifferencePercent.HasValue)
                        {
                            value =
                                ExactDecimalFormatter.FormatInvariant(
                                    lastComparison
                                        .DifferencePercent
                                        .Value);
                        }

                        break;
                }
            }

            return value;
        }

        private static bool TryCopyToClipboard(
            string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            for (var attempt = 0;
                 attempt < 5;
                 attempt++)
            {
                try
                {
                    Clipboard.SetText(
                        text);

                    return true;
                }
                catch (ExternalException)
                {
                    if (attempt < 4)
                    {
                        Thread.Sleep(40);
                    }
                }
            }

            return false;
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                comparisonCopyMenu.Dispose();
                actionToolTip.Dispose();
            }

            base.Dispose(disposing);
        }


    }
}