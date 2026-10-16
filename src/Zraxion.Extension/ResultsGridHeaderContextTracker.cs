using Microsoft.SqlServer.Management.UI.Grid;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Zraxion.Extension
{
    internal static class ResultsGridHeaderContextTracker
    {
        private const int WmRightButtonDown = 0x0204;
        private const int WmContextMenu = 0x007B;

        private static readonly TimeSpan ContextLifetime =
            TimeSpan.FromSeconds(30);

        private static readonly HeaderMessageFilter messageFilter =
            new HeaderMessageFilter();

        private static bool initialized;
        private static GridControl? lastGrid;
        private static int lastUiColumn = -1;
        private static DateTime capturedAtUtc;

        internal static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            Application.AddMessageFilter(
                messageFilter);

            initialized = true;
        }

        internal static bool TryGetRecentHeaderContext(
            out GridControl? grid,
            out int uiColumn)
        {
            grid = null;
            uiColumn = -1;

            var candidateGrid =
                lastGrid;

            if (candidateGrid is null ||
                candidateGrid.IsDisposed ||
                lastUiColumn < 0)
            {
                return false;
            }

            if (DateTime.UtcNow - capturedAtUtc >
                ContextLifetime)
            {
                Clear();
                return false;
            }

            grid = candidateGrid;
            uiColumn = lastUiColumn;

            return true;
        }

        private static void Capture(
            GridControl grid,
            int uiColumn)
        {
            lastGrid = grid;
            lastUiColumn = uiColumn;
            capturedAtUtc = DateTime.UtcNow;
        }

        private static void Clear()
        {
            lastGrid = null;
            lastUiColumn = -1;
            capturedAtUtc = default;
        }

        private static GridControl? FindGridControl(
            IntPtr windowHandle)
        {
            var currentHandle =
                windowHandle;

            while (currentHandle != IntPtr.Zero)
            {
                var control =
                    Control.FromHandle(
                        currentHandle);

                while (control is not null)
                {
                    if (control is GridControl grid)
                    {
                        return grid;
                    }

                    control = control.Parent;
                }

                currentHandle =
                    GetParent(currentHandle);
            }

            return null;
        }

        private static bool TryCaptureHeaderContext(
            IntPtr windowHandle)
        {
            var grid =
                FindGridControl(
                    windowHandle);

            if (grid is null ||
                grid.IsDisposed ||
                !grid.WithHeader)
            {
                return false;
            }

            var point =
                grid.PointToClient(
                    Cursor.Position);

            if (point.Y < 0 ||
                point.Y >= grid.HeaderHeight)
            {
                return false;
            }

            HitTestInfo hit;

            try
            {
                hit =
                    grid.HitTest(
                        point.X,
                        point.Y);
            }
            catch
            {
                return false;
            }

            if (hit.ColumnIndex < 0 ||
                hit.ColumnIndex >= grid.ColumnsNumber)
            {
                return false;
            }

            Capture(
                grid,
                hit.ColumnIndex);

            return true;
        }

        private sealed class HeaderMessageFilter :
            IMessageFilter
        {
            public bool PreFilterMessage(
                ref Message m)
            {
                if (m.Msg != WmRightButtonDown &&
                    m.Msg != WmContextMenu)
                {
                    return false;
                }

                if (!TryCaptureHeaderContext(
                        m.HWnd) &&
                    m.Msg == WmRightButtonDown)
                {
                    Clear();
                }

                return false;
            }
        }

        [DllImport(
            "user32.dll",
            SetLastError = false)]
        private static extern IntPtr GetParent(
            IntPtr hWnd);
    }
}
