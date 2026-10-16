using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Zraxion.Extension
{
    internal sealed class GridColumnAnalyticsFooter : Control
    {
        private readonly Label valueLabel;
        private readonly ToolTip toolTip;
        private readonly Font valueFont;
        private readonly ContextMenuStrip copyMenu;

        private string metric = string.Empty;
        private string displayValue = string.Empty;
        private string copyValue = string.Empty;

        internal GridColumnAnalyticsFooter()
        {
            Name = "ZraxionGridColumnAnalyticsFooter";

            Height = 24;
            BackColor = SystemColors.Control;
            ForeColor = SystemColors.ControlText;

            valueFont = new Font(
                SystemFonts.MessageBoxFont,
                FontStyle.Bold);

            valueLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(5, 0, 6, 0),
                BackColor = BackColor,
                ForeColor = ForeColor,
                Font = valueFont,
                Cursor = Cursors.Hand
            };

            toolTip = new ToolTip
            {
                ShowAlways = true
            };

            copyMenu = new ContextMenuStrip();

            var copyValueItem =
                new ToolStripMenuItem("Copy Value");

            copyValueItem.Click +=
                (_, __) => CopyValueToClipboard();

            var copyLabelAndValueItem =
                new ToolStripMenuItem("Copy Label + Value");

            copyLabelAndValueItem.Click +=
                (_, __) => CopyLabelAndValueToClipboard();

            copyMenu.Items.Add(copyValueItem);
            copyMenu.Items.Add(copyLabelAndValueItem);

            valueLabel.ContextMenuStrip = copyMenu;
            valueLabel.DoubleClick += OnValueLabelDoubleClick;

            Controls.Add(valueLabel);

            Visible = false;
        }

        internal int PreferredContentWidth
        {
            get
            {
                var measured =
                    TextRenderer.MeasureText(
                        valueLabel.Text ?? string.Empty,
                        valueLabel.Font,
                        Size.Empty,
                        TextFormatFlags.SingleLine |
                        TextFormatFlags.NoPadding);

                return measured.Width +
                    valueLabel.Padding.Horizontal +
                    14;
            }
        }

        internal void SetValue(
            string metric,
            string formattedValue,
            string rawCopyValue)
        {
            if (string.IsNullOrWhiteSpace(metric))
            {
                throw new ArgumentException(
                    "A metric name is required.",
                    nameof(metric));
            }

            this.metric =
                metric.Trim().ToUpperInvariant();

            displayValue =
                formattedValue ?? string.Empty;

            copyValue =
                rawCopyValue ?? string.Empty;

            var text =
                $"{this.metric}  {displayValue}";

            valueLabel.Text = text;

            toolTip.SetToolTip(
                valueLabel,
                text + Environment.NewLine +
                "Double-click: Copy Value");

            AccessibleName =
                $"Zraxion {this.metric} footer";
        }

        protected override void OnPaint(
            PaintEventArgs e)
        {
            base.OnPaint(e);

            using (var borderPen =
                new Pen(SystemColors.ControlDark))
            {
                e.Graphics.DrawLine(
                    borderPen,
                    0,
                    0,
                    Math.Max(0, Width - 1),
                    0);
            }
        }

        private void OnValueLabelDoubleClick(
            object? sender,
            EventArgs e)
        {
            CopyValueToClipboard();
        }

        private void CopyValueToClipboard()
        {
            TryCopyToClipboard(copyValue);
        }

        private void CopyLabelAndValueToClipboard()
        {
            var text =
                string.IsNullOrEmpty(metric)
                    ? displayValue
                    : $"{metric} {displayValue}";

            TryCopyToClipboard(text);
        }

        private static bool TryCopyToClipboard(
            string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            // Clipboard can briefly be locked by another Windows process.
            // Retry a few times without showing success/failure modal dialogs.
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    Clipboard.SetText(text);
                    return true;
                }
                catch (ExternalException)
                {
                    if (attempt < 2)
                    {
                        Thread.Sleep(25);
                    }
                }
            }

            return false;
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                valueLabel.DoubleClick -=
                    OnValueLabelDoubleClick;

                copyMenu.Dispose();
                toolTip.Dispose();
                valueFont.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
