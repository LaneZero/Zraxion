using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Zraxion.Extension
{
    [Guid("a9330998-e297-4b39-9e3e-37eeaf90396e")]
    internal sealed class ZraxionWelcomeToolWindow : ToolWindowPane
    {
        private readonly ZraxionWelcomeControl control;

        public ZraxionWelcomeToolWindow()
            : base(null)
        {
            Caption = "Zraxion";
            control = new ZraxionWelcomeControl();
        }

        public override IWin32Window Window => control;
    }
}
