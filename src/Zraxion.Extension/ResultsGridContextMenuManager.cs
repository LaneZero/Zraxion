using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.CommandBars;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Zraxion.Extension
{
    internal static class ResultsGridContextMenuManager
    {
        private const string ResultsGridContextMenuName =
            "SQL Results Grid Tab Context";

        private const string ZraxionCaption =
            "Zraxion";

        private static readonly List<CommandBarButton> buttons =
            new List<CommandBarButton>();

        private static readonly List<CommandBarPopup> popups =
            new List<CommandBarPopup>();

        private static CommandBar? gridCommandBar;
        private static CommandBarPopup? zraxionPopup;
        private static bool initialized;
        private static string lastInitializationStatus = "Not initialized.";

        internal static void Initialize()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (initialized)
            {
                return;
            }

            try
            {
                var dte =
                    Package.GetGlobalService(typeof(DTE))
                        as DTE2;

                if (dte is null)
                {
                    lastInitializationStatus =
                        "DTE2 service was not available.";
                    return;
                }

                var commandBars =
                    dte.CommandBars as CommandBars;

                if (commandBars is null)
                {
                    lastInitializationStatus =
                        "SSMS CommandBars collection was not available.";
                    return;
                }

                gridCommandBar =
                    TryGetCommandBar(
                        commandBars,
                        ResultsGridContextMenuName);

                if (gridCommandBar is null)
                {
                    lastInitializationStatus =
                        $"CommandBar '{ResultsGridContextMenuName}' was not found.";
                    return;
                }

                RemoveExistingZraxionMenu(gridCommandBar);

                zraxionPopup =
                    gridCommandBar.Controls.Add(
                        MsoControlType.msoControlPopup,
                        Type.Missing,
                        Type.Missing,
                        Type.Missing,
                        true)
                    as CommandBarPopup;

                if (zraxionPopup is null)
                {
                    lastInitializationStatus =
                        "The Zraxion popup could not be created.";
                    return;
                }

                zraxionPopup.Caption = ZraxionCaption;
                zraxionPopup.Visible = true;
                zraxionPopup.Enabled = true;
                zraxionPopup.BeginGroup = true;

                popups.Add(zraxionPopup);

                AddQuickStatsMenu(zraxionPopup);

                AddSeparatorButton(
                    zraxionPopup,
                    "Set Selection as A",
                    OnSetBaselineClick);

                AddButton(
                    zraxionPopup,
                    "Compare with A",
                    OnCompareClick);

                AddSeparatorButton(
                    zraxionPopup,
                    "Show Analytics Bar",
                    OnShowAnalyticsBarClick);

                initialized = true;

                lastInitializationStatus =
                    $"Initialized successfully. Target='{gridCommandBar.Name}', " +
                    $"Controls={gridCommandBar.Controls.Count}.";
            }
            catch (Exception ex)
            {
                lastInitializationStatus =
                    $"Initialization failed: {ex.GetType().FullName}: {ex.Message}";
            }
        }

        internal static string GetDiagnosticReport()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var builder = new StringBuilder();

            builder.AppendLine("Zraxion CommandBars Diagnostic");
            builder.AppendLine(new string('=', 31));
            builder.AppendLine();
            builder.AppendLine($"Initialized: {initialized}");
            builder.AppendLine($"Status: {lastInitializationStatus}");
            builder.AppendLine();

            var dte =
                Package.GetGlobalService(typeof(DTE))
                    as DTE2;

            if (dte is null)
            {
                builder.AppendLine("DTE2: <null>");
                return builder.ToString();
            }

            builder.AppendLine($"DTE: {dte.Name} {dte.Version}");

            var commandBars =
                dte.CommandBars as CommandBars;

            if (commandBars is null)
            {
                builder.AppendLine("CommandBars: <null>");
                return builder.ToString();
            }

            builder.AppendLine($"CommandBars count: {commandBars.Count}");
            builder.AppendLine();
            builder.AppendLine("Result/Grid-related CommandBars:");

            var matches = new List<string>();

            foreach (CommandBar bar in commandBars)
            {
                var name = bar.Name ?? string.Empty;

                if (name.IndexOf("grid", StringComparison.OrdinalIgnoreCase) < 0 &&
                    name.IndexOf("result", StringComparison.OrdinalIgnoreCase) < 0 &&
                    name.IndexOf("sql", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                matches.Add(
                    $"- Name='{name}' | Type={bar.Type} | Visible={bar.Visible} | " +
                    $"Enabled={bar.Enabled} | Controls={bar.Controls.Count}");
            }

            foreach (var match in matches.OrderBy(x => x))
            {
                builder.AppendLine(match);
            }

            builder.AppendLine();

            var target =
                TryGetCommandBar(
                    commandBars,
                    ResultsGridContextMenuName);

            if (target is null)
            {
                builder.AppendLine(
                    $"Exact target '{ResultsGridContextMenuName}': NOT FOUND");

                return builder.ToString();
            }

            builder.AppendLine(
                $"Exact target '{ResultsGridContextMenuName}': FOUND");
            builder.AppendLine(
                $"Target controls: {target.Controls.Count}");

            for (var i = 1; i <= target.Controls.Count; i++)
            {
                try
                {
                    var control =
                        target.Controls[i];

                    builder.AppendLine(
                        $"  {i}. Caption='{control.Caption}' | " +
                        $"Type={control.Type} | Visible={control.Visible} | " +
                        $"Enabled={control.Enabled}");
                }
                catch (Exception ex)
                {
                    builder.AppendLine(
                        $"  {i}. <read failed: {ex.GetType().Name}> ");
                }
            }

            return builder.ToString();
        }

        private static CommandBar? TryGetCommandBar(
            CommandBars commandBars,
            string name)
        {
            try
            {
                return commandBars[name];
            }
            catch
            {
                return null;
            }
        }

        private static void RemoveExistingZraxionMenu(
            CommandBar commandBar)
        {
            for (var i = commandBar.Controls.Count; i >= 1; i--)
            {
                CommandBarControl? control = null;

                try
                {
                    control = commandBar.Controls[i];

                    if (!string.Equals(
                            control.Caption,
                            ZraxionCaption,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    control.Delete(Type.Missing);
                }
                catch
                {
                    // Never block SSMS startup because a stale temporary item
                    // cannot be inspected or removed.
                }
            }
        }

        private static void AddQuickStatsMenu(
            CommandBarPopup parent)
        {
            var quickStats =
                parent.Controls.Add(
                    MsoControlType.msoControlPopup,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    true)
                as CommandBarPopup;

            if (quickStats is null)
            {
                throw new InvalidOperationException(
                    "Zraxion could not create the Quick Stats menu.");
            }

            quickStats.Caption = "Quick Stats";
            quickStats.Visible = true;
            quickStats.Enabled = true;

            popups.Add(quickStats);

            AddButton(quickStats, "SUM", OnSumClick);
            AddButton(quickStats, "AVG", OnAverageClick);
            AddButton(quickStats, "COUNT", OnCountClick);
            AddButton(quickStats, "MIN", OnMinClick);
            AddButton(quickStats, "MAX", OnMaxClick);
        }

        private static void AddSeparatorButton(
            CommandBarPopup parent,
            string caption,
            _CommandBarButtonEvents_ClickEventHandler handler)
        {
            var button =
                AddButton(parent, caption, handler);

            button.BeginGroup = true;
        }

        private static CommandBarButton AddButton(
            CommandBarPopup parent,
            string caption,
            _CommandBarButtonEvents_ClickEventHandler handler)
        {
            var button =
                parent.Controls.Add(
                    MsoControlType.msoControlButton,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    true)
                as CommandBarButton;

            if (button is null)
            {
                throw new InvalidOperationException(
                    $"Zraxion could not create '{caption}'.");
            }

            button.Caption = caption;
            button.Visible = true;
            button.Enabled = true;
            button.Click += handler;

            buttons.Add(button);

            return button;
        }

        private static void OnSumClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;
            ShowStatistic("SUM", ResultsGridProbe.SumSelectedCells);
        }

        private static void OnAverageClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;
            ShowStatistic("AVG", ResultsGridProbe.AverageSelectedCells);
        }

        private static void OnCountClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;
            ShowStatistic("COUNT", ResultsGridProbe.CountSelectedCells);
        }

        private static void OnMinClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;
            ShowStatistic("MIN", ResultsGridProbe.MinSelectedCells);
        }

        private static void OnMaxClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;
            ShowStatistic("MAX", ResultsGridProbe.MaxSelectedCells);
        }

        private static void OnSetBaselineClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;

            var message =
                ResultsGridProbe
                    .SetCurrentSelectionAsComparisonBaseline();

            MessageBox.Show(
                message,
                "Zraxion - Set Selection as A",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static void OnCompareClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;

            var message =
                ResultsGridProbe
                    .CompareCurrentSelectionWithBaseline();

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            MessageBox.Show(
                message,
                "Zraxion - Compare with A",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private static void OnShowAnalyticsBarClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;

            GridAnalyticsBarManager.AttachToCurrentResults(
                showSelectionMetrics: true);
        }

        private static void ShowStatistic(
            string title,
            Func<string> calculate)
        {
            var message = calculate();

            MessageBox.Show(
                message,
                $"Zraxion - {title}",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
