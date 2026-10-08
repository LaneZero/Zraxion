using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace Zraxion.Extension
{
    /// <summary>
    /// Provides the diagnostic command used to validate Results Grid integration.
    /// </summary>
    internal sealed class InspectResultsGridCommand
    {
        /// <summary>
        /// Command identifier registered in the Zraxion command set.
        /// </summary>
        public const int CommandId = 0x0101;

        /// <summary>
        /// Shared Zraxion command set GUID.
        /// This must match guidZraxionExtensionPackageCmdSet in the VSCT file.
        /// </summary>
        public static readonly Guid CommandSet =
            new Guid("af1b02be-ba8f-4292-9d65-c7399d43c1e5");

        private readonly AsyncPackage package;

        private InspectResultsGridCommand(
            AsyncPackage package,
            OleMenuCommandService commandService)
        {
            this.package = package ??
                throw new ArgumentNullException(nameof(package));

            if (commandService is null)
            {
                throw new ArgumentNullException(nameof(commandService));
            }

            var commandId = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, commandId);

            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the initialized command instance.
        /// It remains null until initialization completes successfully.
        /// </summary>
        public static InspectResultsGridCommand? Instance { get; private set; }

        /// <summary>
        /// Registers the command with the SSMS command service.
        /// </summary>
        /// <param name="package">The owning Zraxion package.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            if (package is null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(
                package.DisposalToken);

            var commandService =
                await package.GetServiceAsync(typeof(IMenuCommandService))
                    as OleMenuCommandService
                ?? throw new InvalidOperationException(
                    "Zraxion could not acquire the SSMS command service.");

            Instance = new InspectResultsGridCommand(
                package,
                commandService);
        }

        /// <summary>
        /// Executes the initial Results Grid feasibility probe.
        /// Actual Grid discovery will be added after command registration
        /// has been verified inside SSMS 22.
        /// </summary>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var message = ResultsGridProbe.InspectSelectedCellValues();

            VsShellUtilities.ShowMessageBox(
                package,
                message,
                "Zraxion - Results Grid Probe",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}