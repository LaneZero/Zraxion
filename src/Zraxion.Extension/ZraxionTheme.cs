using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Zraxion.Extension
{
    internal readonly struct ZraxionThemePalette
    {
        public ZraxionThemePalette(
            Color background,
            Color foreground,
            Color mutedForeground,
            Color border,
            Color hoverBackground)
        {
            Background = background;
            Foreground = foreground;
            MutedForeground = mutedForeground;
            Border = border;
            HoverBackground = hoverBackground;
        }

        public Color Background { get; }

        public Color Foreground { get; }

        public Color MutedForeground { get; }

        public Color Border { get; }

        public Color HoverBackground { get; }
    }

    internal static class ZraxionTheme
    {
        internal static ZraxionThemePalette Resolve(
            Control? source)
        {
            // SSMS 22 is hosted on the Visual Studio shell. Its Results Grid is
            // custom-rendered, so WinForms Parent.BackColor is not a reliable
            // indicator of the active SSMS theme. Read the shell theme tokens
            // first and only fall back to WinForms colors if the shell lookup
            // is unavailable for any reason.
            try
            {
                var background =
                    VSColorTheme.GetThemedColor(
                        EnvironmentColors.ToolWindowBackgroundColorKey);

                var foreground =
                    VSColorTheme.GetThemedColor(
                        EnvironmentColors.ToolWindowTextColorKey);

                var border =
                    VSColorTheme.GetThemedColor(
                        EnvironmentColors.ToolWindowBorderColorKey);

                if (IsUsable(
                        background,
                        foreground))
                {
                    // Do not use the shell's ToolWindowButtonHoverActive color
                    // directly for ContextMenuStrip selection. In SSMS Dark
                    // theme that token can resolve to a very light surface while
                    // ToolStrip text remains light, making hovered menu text
                    // effectively invisible. Build a restrained hover surface
                    // from the active background/foreground pair instead.
                    var safeHover =
                        Blend(
                            background,
                            foreground,
                            0.12f);

                    return
                        new ZraxionThemePalette(
                            background,
                            foreground,
                            Blend(
                                background,
                                foreground,
                                0.58f),
                            border.IsEmpty
                                ? Blend(
                                    background,
                                    foreground,
                                    0.28f)
                                : border,
                            safeHover);
                }
            }
            catch
            {
                // Theme services can be transiently unavailable while SSMS is
                // creating/rebuilding a shell surface. Fall back safely.
            }

            return
                ResolveFromWinForms(
                    source);
        }

        internal static void ApplyToControlTree(
            Control root,
            ZraxionThemePalette palette)
        {
            if (root is null)
            {
                throw new ArgumentNullException(
                    nameof(root));
            }

            ApplyToControl(
                root,
                palette);

            foreach (Control child in root.Controls)
            {
                ApplyToControlTree(
                    child,
                    palette);
            }
        }

        internal static void ApplyToMenu(
            ContextMenuStrip menu,
            ZraxionThemePalette palette)
        {
            if (menu is null)
            {
                throw new ArgumentNullException(
                    nameof(menu));
            }

            menu.BackColor =
                palette.Background;

            menu.ForeColor =
                palette.Foreground;

            menu.Renderer =
                new ToolStripProfessionalRenderer(
                    new ThemeColorTable(
                        palette));

            foreach (ToolStripItem item in menu.Items)
            {
                ApplyToMenuItem(
                    item,
                    palette);
            }
        }

        private static ZraxionThemePalette ResolveFromWinForms(
            Control? source)
        {
            var background =
                ResolveBackground(
                    source);

            var foreground =
                ResolveForeground(
                    source,
                    background);

            return
                new ZraxionThemePalette(
                    background,
                    foreground,
                    Blend(
                        background,
                        foreground,
                        0.58f),
                    Blend(
                        background,
                        foreground,
                        0.28f),
                    Blend(
                        background,
                        foreground,
                        0.08f));
        }

        private static void ApplyToMenuItem(
            ToolStripItem item,
            ZraxionThemePalette palette)
        {
            item.BackColor =
                palette.Background;

            item.ForeColor =
                item.Enabled
                    ? palette.Foreground
                    : palette.MutedForeground;

            if (item is ToolStripMenuItem menuItem)
            {
                foreach (ToolStripItem child in menuItem.DropDownItems)
                {
                    ApplyToMenuItem(
                        child,
                        palette);
                }
            }
        }

        private static void ApplyToControl(
            Control control,
            ZraxionThemePalette palette)
        {
            if (control is TextBoxBase)
            {
                return;
            }

            control.BackColor =
                palette.Background;

            control.ForeColor =
                control.Enabled
                    ? palette.Foreground
                    : palette.MutedForeground;

            if (control is Button button)
            {
                button.FlatAppearance.BorderColor =
                    palette.Border;

                button.FlatAppearance.MouseOverBackColor =
                    palette.HoverBackground;

                button.FlatAppearance.MouseDownBackColor =
                    palette.HoverBackground;
            }
        }

        private static bool IsUsable(
            Color background,
            Color foreground)
        {
            return
                !background.IsEmpty &&
                !foreground.IsEmpty &&
                HasUsefulContrast(
                    background,
                    foreground);
        }

        private static Color ResolveBackground(
            Control? source)
        {
            for (var current = source;
                 current is not null;
                 current = current.Parent)
            {
                var candidate =
                    current.BackColor;

                if (!candidate.IsEmpty &&
                    candidate != Color.Transparent)
                {
                    return candidate;
                }
            }

            return SystemColors.Control;
        }

        private static Color ResolveForeground(
            Control? source,
            Color background)
        {
            for (var current = source;
                 current is not null;
                 current = current.Parent)
            {
                var candidate =
                    current.ForeColor;

                if (!candidate.IsEmpty &&
                    HasUsefulContrast(
                        background,
                        candidate))
                {
                    return candidate;
                }
            }

            return
                IsDark(background)
                    ? Color.FromArgb(
                        241,
                        241,
                        241)
                    : Color.FromArgb(
                        32,
                        32,
                        32);
        }

        private static bool HasUsefulContrast(
            Color background,
            Color foreground)
        {
            return
                Math.Abs(
                    GetLuminance(background) -
                    GetLuminance(foreground)) >=
                0.28d;
        }

        private static bool IsDark(
            Color color)
        {
            return
                GetLuminance(color) <
                0.50d;
        }

        private static double GetLuminance(
            Color color)
        {
            return
                (0.2126d * color.R +
                 0.7152d * color.G +
                 0.0722d * color.B) /
                255d;
        }

        private static Color Blend(
            Color background,
            Color foreground,
            float foregroundAmount)
        {
            var amount =
                Math.Max(
                    0f,
                    Math.Min(
                        1f,
                        foregroundAmount));

            var backgroundAmount =
                1f -
                amount;

            return
                Color.FromArgb(
                    255,
                    Clamp(
                        (int)Math.Round(
                            background.R * backgroundAmount +
                            foreground.R * amount)),
                    Clamp(
                        (int)Math.Round(
                            background.G * backgroundAmount +
                            foreground.G * amount)),
                    Clamp(
                        (int)Math.Round(
                            background.B * backgroundAmount +
                            foreground.B * amount)));
        }

        private static int Clamp(
            int value)
        {
            return
                Math.Max(
                    0,
                    Math.Min(
                        255,
                        value));
        }

        private sealed class ThemeColorTable :
            ProfessionalColorTable
        {
            private readonly ZraxionThemePalette palette;

            public ThemeColorTable(
                ZraxionThemePalette palette)
            {
                this.palette =
                    palette;

                UseSystemColors =
                    false;
            }

            public override Color ToolStripDropDownBackground =>
                palette.Background;

            public override Color ImageMarginGradientBegin =>
                palette.Background;

            public override Color ImageMarginGradientMiddle =>
                palette.Background;

            public override Color ImageMarginGradientEnd =>
                palette.Background;

            public override Color MenuBorder =>
                palette.Border;

            public override Color MenuItemBorder =>
                palette.Border;

            public override Color MenuItemSelected =>
                palette.HoverBackground;

            public override Color MenuItemSelectedGradientBegin =>
                palette.HoverBackground;

            public override Color MenuItemSelectedGradientEnd =>
                palette.HoverBackground;

            public override Color MenuItemPressedGradientBegin =>
                palette.HoverBackground;

            public override Color MenuItemPressedGradientMiddle =>
                palette.HoverBackground;

            public override Color MenuItemPressedGradientEnd =>
                palette.HoverBackground;

            public override Color SeparatorDark =>
                palette.Border;

            public override Color SeparatorLight =>
                palette.Background;
        }
    }
}
