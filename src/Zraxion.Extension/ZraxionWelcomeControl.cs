using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Zraxion.Extension
{
    internal sealed class ZraxionWelcomeControl : UserControl
    {
        private readonly Color surface;
        private readonly Color surfaceAlt;
        private readonly Color foreground;
        private readonly Color muted;
        private readonly Color border;
        private readonly Color accent;

        internal ZraxionWelcomeControl()
        {
            var palette = ZraxionTheme.Resolve(null);

            surface = palette.Background;
            foreground = palette.Foreground;
            muted = palette.MutedForeground;
            border = palette.Border;
            surfaceAlt = palette.HoverBackground;
            accent = SystemColors.Highlight;

            Dock = DockStyle.Fill;
            AutoScroll = true;
            BackColor = surface;
            ForeColor = foreground;
            Padding = new Padding(30, 26, 30, 28);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 7,
                BackColor = surface,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            root.Controls.Add(CreateHero());
            root.Controls.Add(CreateLogoPanel());
            root.Controls.Add(CreateFeatureStrip());
            root.Controls.Add(CreateCards());
            root.Controls.Add(CreateSupportSection());
            root.Controls.Add(CreateQuickStart());
            root.Controls.Add(CreateFooter());

            Controls.Add(root);
        }

        private Control CreateHero()
        {
            var hero = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 0, 0, 22),
                Margin = Padding.Empty,
                BackColor = surface
            };

            hero.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            hero.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var text = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = Padding.Empty,
                BackColor = surface
            };

            text.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "ZRAXION",
                Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 25F, FontStyle.Bold),
                ForeColor = foreground,
                Margin = new Padding(0, 0, 0, 3)
            });

            text.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(760, 0),
                Text = "Financial-grade Results Grid analytics for SQL Server Management Studio 22.",
                Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 11F, FontStyle.Regular),
                ForeColor = muted,
                Margin = new Padding(0, 0, 0, 12)
            });

            text.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(820, 0),
                Text = "Analyze selections, reconcile values with A/B comparison, apply tolerance rules, and preserve exact SQL DECIMAL/NUMERIC precision without leaving the Results Grid.",
                ForeColor = foreground,
                Margin = Padding.Empty
            });

            var badge = new Label
            {
                AutoSize = true,
                Text = "  v" + ZraxionProductInfo.Version + "  ",
                ForeColor = SystemColors.HighlightText,
                BackColor = accent,
                Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
                Padding = new Padding(6, 4, 6, 4),
                Margin = new Padding(14, 5, 0, 0)
            };

            hero.Controls.Add(text, 0, 0);
            hero.Controls.Add(badge, 1, 0);
            return hero;
        }

        private Control CreateLogoPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = surface,
                Margin = new Padding(0, 0, 0, 20),
                Padding = Padding.Empty
            };

            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var logo = new PictureBox
            {
                Width = 250,
                Height = 250,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = ZraxionBrandAssets.CreateWelcomeLogo(),
                BackColor = surface,
                Margin = Padding.Empty,
                TabStop = false
            };

            panel.Controls.Add(logo, 1, 0);
            return panel;
        }

        private Control CreateFeatureStrip()
        {
            var strip = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = surface,
                Margin = new Padding(0, 0, 0, 20),
                Padding = Padding.Empty
            };

            AddChip(strip, "Exact precision");
            AddChip(strip, "Selection analytics");
            AddChip(strip, "Column analytics");
            AddChip(strip, "A/B compare");
            AddChip(strip, "Tolerance rules");
            AddChip(strip, "Per-tab state");
            AddChip(strip, "Open source");
            return strip;
        }

        private void AddChip(FlowLayoutPanel host, string text)
        {
            host.Controls.Add(new Label
            {
                AutoSize = true,
                Text = text,
                BackColor = surfaceAlt,
                ForeColor = foreground,
                Padding = new Padding(10, 5, 10, 5),
                Margin = new Padding(0, 0, 8, 8)
            });
        }

        private Control CreateCards()
        {
            var cards = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = surface,
                Margin = new Padding(0, 0, 0, 20),
                Padding = Padding.Empty
            };

            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            cards.Controls.Add(CreateWorkflowCard(), 0, 0);
            cards.Controls.Add(CreateLinksCard(), 1, 0);
            return cards;
        }

        private Control CreateWorkflowCard()
        {
            var card = CreateCard("Core workflow", "Built for the way you already use SSMS.");
            AddBody(card, "1  Select cells in the Results Grid");
            AddBody(card, "2  Right-click → Zraxion");
            AddBody(card, "3  Analyze or set Selection as A");
            AddBody(card, "4  Compare B against A with exact or tolerant matching");
            AddBody(card, "5  Copy exact invariant values when needed");
            return card;
        }

        private Control CreateLinksCard()
        {
            var card = CreateCard("Project", "Documentation, releases and support.");
            AddLink(card, "GitHub repository", ZraxionProductInfo.RepositoryUrl);
            AddLink(card, "Releases", ZraxionProductInfo.ReleasesUrl);
            AddLink(card, "Report an issue", ZraxionProductInfo.IssuesUrl);
            AddLink(card, "Documentation", ZraxionProductInfo.DocumentationUrl);
            AddLink(card, "License", ZraxionProductInfo.LicenseUrl);
            AddBody(card, "Support: voluntary crypto donations are listed below.");
            return card;
        }

        private Control CreateQuickStart()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = surfaceAlt,
                Padding = new Padding(18),
                Margin = new Padding(0, 0, 0, 18)
            };

            panel.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "Quick start",
                ForeColor = foreground,
                Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 6)
            });

            panel.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(1050, 0),
                Text = "Run any query, select numeric cells, then use Results Grid → Zraxion. Use the Analytics Bar for persistent metrics and View to switch between Selection metrics, Baseline A, and Last comparison.",
                ForeColor = foreground,
                Margin = new Padding(0, 0, 0, 6)
            });

            panel.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "Tip: update checks run quietly in the background and only surface an SSMS InfoBar when a newer release exists.",
                ForeColor = muted
            });

            return panel;
        }


        private Control CreateSupportSection()
        {
            var section = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = surface,
                Margin = new Padding(0, 0, 0, 18),
                Padding = Padding.Empty
            };

            section.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "Support the project",
                ForeColor = foreground,
                Font = new Font(
                    SystemFonts.MessageBoxFont.FontFamily,
                    14F,
                    FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 4)
            });

            section.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(1050, 0),
                Text = "Zraxion is free and open source. If it saves you time, voluntary donations help fund maintenance, testing, and future SSMS features.",
                ForeColor = muted,
                Margin = new Padding(0, 0, 0, 14)
            });

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = surface,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            grid.Controls.Add(
                CreateDonationCard(
                    "Bitcoin",
                    "Bitcoin network",
                    "bc1q5tl36qpk27hf7upl8l753xa0gcm57adrvmwgkz"),
                0,
                0);

            grid.Controls.Add(
                CreateDonationCard(
                    "Tether (USDT)",
                    "TRON / TRC20 only",
                    "TA5pibChqS7CeHiDvfP7V3uQVMynk6SxLq"),
                1,
                0);

            grid.Controls.Add(
                CreateDonationCard(
                    "EVM Networks",
                    "Ethereum • BNB Smart Chain • Polygon",
                    "0x6634E26BA0e323182B7A9a89278E9a7BbCa9aF70"),
                0,
                1);

            grid.Controls.Add(
                CreateDonationCard(
                    "Solana",
                    "Solana network",
                    "91kuBFEZAnRk8ixsuzAQnAVvtzLAsQpPFRsfe285ZKUC"),
                1,
                1);

            section.Controls.Add(grid);

            var warning = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = surfaceAlt,
                Padding = new Padding(14),
                Margin = new Padding(0, 12, 0, 0)
            };

            warning.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(1020, 0),
                Text = "Important: Always verify the complete wallet address and blockchain network before transferring cryptocurrency. Cryptocurrency transactions generally cannot be reversed.",
                ForeColor = foreground,
                Margin = Padding.Empty
            });

            section.Controls.Add(warning);
            return section;
        }

        private Control CreateDonationCard(
            string title,
            string network,
            string address)
        {
            var card = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = surfaceAlt,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(16),
                Margin = new Padding(0, 0, 10, 10)
            };

            card.Controls.Add(new Label
            {
                AutoSize = true,
                Text = title,
                ForeColor = foreground,
                Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 3)
            });

            card.Controls.Add(new Label
            {
                AutoSize = true,
                Text = network,
                ForeColor = muted,
                Margin = new Padding(0, 0, 0, 10)
            });

            var addressBox = new TextBox
            {
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = surfaceAlt,
                ForeColor = foreground,
                Text = address,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 8),
                TabStop = false
            };

            card.Controls.Add(addressBox);

            var copyButton = new Button
            {
                AutoSize = true,
                Text = "Copy address",
                FlatStyle = FlatStyle.System,
                Anchor = AnchorStyles.Left,
                Margin = Padding.Empty
            };

            copyButton.Click += (_, __) =>
            {
                try
                {
                    Clipboard.SetText(address);
                    copyButton.Text = "Copied";

                    var resetTimer = new System.Windows.Forms.Timer
                    {
                        Interval = 1600
                    };

                    resetTimer.Tick += (sender, args) =>
                    {
                        resetTimer.Stop();
                        resetTimer.Dispose();

                        if (!copyButton.IsDisposed)
                        {
                            copyButton.Text = "Copy address";
                        }
                    };

                    resetTimer.Start();
                }
                catch
                {
                    copyButton.Text = "Copy failed";
                }
            };

            card.Controls.Add(copyButton);
            return card;
        }

        private Control CreateFooter()
        {
            return new Label
            {
                AutoSize = true,
                Text = "LaneZero  •  Zraxion for SSMS 22  •  Local settings: %LocalAppData%\\Zraxion",
                ForeColor = muted,
                Margin = new Padding(0, 0, 0, 6)
            };
        }

        private FlowLayoutPanel CreateCard(string title, string subtitle)
        {
            var card = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = surface,
                Padding = new Padding(16),
                Margin = new Padding(0, 0, 10, 0),
                BorderStyle = BorderStyle.FixedSingle
            };

            card.Controls.Add(new Label
            {
                AutoSize = true,
                Text = title,
                ForeColor = foreground,
                Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 4)
            });

            card.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(480, 0),
                Text = subtitle,
                ForeColor = muted,
                Margin = new Padding(0, 0, 0, 12)
            });

            return card;
        }

        private void AddBody(FlowLayoutPanel card, string text)
        {
            card.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(480, 0),
                Text = text,
                ForeColor = foreground,
                Margin = new Padding(0, 4, 0, 4)
            });
        }

        private void AddLink(FlowLayoutPanel card, string text, string url)
        {
            var link = new LinkLabel
            {
                AutoSize = true,
                Text = text + "  ↗",
                LinkColor = accent,
                ActiveLinkColor = accent,
                VisitedLinkColor = accent,
                Margin = new Padding(0, 4, 0, 4)
            };

            link.LinkClicked += (_, __) => OpenExternalUrl(url);
            card.Controls.Add(link);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (Control control in Controls)
                {
                    DisposeImages(control);
                }
            }

            base.Dispose(disposing);
        }

        private static void DisposeImages(Control control)
        {
            if (control is PictureBox pictureBox && pictureBox.Image is not null)
            {
                pictureBox.Image.Dispose();
                pictureBox.Image = null;
            }

            foreach (Control child in control.Controls)
            {
                DisposeImages(child);
            }
        }

        internal static void OpenExternalUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Browser launch is non-critical.
            }
        }
    }
}
