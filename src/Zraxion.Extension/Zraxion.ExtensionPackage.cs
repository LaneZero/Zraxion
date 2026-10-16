using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Zraxion.Extension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(
        UseManagedResourcesOnly = true,
        AllowsBackgroundLoading = true)]
    [Guid(ZraxionExtensionPackage.PackageGuidString)]

    [ProvideAutoLoad(
        VSConstants.UICONTEXT.NoSolution_string,
        PackageAutoLoadFlags.BackgroundLoad)]

    [ProvideAutoLoad(
        VSConstants.UICONTEXT.SolutionExists_string,
        PackageAutoLoadFlags.BackgroundLoad)]

    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(ZraxionWelcomeToolWindow), DocumentLikeTool = true)]
    public sealed class ZraxionExtensionPackage : AsyncPackage
    {
        /// <summary>
        /// ZraxionExtensionPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "adba31ac-1eb5-48ec-b2e9-5d4fd241b075";

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            // Command registration requires access to the SSMS UI thread.
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Register Zraxion commands once.
            await AboutZraxionCommand.InitializeAsync(this);
            await InspectResultsGridCommand.InitializeAsync(this);
            await SumSelectedCellsCommand.InitializeAsync(this);
            await AverageSelectedCellsCommand.InitializeAsync(this);
            await MinSelectedCellsCommand.InitializeAsync(this);
            await MaxSelectedCellsCommand.InitializeAsync(this);
            await CountSelectedCellsCommand.InitializeAsync(this);
            await SetComparisonBaselineCommand.InitializeAsync(this);
            await CompareSelectionWithBaselineCommand.InitializeAsync(this);
            await CheckForZraxionUpdatesCommand.InitializeAsync(this);

            ResultsGridContextMenuManager.Initialize();

            // Show Welcome once per installed version and check for updates without
            // blocking the normal SSMS startup path.
            ZraxionStartupCoordinator.Start(this);

        }








        internal async Task ShowWelcomeToolWindowAsync(bool force)
        {
            if (!force &&
                !ZraxionWelcomeState.ShouldShowForCurrentVersion())
            {
                return;
            }

            var pane =
                await ShowToolWindowAsync(
                    typeof(ZraxionWelcomeToolWindow),
                    0,
                    true,
                    DisposalToken);

            if (pane is null)
            {
                return;
            }

            // IVsWindowFrame is a UI-thread-affine COM service. ShowToolWindowAsync
            // may resume on a background thread, so explicitly switch back before
            // reading Frame, changing frame mode, or calling Show().
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);

            // Welcome belongs in the SSMS document well, not as a floating
            // utility window. Force document-like MDI mode even if SSMS has
            // persisted an older floating placement from a previous build.
            if (pane.Frame is IVsWindowFrame frame)
            {
                ErrorHandler.ThrowOnFailure(
                    frame.SetProperty(
                        (int)__VSFPROPID.VSFPROPID_FrameMode,
                        (int)VsFrameMode.MdiChild));

                ErrorHandler.ThrowOnFailure(
                    frame.Show());
            }

            ZraxionWelcomeState.MarkCurrentVersionShown();
        }

        #endregion
    }
}

