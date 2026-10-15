using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.CommandBars;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
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

        private static bool initialized;

        internal static void Initialize()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (initialized)
            {
                return;
            }

            var dte =
                Package.GetGlobalService(typeof(DTE))
                    as DTE2
                ?? throw new InvalidOperationException(
                    "Zraxion could not acquire the SSMS DTE service.");

            var commandBars =
                dte.CommandBars as CommandBars
                ?? throw new InvalidOperationException(
                    "Zraxion could not access SSMS command bars.");

            CommandBar gridCommandBar;

            try
            {
                gridCommandBar =
                    commandBars[ResultsGridContextMenuName];
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"The SSMS Results Grid context menu " +
                    $"'{ResultsGridContextMenuName}' was not found.",
                    ex);
            }

            RemoveExistingZraxionMenu(gridCommandBar);

            var zraxionPopup =
                gridCommandBar.Controls.Add(
                    MsoControlType.msoControlPopup,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    true)
                as CommandBarPopup
                ?? throw new InvalidOperationException(
                    "Zraxion could not create the Results Grid context menu.");

            zraxionPopup.Caption = ZraxionCaption;
            zraxionPopup.Visible = true;
            zraxionPopup.Enabled = true;
            zraxionPopup.BeginGroup = true;

            popups.Add(zraxionPopup);

            AddSelectionAnalyticsMenu(zraxionPopup);
            AddCompareMenu(zraxionPopup);

            AddSeparatorButton(
                zraxionPopup,
                "Show Analytics Bar",
                OnShowAnalyticsBarClick);

            initialized = true;
        }

        private static void AddSelectionAnalyticsMenu(
            CommandBarPopup parent)
        {
            var analyticsPopup =
                parent.Controls.Add(
                    MsoControlType.msoControlPopup,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    true)
                as CommandBarPopup
                ?? throw new InvalidOperationException(
                    "Zraxion could not create the Selection Analytics menu.");

            analyticsPopup.Caption = "Selection Analytics";
            analyticsPopup.Visible = true;
            analyticsPopup.Enabled = true;

            popups.Add(analyticsPopup);

            AddButton(analyticsPopup, "SUM", OnSumClick);
            AddButton(analyticsPopup, "AVG", OnAverageClick);
            AddButton(analyticsPopup, "COUNT", OnCountClick);
            AddButton(analyticsPopup, "MIN", OnMinClick);
            AddButton(analyticsPopup, "MAX", OnMaxClick);
        }

        private static void AddCompareMenu(
            CommandBarPopup parent)
        {
            var comparePopup =
                parent.Controls.Add(
                    MsoControlType.msoControlPopup,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    true)
                as CommandBarPopup
                ?? throw new InvalidOperationException(
                    "Zraxion could not create the Compare menu.");

            comparePopup.Caption = "Compare";
            comparePopup.Visible = true;
            comparePopup.Enabled = true;
            comparePopup.BeginGroup = true;

            popups.Add(comparePopup);

            AddButton(
                comparePopup,
                "Set Selection as A",
                OnSetBaselineClick);

            AddButton(
                comparePopup,
                "Compare Selection with A",
                OnCompareClick);

            AddSeparatorButton(
                comparePopup,
                "Clear Baseline A",
                OnClearBaselineClick);
        }

        private static void RemoveExistingZraxionMenu(
            CommandBar commandBar)
        {
            for (var i = commandBar.Controls.Count; i >= 1; i--)
            {
                try
                {
                    var control =
                        commandBar.Controls[i];

                    if (string.Equals(
                            control.Caption,
                            ZraxionCaption,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        control.Delete(Type.Missing);
                    }
                }
                catch
                {
                    // A stale temporary CommandBar item must never block SSMS startup.
                }
            }

            buttons.Clear();
            popups.Clear();
        }

        private static void AddSeparatorButton(
            CommandBarPopup parent,
            string caption,
            _CommandBarButtonEvents_ClickEventHandler handler)
        {
            var button =
                AddButton(
                    parent,
                    caption,
                    handler);

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
                as CommandBarButton
                ?? throw new InvalidOperationException(
                    $"Zraxion could not create '{caption}'.");

            button.Caption = caption;
            button.Visible = true;
            button.Enabled = true;
            button.Click += handler;

            // Keep COM event sources alive for the package lifetime.
            buttons.Add(button);

            return button;
        }

        private static void OnSumClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;
            GridAnalyticsBarManager.ShowSelectionMetric("SUM");
        }

        private static void OnAverageClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;
            GridAnalyticsBarManager.ShowSelectionMetric("AVG");
        }

        private static void OnCountClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;
            GridAnalyticsBarManager.ShowSelectionMetric("COUNT");
        }

        private static void OnMinClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;
            GridAnalyticsBarManager.ShowSelectionMetric("MIN");
        }

        private static void OnMaxClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;
            GridAnalyticsBarManager.ShowSelectionMetric("MAX");
        }

        private static void OnSetBaselineClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;

            var message =
                ResultsGridProbe
                    .SetCurrentSelectionAsComparisonBaseline();

            if (message.StartsWith(
                    "Comparison baseline A saved.",
                    StringComparison.Ordinal))
            {
                // No success modal. The analytics bar is the feedback surface.
                GridAnalyticsBarManager.AttachToCurrentResults(
                    showSelectionMetrics: true);

                return;
            }

            MessageBox.Show(
                message,
                "Zraxion - Set Selection as A",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private static void OnCompareClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;

            var message =
                ResultsGridProbe
                    .CompareCurrentSelectionWithBaseline();

            // Success is rendered in the analytics bar by ResultsGridProbe.
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            MessageBox.Show(
                message,
                "Zraxion - Compare Selection with A",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private static void OnClearBaselineClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;

            GridComparisonSession.Clear();
            GridAnalyticsBarManager.ClearComparisonView();
        }

        private static void OnShowAnalyticsBarClick(
            CommandBarButton ctrl,
            ref bool cancelDefault)
        {
            cancelDefault = true;

            GridAnalyticsBarManager.AttachToCurrentResults(
                showSelectionMetrics: true);
        }
    }
}
