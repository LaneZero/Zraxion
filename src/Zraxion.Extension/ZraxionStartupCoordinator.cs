using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;

namespace Zraxion.Extension
{
    internal static class ZraxionStartupCoordinator
    {
        internal static void Start(ZraxionExtensionPackage package)
        {
            var startupTask =
                package.JoinableTaskFactory.RunAsync(async delegate
            {
                await Task.Delay(1200);
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();

                await package.ShowWelcomeToolWindowAsync(force: false);

                ZraxionReleaseInfo? release = null;
                try
                {
                    release = await Task.Run(
                        () => ZraxionUpdateService.CheckForUpdateAsync());
                }
                catch
                {
                    // Update discovery must never interfere with SSMS startup.
                }

                if (release is null)
                {
                    return;
                }

                await ZraxionUpdateInfoBar.ShowAsync(package, release);
            });

            // Observe the JoinableTask without blocking SSMS startup.
            _ = startupTask.Task;
        }
    }
}
