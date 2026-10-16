using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Zraxion.Extension
{
    /// <summary>
    /// Exact base-10 number backed by BigInteger.
    ///
    /// Unlike System.Decimal, this type is not limited to 28-29 significant
    /// decimal digits. It is intended for SQL Server DECIMAL/NUMERIC values
    /// and exact aggregate/comparison arithmetic.
    ///
    /// Value = UnscaledValue * 10^(-Scale)
    /// </summary>
    internal readonly struct ExactDecimal :
        IComparable<ExactDecimal>,
        IEquatable<ExactDecimal>
    {
        private static readonly BigInteger Ten =
            new BigInteger(10);

        private readonly BigInteger unscaledValue;
        private readonly int scale;

        public static ExactDecimal Zero { get; } =
            new ExactDecimal(
                BigInteger.Zero,
                0,
                normalize: false);

        private ExactDecimal(
            BigInteger unscaledValue,
            int scale,
            bool normalize)
        {
            if (scale < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(scale));
            }

            if (normalize)
            {
                Normalize(
                    ref unscaledValue,
                    ref scale);
            }

            this.unscaledValue =
                unscaledValue;

            this.scale =
                scale;
        }

        public BigInteger UnscaledValue =>
            unscaledValue;

        public int Scale =>
            scale;

        public int Sign =>
            unscaledValue.Sign;

        public bool IsZero =>
            unscaledValue.IsZero;

        public static ExactDecimal FromInt64(
            long value)
        {
            return
                new ExactDecimal(
                    new BigInteger(value),
                    0,
                    normalize: false);
        }

        public static ExactDecimal FromDecimal(
            decimal value)
        {
            return Parse(
                value.ToString(
                    "G29",
                    CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture);
        }

        public static ExactDecimal Parse(
            string text,
            CultureInfo culture)
        {
            if (!TryParse(
                    text,
                    culture,
                    out var value))
            {
                throw new FormatException(
                    $"'{text}' is not a valid exact decimal value.");
            }

            return value;
        }

        public static bool TryParse(
            string? text,
            CultureInfo culture,
            out ExactDecimal value)
        {
            value = Zero;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (culture is null)
            {
                throw new ArgumentNullException(
                    nameof(culture));
            }

            var numberFormat =
                culture.NumberFormat;

            var normalized =
                text!.Trim();

            var isNegative =
                false;

            if (!string.IsNullOrEmpty(
                    numberFormat.NegativeSign) &&
                normalized.StartsWith(
                    numberFormat.NegativeSign,
                    StringComparison.Ordinal))
            {
                isNegative = true;

                normalized =
                    normalized.Substring(
                        numberFormat.NegativeSign.Length)
                    .TrimStart();
            }
            else if (normalized.StartsWith(
                         "-",
                         StringComparison.Ordinal))
            {
                isNegative = true;

                normalized =
                    normalized.Substring(1)
                    .TrimStart();
            }
            else if (!string.IsNullOrEmpty(
                         numberFormat.PositiveSign) &&
                     normalized.StartsWith(
                         numberFormat.PositiveSign,
                         StringComparison.Ordinal))
            {
                normalized =
                    normalized.Substring(
                        numberFormat.PositiveSign.Length)
                    .TrimStart();
            }
            else if (normalized.StartsWith(
                         "+",
                         StringComparison.Ordinal))
            {
                normalized =
                    normalized.Substring(1)
                    .TrimStart();
            }

            if (!string.IsNullOrEmpty(
                    numberFormat.CurrencySymbol))
            {
                normalized =
                    normalized.Replace(
                        numberFormat.CurrencySymbol,
                        string.Empty)
                    .Trim();
            }

            if (!TrySplitExponent(
                    normalized,
                    out var mantissa,
                    out var exponent))
            {
                return false;
            }

            var decimalSeparator =
                numberFormat.NumberDecimalSeparator;

            var groupSeparator =
                numberFormat.NumberGroupSeparator;

            if (!string.IsNullOrEmpty(
                    decimalSeparator) &&
                string.Equals(
                    decimalSeparator,
                    groupSeparator,
                    StringComparison.Ordinal))
            {
                return false;
            }

            NormalizeWhitespaceGroupSeparator(
                ref mantissa,
                groupSeparator);

            var decimalIndex =
                string.IsNullOrEmpty(
                    decimalSeparator)
                    ? -1
                    : mantissa.IndexOf(
                        decimalSeparator,
                        StringComparison.Ordinal);

            if (decimalIndex >= 0 &&
                mantissa.IndexOf(
                    decimalSeparator,
                    decimalIndex +
                    decimalSeparator.Length,
                    StringComparison.Ordinal) >= 0)
            {
                return false;
            }

            string integerPart;
            string fractionalPart;

            if (decimalIndex < 0)
            {
                integerPart =
                    mantissa;

                fractionalPart =
                    string.Empty;
            }
            else
            {
                integerPart =
                    mantissa.Substring(
                        0,
                        decimalIndex);

                fractionalPart =
                    mantissa.Substring(
                        decimalIndex +
                        decimalSeparator.Length);
            }

            integerPart =
                integerPart.Trim();

            fractionalPart =
                fractionalPart.Trim();

            if (integerPart.Length == 0)
            {
                integerPart = "0";
            }

            if (!string.IsNullOrEmpty(
                    groupSeparator) &&
                fractionalPart.IndexOf(
                    groupSeparator,
                    StringComparison.Ordinal) >= 0)
            {
                return false;
            }

            if (!TryNormalizeGroupedInteger(
                    integerPart,
                    groupSeparator,
                    numberFormat.NumberGroupSizes,
                    out integerPart))
            {
                return false;
            }

            if (!ContainsOnlyAsciiDigits(
                    integerPart) ||
                !ContainsOnlyAsciiDigits(
                    fractionalPart))
            {
                return false;
            }

            var digits =
                integerPart +
                fractionalPart;

            if (digits.Length == 0)
            {
                return false;
            }

            if (!BigInteger.TryParse(
                    digits,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var unscaled))
            {
                return false;
            }

            if (isNegative)
            {
                unscaled =
                    BigInteger.Negate(
                        unscaled);
            }

            var finalScaleLong =
                (long)fractionalPart.Length -
                exponent;

            if (finalScaleLong < 0)
            {
                var scaleIncrease =
                    checked(
                        (int)-finalScaleLong);

                unscaled *=
                    Pow10(
                        scaleIncrease);

                finalScaleLong = 0;
            }

            if (finalScaleLong >
                int.MaxValue)
            {
                return false;
            }

            value =
                new ExactDecimal(
                    unscaled,
                    (int)finalScaleLong,
                    normalize: true);

            return true;
        }

        /// <summary>
        /// Parses a numeric value read from the SSMS Results Grid without silently
        /// accepting culture-dependent ambiguity.
        ///
        /// The current culture and invariant culture are both attempted:
        /// - one valid interpretation: use it;
        /// - two identical interpretations: use it;
        /// - two different valid interpretations: reject as ambiguous.
        ///
        /// Example under a comma-decimal culture:
        /// "1,234" can mean 1.234 or 1234, so it is intentionally rejected.
        /// </summary>
        public static bool TryParseGridValue(
            string? text,
            CultureInfo currentCulture,
            out ExactDecimal value)
        {
            value = Zero;

            if (currentCulture is null)
            {
                throw new ArgumentNullException(
                    nameof(currentCulture));
            }

            var currentParsed =
                TryParse(
                    text,
                    currentCulture,
                    out var currentValue);

            var invariantParsed =
                TryParse(
                    text,
                    CultureInfo.InvariantCulture,
                    out var invariantValue);

            if (currentParsed &&
                invariantParsed)
            {
                if (currentValue !=
                    invariantValue)
                {
                    return false;
                }

                value =
                    currentValue;

                return true;
            }

            if (currentParsed)
            {
                value =
                    currentValue;

                return true;
            }

            if (invariantParsed)
            {
                value =
                    invariantValue;

                return true;
            }

            return false;
        }

        private static void NormalizeWhitespaceGroupSeparator(
            ref string mantissa,
            string groupSeparator)
        {
            if (groupSeparator != "\u00A0" &&
                groupSeparator != "\u202F" &&
                groupSeparator != " ")
            {
                return;
            }

            mantissa =
                mantissa
                    .Replace(
                        "\u00A0",
                        " ")
                    .Replace(
                        "\u202F",
                        " ");

            if (groupSeparator != " ")
            {
                mantissa =
                    mantissa.Replace(
                        " ",
                        groupSeparator);
            }
        }

        private static bool TryNormalizeGroupedInteger(
            string integerPart,
            string groupSeparator,
            int[] groupSizes,
            out string normalizedInteger)
        {
            normalizedInteger =
                integerPart;

            if (string.IsNullOrEmpty(
                    groupSeparator) ||
                integerPart.IndexOf(
                    groupSeparator,
                    StringComparison.Ordinal) < 0)
            {
                return
                    ContainsOnlyAsciiDigits(
                        integerPart);
            }

            var groups =
                integerPart.Split(
                    new[]
                    {
                        groupSeparator
                    },
                    StringSplitOptions.None);

            if (groups.Length < 2)
            {
                return false;
            }

            for (var i = 0;
                 i < groups.Length;
                 i++)
            {
                if (groups[i].Length == 0 ||
                    !ContainsOnlyAsciiDigits(
                        groups[i]))
                {
                    return false;
                }
            }

            if (!ValidateGroupSizes(
                    groups,
                    groupSizes))
            {
                return false;
            }

            normalizedInteger =
                string.Concat(
                    groups);

            return true;
        }

        private static bool ValidateGroupSizes(
            string[] groups,
            int[] groupSizes)
        {
            if (groupSizes is null ||
                groupSizes.Length == 0 ||
                groupSizes[0] <= 0)
            {
                return false;
            }

            var groupSizeIndex = 0;
            var expectedSize =
                groupSizes[0];

            for (var i =
                     groups.Length - 1;
                 i >= 1;
                 i--)
            {
                if (groups[i].Length !=
                    expectedSize)
                {
                    return false;
                }

                if (groupSizeIndex <
                    groupSizes.Length - 1)
                {
                    var nextSize =
                        groupSizes[
                            groupSizeIndex + 1];

                    if (nextSize > 0)
                    {
                        groupSizeIndex++;
                        expectedSize =
                            nextSize;
                    }
                }
            }

            return
                groups[0].Length >= 1 &&
                groups[0].Length <=
                expectedSize;
        }

        public ExactDecimal Abs()
        {
            return
                Sign >= 0
                    ? this
                    : new ExactDecimal(
                        BigInteger.Abs(
                            unscaledValue),
                        scale,
                        normalize: false);
        }

        public ExactDecimal Negate()
        {
            return
                new ExactDecimal(
                    BigInteger.Negate(
                        unscaledValue),
                    scale,
                    normalize: false);
        }

        public ExactDecimal Add(
            ExactDecimal other)
        {
            AlignScales(
                this,
                other,
                out var left,
                out var right,
                out var commonScale);

            return
                new ExactDecimal(
                    left + right,
                    commonScale,
                    normalize: true);
        }

        public ExactDecimal Subtract(
            ExactDecimal other)
        {
            AlignScales(
                this,
                other,
                out var left,
                out var right,
                out var commonScale);

            return
                new ExactDecimal(
                    left - right,
                    commonScale,
                    normalize: true);
        }

        public ExactDecimal Multiply(
            ExactDecimal other)
        {
            var resultScale =
                checked(
                    scale +
                    other.scale);

            return
                new ExactDecimal(
                    unscaledValue *
                    other.unscaledValue,
                    resultScale,
                    normalize: true);
        }

        public ExactDecimal Multiply(
            long value)
        {
            return
                new ExactDecimal(
                    unscaledValue *
                    value,
                    scale,
                    normalize: true);
        }

        /// <summary>
        /// Divides this value by a non-zero integer using decimal half-away-from-zero
        /// rounding when the result is repeating. maxFractionalDigits controls only
        /// the generated fractional precision; terminating results remain exact.
        /// </summary>
        public ExactDecimal Divide(
            long divisor,
            int maxFractionalDigits = 38)
        {
            if (divisor == 0)
            {
                throw new DivideByZeroException();
            }

            if (maxFractionalDigits < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxFractionalDigits));
            }

            var divisorBig =
                new BigInteger(
                    divisor);

            var sign =
                unscaledValue.Sign *
                divisorBig.Sign;

            var numerator =
                BigInteger.Abs(
                    unscaledValue);

            var denominator =
                BigInteger.Abs(
                    divisorBig);

            var integerPart =
                BigInteger.DivRem(
                    numerator,
                    denominator,
                    out var remainder);

            if (remainder.IsZero)
            {
                var signedInteger =
                    sign < 0
                        ? BigInteger.Negate(
                            integerPart)
                        : integerPart;

                return
                    new ExactDecimal(
                        signedInteger,
                        scale,
                        normalize: true);
            }

            var generatedDigits =
                new StringBuilder();

            for (var i = 0;
                 i < maxFractionalDigits &&
                 !remainder.IsZero;
                 i++)
            {
                remainder *= Ten;

                var digit =
                    BigInteger.DivRem(
                        remainder,
                        denominator,
                        out remainder);

                generatedDigits.Append(
                    (char)(
                        '0' +
                        (int)digit));
            }

            var roundUp =
                false;

            if (!remainder.IsZero)
            {
                remainder *= Ten;

                var nextDigit =
                    remainder /
                    denominator;

                roundUp =
                    nextDigit >= 5;
            }

            var fractionalScale =
                generatedDigits.Length;

            BigInteger fractional =
                BigInteger.Zero;

            if (fractionalScale > 0)
            {
                fractional =
                    BigInteger.Parse(
                        generatedDigits.ToString(),
                        CultureInfo.InvariantCulture);
            }

            var combined =
                integerPart *
                Pow10(
                    fractionalScale) +
                fractional;

            if (roundUp)
            {
                combined +=
                    BigInteger.One;
            }

            var resultScale =
                checked(
                    scale +
                    fractionalScale);

            if (sign < 0)
            {
                combined =
                    BigInteger.Negate(
                        combined);
            }

            return
                new ExactDecimal(
                    combined,
                    resultScale,
                    normalize: true);
        }

        /// <summary>
        /// Divides this value by another exact decimal. Terminating results are
        /// exact; repeating results are rounded half-away-from-zero to the
        /// requested number of fractional digits.
        /// </summary>
        public ExactDecimal Divide(
            ExactDecimal divisor,
            int maxFractionalDigits = 38)
        {
            if (divisor.IsZero)
            {
                throw new DivideByZeroException();
            }

            if (maxFractionalDigits < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxFractionalDigits));
            }

            var sign =
                Sign *
                divisor.Sign;

            var numerator =
                BigInteger.Abs(
                    unscaledValue);

            var denominator =
                BigInteger.Abs(
                    divisor.unscaledValue);

            var scaleDelta =
                divisor.scale -
                scale;

            if (scaleDelta > 0)
            {
                numerator *=
                    Pow10(
                        scaleDelta);
            }
            else if (scaleDelta < 0)
            {
                denominator *=
                    Pow10(
                        -scaleDelta);
            }

            var integerPart =
                BigInteger.DivRem(
                    numerator,
                    denominator,
                    out var remainder);

            if (remainder.IsZero)
            {
                if (sign < 0)
                {
                    integerPart =
                        BigInteger.Negate(
                            integerPart);
                }

                return
                    new ExactDecimal(
                        integerPart,
                        0,
                        normalize: true);
            }

            var generatedDigits =
                new StringBuilder();

            for (var i = 0;
                 i < maxFractionalDigits &&
                 !remainder.IsZero;
                 i++)
            {
                remainder *= Ten;

                var digit =
                    BigInteger.DivRem(
                        remainder,
                        denominator,
                        out remainder);

                generatedDigits.Append(
                    (char)(
                        '0' +
                        (int)digit));
            }

            var roundUp =
                false;

            if (!remainder.IsZero)
            {
                remainder *= Ten;

                var nextDigit =
                    remainder /
                    denominator;

                roundUp =
                    nextDigit >= 5;
            }

            var fractionalScale =
                generatedDigits.Length;

            BigInteger fractional =
                BigInteger.Zero;

            if (fractionalScale > 0)
            {
                fractional =
                    BigInteger.Parse(
                        generatedDigits.ToString(),
                        CultureInfo.InvariantCulture);
            }

            var combined =
                integerPart *
                Pow10(
                    fractionalScale) +
                fractional;

            if (roundUp)
            {
                combined +=
                    BigInteger.One;
            }

            if (sign < 0)
            {
                combined =
                    BigInteger.Negate(
                        combined);
            }

            return
                new ExactDecimal(
                    combined,
                    fractionalScale,
                    normalize: true);
        }

        public int CompareTo(
            ExactDecimal other)
        {
            AlignScales(
                this,
                other,
                out var left,
                out var right,
                out _);

            return
                left.CompareTo(
                    right);
        }

        public bool Equals(
            ExactDecimal other)
        {
            return
                CompareTo(
                    other) == 0;
        }

        public override bool Equals(
            object? obj)
        {
            return
                obj is ExactDecimal other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return
                    (unscaledValue.GetHashCode() * 397) ^
                    scale;
            }
        }

        public override string ToString()
        {
            return
                ToString(
                    CultureInfo.InvariantCulture);
        }

        public string ToString(
            CultureInfo culture)
        {
            if (culture is null)
            {
                throw new ArgumentNullException(
                    nameof(culture));
            }

            var separator =
                culture.NumberFormat
                    .NumberDecimalSeparator;

            var absoluteDigits =
                BigInteger.Abs(
                    unscaledValue)
                .ToString(
                    CultureInfo.InvariantCulture);

            string integerPart;
            string fractionalPart;

            if (scale == 0)
            {
                integerPart =
                    absoluteDigits;

                fractionalPart =
                    string.Empty;
            }
            else if (absoluteDigits.Length <=
                     scale)
            {
                integerPart =
                    "0";

                fractionalPart =
                    new string(
                        '0',
                        scale -
                        absoluteDigits.Length) +
                    absoluteDigits;
            }
            else
            {
                var split =
                    absoluteDigits.Length -
                    scale;

                integerPart =
                    absoluteDigits.Substring(
                        0,
                        split);

                fractionalPart =
                    absoluteDigits.Substring(
                        split);
            }

            var signPrefix =
                Sign < 0
                    ? culture.NumberFormat
                        .NegativeSign
                    : string.Empty;

            return
                fractionalPart.Length == 0
                    ? signPrefix +
                      integerPart
                    : signPrefix +
                      integerPart +
                      separator +
                      fractionalPart;
        }

        public static ExactDecimal operator +(
            ExactDecimal left,
            ExactDecimal right)
        {
            return
                left.Add(
                    right);
        }

        public static ExactDecimal operator -(
            ExactDecimal left,
            ExactDecimal right)
        {
            return
                left.Subtract(
                    right);
        }

        public static ExactDecimal operator *(
            ExactDecimal left,
            ExactDecimal right)
        {
            return
                left.Multiply(
                    right);
        }

        public static ExactDecimal operator -(
            ExactDecimal value)
        {
            return
                value.Negate();
        }

        public static bool operator ==(
            ExactDecimal left,
            ExactDecimal right)
        {
            return
                left.Equals(
                    right);
        }

        public static bool operator !=(
            ExactDecimal left,
            ExactDecimal right)
        {
            return
                !left.Equals(
                    right);
        }

        public static bool operator <(
            ExactDecimal left,
            ExactDecimal right)
        {
            return
                left.CompareTo(
                    right) < 0;
        }

        public static bool operator >(
            ExactDecimal left,
            ExactDecimal right)
        {
            return
                left.CompareTo(
                    right) > 0;
        }

        public static bool operator <=(
            ExactDecimal left,
            ExactDecimal right)
        {
            return
                left.CompareTo(
                    right) <= 0;
        }

        public static bool operator >=(
            ExactDecimal left,
            ExactDecimal right)
        {
            return
                left.CompareTo(
                    right) >= 0;
        }

        private static void Normalize(
            ref BigInteger unscaledValue,
            ref int scale)
        {
            if (unscaledValue.IsZero)
            {
                scale = 0;
                return;
            }

            while (scale > 0)
            {
                var quotient =
                    BigInteger.DivRem(
                        unscaledValue,
                        Ten,
                        out var remainder);

                if (!remainder.IsZero)
                {
                    break;
                }

                unscaledValue =
                    quotient;

                scale--;
            }
        }

        private static void AlignScales(
            ExactDecimal left,
            ExactDecimal right,
            out BigInteger leftUnscaled,
            out BigInteger rightUnscaled,
            out int commonScale)
        {
            commonScale =
                Math.Max(
                    left.scale,
                    right.scale);

            leftUnscaled =
                left.unscaledValue *
                Pow10(
                    commonScale -
                    left.scale);

            rightUnscaled =
                right.unscaledValue *
                Pow10(
                    commonScale -
                    right.scale);
        }

        private static BigInteger Pow10(
            int exponent)
        {
            if (exponent < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exponent));
            }

            return
                BigInteger.Pow(
                    Ten,
                    exponent);
        }

        private static bool TrySplitExponent(
            string text,
            out string mantissa,
            out int exponent)
        {
            mantissa =
                text;

            exponent =
                0;

            var exponentIndex =
                text.IndexOfAny(
                    new[]
                    {
                        'e',
                        'E'
                    });

            if (exponentIndex < 0)
            {
                return true;
            }

            if (text.IndexOfAny(
                    new[]
                    {
                        'e',
                        'E'
                    },
                    exponentIndex + 1) >= 0)
            {
                return false;
            }

            mantissa =
                text.Substring(
                    0,
                    exponentIndex);

            var exponentText =
                text.Substring(
                    exponentIndex + 1);

            return
                exponentText.Length > 0 &&
                int.TryParse(
                    exponentText,
                    NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture,
                    out exponent);
        }

        private static bool ContainsOnlyAsciiDigits(
            string text)
        {
            for (var i = 0;
                 i < text.Length;
                 i++)
            {
                var ch =
                    text[i];

                if (ch < '0' ||
                    ch > '9')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
