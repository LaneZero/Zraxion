using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace Zraxion.Extension
{
    internal sealed class InspectResultsGridCommand
    {
        public const int CommandId = 0x0101;

        public static readonly Guid CommandSet =
            new Guid("af1b02be-ba8f-4292-9d65-c7399d43c1e5");

        private readonly AsyncPackage package;

        private InspectResultsGridCommand(
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

        public static InspectResultsGridCommand? Instance
        {
            get;
            private set;
        }

        public static async Task InitializeAsync(
            AsyncPackage package)
        {
            var commandService =
                await package.GetServiceAsync(typeof(IMenuCommandService))
                    as OleMenuCommandService
                ?? throw new InvalidOperationException(
                    "Zraxion could not acquire the SSMS command service.");

            Instance =
                new InspectResultsGridCommand(
                    package,
                    commandService);
        }

        private void Execute(
            object sender,
            EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            GridAnalyticsBarManager.AttachToCurrentResults();
        }
    }
}