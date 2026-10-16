using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Zraxion.Extension
{
    internal sealed class AboutZraxionCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet =
            new Guid("af1b02be-ba8f-4292-9d65-c7399d43c1e5");

        private readonly ZraxionExtensionPackage package;

        private AboutZraxionCommand(
            ZraxionExtensionPackage package,
            OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService.AddCommand(
                new MenuCommand(
                    Execute,
                    new CommandID(CommandSet, CommandId)));
        }

        public static AboutZraxionCommand? Instance { get; private set; }

        public static async Task InitializeAsync(ZraxionExtensionPackage package)
        {
            var commandService =
                await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService
                ?? throw new InvalidOperationException(
                    "Zraxion could not acquire the SSMS command service.");

            Instance = new AboutZraxionCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var task = package.JoinableTaskFactory.RunAsync(
                () => package.ShowWelcomeToolWindowAsync(force: true));
            _ = task.Task;
        }
    }
}
