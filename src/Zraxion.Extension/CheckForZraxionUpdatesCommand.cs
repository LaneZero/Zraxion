using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Zraxion.Extension
{
    internal sealed class CheckForZraxionUpdatesCommand
    {
        internal const int CommandId = 0x010A;
        internal static readonly Guid CommandSet =
            new Guid("af1b02be-ba8f-4292-9d65-c7399d43c1e5");

        private readonly AsyncPackage package;

        private CheckForZraxionUpdatesCommand(
            AsyncPackage package,
            OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService.AddCommand(
                new MenuCommand(
                    Execute,
                    new CommandID(CommandSet, CommandId)));
        }

        internal static async Task InitializeAsync(AsyncPackage package)
        {
            var commandService =
                await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService
                ?? throw new InvalidOperationException(
                    "Zraxion could not acquire the SSMS command service.");

            _ = new CheckForZraxionUpdatesCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var task = package.JoinableTaskFactory.RunAsync(async delegate
            {
                var release = await Task.Run(
                    () => ZraxionUpdateService.CheckForUpdateAsync());

                if (release is null)
                {
                    await ZraxionUpdateInfoBar.ShowStatusAsync(
                        package,
                        "Zraxion is up to date.");
                    return;
                }

                await ZraxionUpdateInfoBar.ShowAsync(package, release);
            });

            _ = task.Task;
        }
    }
}
