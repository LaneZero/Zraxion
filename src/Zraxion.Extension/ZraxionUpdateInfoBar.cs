using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;

namespace Zraxion.Extension
{
    internal static class ZraxionUpdateInfoBar
    {
        private static InfoBarEventSink? activeEventSink;

        internal static async Task ShowAsync(AsyncPackage package, ZraxionReleaseInfo release)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();

            var shell = await package.GetServiceAsync(typeof(SVsShell)) as IVsShell;
            var factory = await package.GetServiceAsync(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;

            if (shell is null || factory is null)
            {
                return;
            }

            if (ErrorHandler.Failed(shell.GetProperty(
                    (int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost,
                    out var hostObject)) ||
                hostObject is not IVsInfoBarHost host)
            {
                return;
            }

            var releaseNotes = new InfoBarHyperlink("Release Notes");
            var openRelease = new InfoBarButton("Open Release");

            var model = new InfoBarModel(
                new IVsInfoBarTextSpan[]
                {
                    new InfoBarTextSpan("Zraxion " + release.TagName + " is available.")
                },
                new IVsInfoBarActionItem[]
                {
                    releaseNotes,
                    openRelease
                },
                isCloseButtonVisible: true);

            var uiElement = factory.CreateInfoBar(model);
            if (uiElement is null)
            {
                return;
            }

            var sink = new InfoBarEventSink(release, releaseNotes, openRelease);
            uiElement.Advise(sink, out var cookie);
            sink.SetCookie(cookie);
            activeEventSink = sink;
            host.AddInfoBar(uiElement);
        }

        internal static async Task ShowStatusAsync(
            AsyncPackage package,
            string message)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();

            var shell = await package.GetServiceAsync(typeof(SVsShell)) as IVsShell;
            var factory = await package.GetServiceAsync(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;

            if (shell is null || factory is null)
            {
                return;
            }

            if (ErrorHandler.Failed(shell.GetProperty(
                    (int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost,
                    out var hostObject)) ||
                hostObject is not IVsInfoBarHost host)
            {
                return;
            }

            var model = new InfoBarModel(
                new IVsInfoBarTextSpan[] { new InfoBarTextSpan(message) },
                Array.Empty<IVsInfoBarActionItem>(),
                isCloseButtonVisible: true);

            var uiElement = factory.CreateInfoBar(model);
            if (uiElement is not null)
            {
                host.AddInfoBar(uiElement);
            }
        }

        private sealed class InfoBarEventSink : IVsInfoBarUIEvents
        {
            private readonly ZraxionReleaseInfo release;
            private readonly IVsInfoBarActionItem releaseNotes;
            private readonly IVsInfoBarActionItem openRelease;
            private uint cookie;

            internal InfoBarEventSink(
                ZraxionReleaseInfo release,
                IVsInfoBarActionItem releaseNotes,
                IVsInfoBarActionItem openRelease)
            {
                this.release = release;
                this.releaseNotes = releaseNotes;
                this.openRelease = openRelease;
            }

            internal void SetCookie(uint value)
            {
                cookie = value;
            }

            public void OnActionItemClicked(
                IVsInfoBarUIElement infoBarUIElement,
                IVsInfoBarActionItem actionItem)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (ReferenceEquals(actionItem, releaseNotes) ||
                    ReferenceEquals(actionItem, openRelease))
                {
                    ZraxionWelcomeControl.OpenExternalUrl(release.HtmlUrl);
                }
            }

            public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (cookie != 0)
                {
                    infoBarUIElement.Unadvise(cookie);
                    cookie = 0;
                }

                if (ReferenceEquals(activeEventSink, this))
                {
                    activeEventSink = null;
                }
            }
        }
    }
}
