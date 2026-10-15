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
    internal static class GridComparisonSession
    {
        public static GridAnalyticsSnapshot? LeftSnapshot
        {
            get;
            private set;
        }

        public static void SetLeft(
            GridAnalyticsSnapshot snapshot)
        {
            LeftSnapshot = snapshot;
        }

        public static void Clear()
        {
            LeftSnapshot = null;
        }
    }
}
