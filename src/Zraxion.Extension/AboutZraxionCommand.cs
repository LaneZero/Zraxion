using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Zraxion.Extension
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class AboutZraxionCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("af1b02be-ba8f-4292-9d65-c7399d43c1e5");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutZraxionCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private AboutZraxionCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the initialized command instance.
        /// The value is null until initialization completes successfully.
        /// </summary>
        public static AboutZraxionCommand? Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            var commandService =
            await package.GetServiceAsync(typeof(IMenuCommandService))
                as OleMenuCommandService
            ?? throw new InvalidOperationException(
                "Zraxion could not acquire the SSMS command service.");

            Instance = new AboutZraxionCommand(package, commandService);
        }

        /// <summary>
        /// Displays basic information about the Zraxion extension.
        /// This command also verifies that the Zraxion package has been
        /// successfully loaded inside SQL Server Management Studio.
        /// </summary>
        /// <param name="sender">The command sender.</param>
        /// <param name="e">Command event arguments.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            const string title = "About Zraxion";
            const string message =
                "Zraxion v0.1.0\n\n" +
                "SQL Server Management Studio productivity and data analysis tools.\n\n" +
                "Publisher: LaneZero";

            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
