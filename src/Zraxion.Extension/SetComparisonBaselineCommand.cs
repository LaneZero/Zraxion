using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Zraxion.Extension
{
    internal sealed class SetComparisonBaselineCommand
    {
        public const int CommandId = 0x0107;
        public static readonly Guid CommandSet =
            new Guid("af1b02be-ba8f-4292-9d65-c7399d43c1e5");

        private readonly AsyncPackage package;

        private SetComparisonBaselineCommand(
            AsyncPackage package,
            OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService.AddCommand(
                new MenuCommand(
                    Execute,
                    new CommandID(CommandSet, CommandId)));
        }

        public static SetComparisonBaselineCommand? Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            var commandService =
                await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService
                ?? throw new InvalidOperationException(
                    "Zraxion could not acquire the SSMS command service.");

            Instance = new SetComparisonBaselineCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var message = ResultsGridProbe.SetCurrentSelectionAsComparisonBaseline();

            if (message.StartsWith(
                    "Comparison baseline A saved.",
                    StringComparison.Ordinal))
            {
                GridAnalyticsBarManager.AttachToCurrentResults(showSelectionMetrics: true);
                return;
            }

            VsShellUtilities.ShowMessageBox(
                package,
                message,
                "Zraxion - Set Selection as A",
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
