using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Zraxion.Extension
{
    internal sealed class SumSelectedCellsCommand
    {
        public const int CommandId = 0x0102;
        public static readonly Guid CommandSet =
            new Guid("af1b02be-ba8f-4292-9d65-c7399d43c1e5");

        private SumSelectedCellsCommand(OleMenuCommandService commandService)
        {
            commandService.AddCommand(
                new MenuCommand(
                    Execute,
                    new CommandID(CommandSet, CommandId)));
        }

        public static SumSelectedCellsCommand? Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            var commandService =
                await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService
                ?? throw new InvalidOperationException(
                    "Zraxion could not acquire the SSMS command service.");

            Instance = new SumSelectedCellsCommand(commandService);
        }

        private static void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            GridAnalyticsBarManager.ShowSelectionMetric("SUM");
        }
    }
}
