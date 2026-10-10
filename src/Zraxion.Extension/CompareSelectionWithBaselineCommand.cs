using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Zraxion.Extension
{
    internal sealed class CompareSelectionWithBaselineCommand
    {
        public const int CommandId = 0x0108;

        public static readonly Guid CommandSet =
            new Guid("af1b02be-ba8f-4292-9d65-c7399d43c1e5");

        private readonly AsyncPackage package;

        private CompareSelectionWithBaselineCommand(
            AsyncPackage package,
            OleMenuCommandService commandService)
        {
            this.package =
                package ?? throw new ArgumentNullException(nameof(package));

            commandService =
                commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandId =
                new CommandID(CommandSet, CommandId);

            var menuItem =
                new MenuCommand(this.Execute, menuCommandId);

            commandService.AddCommand(menuItem);
        }

        public static CompareSelectionWithBaselineCommand? Instance
        {
            get;
            private set;
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            var commandService =
                await package.GetServiceAsync(typeof(IMenuCommandService))
                    as OleMenuCommandService
                ?? throw new InvalidOperationException(
                    "Zraxion could not acquire the SSMS command service.");

            Instance = new CompareSelectionWithBaselineCommand(
                package,
                commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var message =
                ResultsGridProbe.CompareCurrentSelectionWithBaseline();

            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                "Zraxion - Compare with A",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}