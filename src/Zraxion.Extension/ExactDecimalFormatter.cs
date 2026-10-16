using System;
using System.Globalization;
using System.Text;

namespace Zraxion.Extension
{
    internal static class ExactDecimalFormatter
    {
        internal static string Format(
            ExactDecimal value,
            CultureInfo culture)
        {
            if (culture is null)
            {
                throw new ArgumentNullException(
                    nameof(culture));
            }

            var raw =
                value.ToString(
                    culture);

            var numberFormat =
                culture.NumberFormat;

            var negativeSign =
                numberFormat.NegativeSign;

            var isNegative =
                !string.IsNullOrEmpty(
                    negativeSign) &&
                raw.StartsWith(
                    negativeSign,
                    StringComparison.Ordinal);

            var unsigned =
                isNegative
                    ? raw.Substring(
                        negativeSign.Length)
                    : raw;

            var decimalSeparator =
                numberFormat.NumberDecimalSeparator;

            var decimalIndex =
                string.IsNullOrEmpty(
                    decimalSeparator)
                    ? -1
                    : unsigned.IndexOf(
                        decimalSeparator,
                        StringComparison.Ordinal);

            var integerPart =
                decimalIndex >= 0
                    ? unsigned.Substring(
                        0,
                        decimalIndex)
                    : unsigned;

            var fractionalSuffix =
                decimalIndex >= 0
                    ? unsigned.Substring(
                        decimalIndex)
                    : string.Empty;

            var groupedInteger =
                ApplyGrouping(
                    integerPart,
                    numberFormat);

            return
                (isNegative
                    ? negativeSign
                    : string.Empty) +
                groupedInteger +
                fractionalSuffix;
        }

        internal static string FormatInvariant(
            ExactDecimal value)
        {
            return
                value.ToString(
                    CultureInfo.InvariantCulture);
        }

        private static string ApplyGrouping(
            string digits,
            NumberFormatInfo numberFormat)
        {
            if (digits.Length <= 3 ||
                string.IsNullOrEmpty(
                    numberFormat.NumberGroupSeparator))
            {
                return digits;
            }

            var groupSizes =
                numberFormat.NumberGroupSizes;

            if (groupSizes is null ||
                groupSizes.Length == 0 ||
                groupSizes[0] <= 0)
            {
                return digits;
            }

            var builder =
                new StringBuilder(
                    digits.Length +
                    digits.Length / 3);

            var end =
                digits.Length;

            var groupIndex = 0;
            var groupSize =
                groupSizes[0];

            while (end > 0 &&
                   groupSize > 0)
            {
                var start =
                    Math.Max(
                        0,
                        end -
                        groupSize);

                if (builder.Length > 0)
                {
                    builder.Insert(
                        0,
                        numberFormat.NumberGroupSeparator);
                }

                builder.Insert(
                    0,
                    digits.Substring(
                        start,
                        end - start));

                end =
                    start;

                if (end == 0)
                {
                    break;
                }

                if (groupIndex <
                    groupSizes.Length - 1)
                {
                    groupIndex++;

                    if (groupSizes[groupIndex] > 0)
                    {
                        groupSize =
                            groupSizes[groupIndex];
                    }
                }
            }

            if (end > 0)
            {
                if (builder.Length > 0)
                {
                    builder.Insert(
                        0,
                        numberFormat.NumberGroupSeparator);
                }

                builder.Insert(
                    0,
                    digits.Substring(
                        0,
                        end));
            }

            return builder.ToString();
        }
    }
}
