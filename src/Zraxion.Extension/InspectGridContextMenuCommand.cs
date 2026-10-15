using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Zraxion.Extension
{
    internal sealed class InspectGridContextMenuCommand
    {
        public const int CommandId = 0x0109;

        public static readonly Guid CommandSet =
            new Guid("af1b02be-ba8f-4292-9d65-c7399d43c1e5");

        private readonly AsyncPackage package;

        private InspectGridContextMenuCommand(
            AsyncPackage package,
            OleMenuCommandService commandService)
        {
            this.package =
                package ?? throw new ArgumentNullException(nameof(package));

            commandService =
                commandService ?? throw new ArgumentNullException(nameof(commandService));

            var commandId =
                new CommandID(CommandSet, CommandId);

            var menuItem =
                new MenuCommand(Execute, commandId);

            commandService.AddCommand(menuItem);
        }

        public static InspectGridContextMenuCommand? Instance
        {
            get;
            private set;
        }

        public static async Task InitializeAsync(
            AsyncPackage package)
        {
            var commandService =
                await package.GetServiceAsync(
                    typeof(IMenuCommandService))
                    as OleMenuCommandService
                ?? throw new InvalidOperationException(
                    "Zraxion could not acquire the SSMS command service.");

            Instance =
                new InspectGridContextMenuCommand(
                    package,
                    commandService);
        }

        private void Execute(
            object sender,
            EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var report =
                ResultsGridContextMenuManager
                    .GetDiagnosticReport();

            try
            {
                Clipboard.SetText(report);

                VsShellUtilities.ShowMessageBox(
                    package,
                    "CommandBars diagnostic copied to the clipboard.\n\n" +
                    "Paste the report back into ChatGPT.",
                    "Zraxion - CommandBars Diagnostic",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    package,
                    report + "\n\nClipboard error:\n" + ex.Message,
                    "Zraxion - CommandBars Diagnostic",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}
