using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Zraxion.Extension
{
    internal sealed class GridComparisonToleranceDialog : Form
    {
        private readonly ComboBox modeComboBox;
        private readonly TextBox absoluteTextBox;
        private readonly TextBox percentageTextBox;
        private readonly Label absoluteLabel;
        private readonly Label percentageLabel;
        private readonly Label explanationLabel;
        private readonly Label errorLabel;

        private GridComparisonToleranceDialog(
            GridComparisonTolerancePolicy currentPolicy)
        {
            Text = "Zraxion - Comparison Tolerance";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(430, 338);
            Font = SystemFonts.MessageBoxFont;

            var modeLabel = new Label
            {
                AutoSize = true,
                Text = "Tolerance mode",
                Location = new Point(18, 18)
            };

            modeComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(18, 40),
                Width = 390
            };

            modeComboBox.Items.AddRange(
                new object[]
                {
                    "Exact",
                    "Absolute",
                    "Percentage",
                    "Absolute OR Percentage"
                });

            absoluteLabel = new Label
            {
                AutoSize = true,
                Text = "Absolute tolerance",
                Location = new Point(18, 80)
            };

            absoluteTextBox = new TextBox
            {
                Location = new Point(18, 101),
                Width = 185
            };

            percentageLabel = new Label
            {
                AutoSize = true,
                Text = "Percentage tolerance (%)",
                Location = new Point(223, 80)
            };

            percentageTextBox = new TextBox
            {
                Location = new Point(223, 101),
                Width = 185
            };

            var explanationGroup = new GroupBox
            {
                Text = "How this changes Compare",
                Location = new Point(18, 136),
                Size = new Size(390, 122)
            };

            explanationLabel = new Label
            {
                AutoSize = false,
                Location = new Point(12, 22),
                Size = new Size(364, 88)
            };

            explanationGroup.Controls.Add(
                explanationLabel);

            errorLabel = new Label
            {
                AutoSize = false,
                Location = new Point(18, 267),
                Size = new Size(390, 20),
                ForeColor = Color.Firebrick
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.None,
                Location = new Point(252, 298),
                Width = 75
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(333, 298),
                Width = 75
            };

            okButton.Click += OnOkClick;
            modeComboBox.SelectedIndexChanged +=
                (_, __) => UpdateFieldState();

            Controls.Add(modeLabel);
            Controls.Add(modeComboBox);
            Controls.Add(absoluteLabel);
            Controls.Add(absoluteTextBox);
            Controls.Add(percentageLabel);
            Controls.Add(percentageTextBox);
            Controls.Add(explanationGroup);
            Controls.Add(errorLabel);
            Controls.Add(okButton);
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            LoadPolicy(currentPolicy);
        }

        internal GridComparisonTolerancePolicy? SelectedPolicy
        {
            get;
            private set;
        }

        internal static bool TryShow(
            GridComparisonTolerancePolicy currentPolicy,
            out GridComparisonTolerancePolicy tolerancePolicy)
        {
            using (var dialog =
                   new GridComparisonToleranceDialog(
                       currentPolicy))
            {
                var result =
                    dialog.ShowDialog();

                if (result == DialogResult.OK &&
                    dialog.SelectedPolicy is not null)
                {
                    tolerancePolicy =
                        dialog.SelectedPolicy;
                    return true;
                }
            }

            tolerancePolicy = currentPolicy;
            return false;
        }

        private void LoadPolicy(
            GridComparisonTolerancePolicy policy)
        {
            if (policy.IsExact)
            {
                modeComboBox.SelectedIndex = 0;
            }
            else if (
                policy.AbsoluteTolerance.HasValue &&
                policy.PercentageTolerance.HasValue)
            {
                modeComboBox.SelectedIndex = 3;
            }
            else if (policy.AbsoluteTolerance.HasValue)
            {
                modeComboBox.SelectedIndex = 1;
            }
            else
            {
                modeComboBox.SelectedIndex = 2;
            }

            absoluteTextBox.Text =
                policy.AbsoluteTolerance?.ToString(
                    "G29",
                    CultureInfo.CurrentCulture)
                ?? string.Empty;

            percentageTextBox.Text =
                policy.PercentageTolerance?.ToString(
                    "G29",
                    CultureInfo.CurrentCulture)
                ?? string.Empty;

            UpdateFieldState();
        }

        private void UpdateFieldState()
        {
            var mode =
                modeComboBox.SelectedIndex;

            var absoluteEnabled =
                mode == 1 ||
                mode == 3;

            var percentageEnabled =
                mode == 2 ||
                mode == 3;

            absoluteLabel.Enabled =
                absoluteEnabled;
            absoluteTextBox.Enabled =
                absoluteEnabled;

            percentageLabel.Enabled =
                percentageEnabled;
            percentageTextBox.Enabled =
                percentageEnabled;

            explanationLabel.Text =
                GetModeExplanation(mode);

            errorLabel.Text =
                string.Empty;
        }

        private static string GetModeExplanation(
            int mode)
        {
            switch (mode)
            {
                case 0:
                    return
                        "Exact: Compare reports MATCH only when A and B are exactly equal.\r\n" +
                        "Example: A 1000, B 1000 → MATCH.  B 1000.01 → MISMATCH.";

                case 1:
                    return
                        "Absolute: MATCH when |B - A| is less than or equal to the entered amount.\r\n" +
                        "Example: tolerance 1, A 1000, B 1000.50 → MATCH.";

                case 2:
                    return
                        "Percentage: MATCH when the percentage difference is within the entered %.\r\n" +
                        "Example: tolerance 0.1%, A 1,000,000, B 1,000,500 → MATCH.\r\n" +
                        "If A is 0, percentage tolerance cannot be evaluated.";

                case 3:
                    return
                        "Absolute OR Percentage: MATCH when either tolerance rule passes.\r\n" +
                        "Useful when both a fixed amount and a relative percentage are acceptable.\r\n" +
                        "If A is 0, only the absolute rule can produce a tolerance match.";

                default:
                    return string.Empty;
            }
        }

        private void OnOkClick(
            object? sender,
            EventArgs e)
        {
            var mode =
                modeComboBox.SelectedIndex;

            if (mode == 0)
            {
                SelectedPolicy =
                    GridComparisonTolerancePolicy.Exact;

                DialogResult =
                    DialogResult.OK;
                Close();
                return;
            }

            decimal? absoluteTolerance = null;
            decimal? percentageTolerance = null;

            if (mode == 1 ||
                mode == 3)
            {
                if (!TryParseNonNegative(
                        absoluteTextBox.Text,
                        out var absolute))
                {
                    errorLabel.Text =
                        "Absolute tolerance must be a non-negative number.";
                    absoluteTextBox.Focus();
                    return;
                }

                absoluteTolerance =
                    absolute;
            }

            if (mode == 2 ||
                mode == 3)
            {
                if (!TryParseNonNegative(
                        percentageTextBox.Text,
                        out var percentage))
                {
                    errorLabel.Text =
                        "Percentage tolerance must be a non-negative number.";
                    percentageTextBox.Focus();
                    return;
                }

                percentageTolerance =
                    percentage;
            }

            SelectedPolicy =
                new GridComparisonTolerancePolicy(
                    absoluteTolerance,
                    percentageTolerance);

            DialogResult =
                DialogResult.OK;
            Close();
        }

        private static bool TryParseNonNegative(
            string text,
            out decimal value)
        {
            var styles =
                NumberStyles.Number |
                NumberStyles.AllowExponent;

            return
                (decimal.TryParse(
                     text,
                     styles,
                     CultureInfo.CurrentCulture,
                     out value) ||
                 decimal.TryParse(
                     text,
                     styles,
                     CultureInfo.InvariantCulture,
                     out value)) &&
                value >= 0m;
        }
    }
}
