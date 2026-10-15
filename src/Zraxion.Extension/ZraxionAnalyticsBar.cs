using System;
using System.Drawing;
using System.Globalization;
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
        private readonly ToolStripMenuItem showComparisonMenuItem;

        private readonly CheckBox autoRefreshCheckBox;
        private readonly Button refreshButton;
        private readonly GridAnalyticsDisplayOptions displayOptions;

        private GridAnalyticsComparison? lastComparison;
        private bool comparisonMode;
        private string? focusedMetric;


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
            Name = "ZraxionAnalyticsBar";

            Height = 38;
            Dock = DockStyle.None;

            Anchor =
                AnchorStyles.Left |
                AnchorStyles.Right |
                AnchorStyles.Bottom;

            BackColor = Color.White;
            ForeColor = Color.FromArgb(35, 35, 35);

            BorderStyle = BorderStyle.FixedSingle;

            Padding = new Padding(10, 0, 8, 0);

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
                Margin = new Padding(6, 8, 8, 0)
            };

            autoRefreshCheckBox.CheckedChanged +=
                OnAutoRefreshCheckedChanged;

            refreshButton = new Button
            {
                AutoSize = true,
                Text = "Refresh",
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 5, 0, 0),
                Padding = new Padding(6, 0, 6, 0),
                Height = 26
            };



            refreshButton.FlatAppearance.BorderSize = 1;


            refreshButton.Click +=
                OnRefreshButtonClick;

            var newMetricsMenu = new ContextMenuStrip();

            var showSelectionMetricsItem =
                new ToolStripMenuItem("Show selection metrics");

            showSelectionMetricsItem.Click += (_, __) =>
            {
                comparisonMode = false;
                focusedMetric = null;
                ApplyCurrentView();
            };

            newMetricsMenu.Items.Add(showSelectionMetricsItem);

            showComparisonMenuItem =
                new ToolStripMenuItem("Show comparison")
                {
                    Enabled = false
                };

            showComparisonMenuItem.Click += (_, __) =>
            {
                if (lastComparison is null)
                {
                    return;
                }

                comparisonMode = true;
                ApplyCurrentView();
            };

            newMetricsMenu.Items.Add(showComparisonMenuItem);
            newMetricsMenu.Items.Add(new ToolStripSeparator());

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
                Text = "Metrics",
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(6, 5, 0, 0),
                Padding = new Padding(6, 0, 6, 0),
                Height = 26
            };

            newMetricsButton.FlatAppearance.BorderSize = 1;

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
            actionsLayout.Controls.Add(metricsButton);

            rootLayout.Controls.Add(metricsLayout, 0, 0);
            rootLayout.Controls.Add(actionsLayout, 1, 0);

            Controls.Add(rootLayout);

            Reset();
        }

        public event EventHandler? AutoRefreshChanged;

        public event EventHandler? RefreshRequested;

        public bool AutoRefreshEnabled =>
            autoRefreshCheckBox.Checked;

        public void UpdateAnalytics(
            int resultIndex,
            GridSelectionAnalytics.NumericSelectionSummary summary)
        {
            if (summary is null)
            {
                throw new ArgumentNullException(nameof(summary));
            }

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

        public void ShowComparison(
            GridAnalyticsComparison comparison)
        {
            if (comparison is null)
            {
                throw new ArgumentNullException(nameof(comparison));
            }

            lastComparison = comparison;
            comparisonMode = true;
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

            compareLabel.Text =
                $"COMPARE  A {FormatNumber(comparison.Left.Sum)}" +
                $" → B {FormatNumber(comparison.Right.Sum)}" +
                $"   |   Δ {FormatSignedNumber(comparison.Difference)}" +
                $" ({percent})" +
                $"   |   {status}";

            ApplyCurrentView();
        }

        public void ShowSelectionMetricsView()
        {
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

            comparisonMode = false;
            focusedMetric = metric.Trim().ToUpperInvariant();
            ApplyCurrentView();
        }

        public void ClearComparison()
        {
            lastComparison = null;
            comparisonMode = false;
            focusedMetric = null;
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
            lastComparison = null;
            comparisonMode = false;
            showComparisonMenuItem.Enabled = false;

            ApplyCurrentView();
        }

        private void ApplyCurrentView()
        {
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
                Margin = new Padding(2, 10, 8, 0)
            };
        }

        private static Label CreateMetricLabel()
        {
            return new Label
            {
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(8, 10, 8, 0)
            };
        }

        private static Label CreateSeparator()
        {
            return new Label
            {
                AutoSize = true,
                Text = "│",
                ForeColor = Color.FromArgb(180, 180, 180),
                Margin = new Padding(2, 10, 2, 0)
            };
        }

        private static string FormatSignedNumber(decimal value)
        {
            if (value > 0)
            {
                return "+" + FormatNumber(value);
            }

            return FormatNumber(value);
        }

        private static string FormatSignedPercent(decimal value)
        {
            var formatted = value.ToString(
                "0.####",
                CultureInfo.CurrentCulture) + "%";

            return value > 0
                ? "+" + formatted
                : formatted;
        }

        private static string FormatNumber(decimal value)
        {
            return value.ToString(
                "#,##0.##",
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
                CheckOnClick = true
            };

            item.Click += onClick;

            return item;
        }


    }
}