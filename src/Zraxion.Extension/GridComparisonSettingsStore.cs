using System;
using System.Globalization;
using System.IO;

namespace Zraxion.Extension
{
    internal static class GridComparisonSettingsStore
    {
        private const string SettingsFileName =
            "comparison.settings";

        private const string AbsoluteToleranceName =
            "DefaultAbsoluteTolerance";

        private const string PercentageToleranceName =
            "DefaultPercentageTolerance";

        private static readonly object SyncRoot =
            new object();

        private static readonly string SettingsDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "Zraxion");

        private static readonly string SettingsFilePath =
            Path.Combine(
                SettingsDirectory,
                SettingsFileName);

        internal static GridComparisonTolerancePolicy
            LoadDefaultTolerancePolicy()
        {
            lock (SyncRoot)
            {
                decimal? absoluteTolerance = null;
                decimal? percentageTolerance = null;

                if (!File.Exists(
                        SettingsFilePath))
                {
                    return
                        GridComparisonTolerancePolicy.Exact;
                }

                try
                {
                    foreach (var line in
                             File.ReadAllLines(
                                 SettingsFilePath))
                    {
                        if (!TryParseSetting(
                                line,
                                out var name,
                                out var valueText))
                        {
                            continue;
                        }

                        if (string.Equals(
                                name,
                                AbsoluteToleranceName,
                                StringComparison.Ordinal))
                        {
                            if (TryParseNullableNonNegativeDecimal(
                                    valueText,
                                    out var parsed))
                            {
                                absoluteTolerance =
                                    parsed;
                            }

                            continue;
                        }

                        if (string.Equals(
                                name,
                                PercentageToleranceName,
                                StringComparison.Ordinal))
                        {
                            if (TryParseNullableNonNegativeDecimal(
                                    valueText,
                                    out var parsed))
                            {
                                percentageTolerance =
                                    parsed;
                            }
                        }
                    }
                }
                catch (IOException)
                {
                    return
                        GridComparisonTolerancePolicy.Exact;
                }
                catch (UnauthorizedAccessException)
                {
                    return
                        GridComparisonTolerancePolicy.Exact;
                }

                return
                    new GridComparisonTolerancePolicy(
                        absoluteTolerance,
                        percentageTolerance);
            }
        }

        internal static void SaveDefaultTolerancePolicy(
            GridComparisonTolerancePolicy tolerancePolicy)
        {
            if (tolerancePolicy is null)
            {
                throw new ArgumentNullException(
                    nameof(tolerancePolicy));
            }

            var lines =
                new[]
                {
                    FormatSetting(
                        AbsoluteToleranceName,
                        tolerancePolicy.AbsoluteTolerance),
                    FormatSetting(
                        PercentageToleranceName,
                        tolerancePolicy.PercentageTolerance)
                };

            lock (SyncRoot)
            {
                TryWriteAtomically(
                    SettingsFilePath,
                    lines);
            }
        }

        private static bool TryParseSetting(
            string line,
            out string name,
            out string valueText)
        {
            name = string.Empty;
            valueText = string.Empty;

            if (string.IsNullOrWhiteSpace(
                    line))
            {
                return false;
            }

            var separatorIndex =
                line.IndexOf('=');

            if (separatorIndex <= 0)
            {
                return false;
            }

            name =
                line.Substring(
                        0,
                        separatorIndex)
                    .Trim();

            valueText =
                separatorIndex ==
                line.Length - 1
                    ? string.Empty
                    : line.Substring(
                            separatorIndex + 1)
                        .Trim();

            return true;
        }

        private static bool
            TryParseNullableNonNegativeDecimal(
                string valueText,
                out decimal? value)
        {
            value = null;

            if (string.IsNullOrWhiteSpace(
                    valueText))
            {
                return true;
            }

            if (!decimal.TryParse(
                    valueText,
                    NumberStyles.Number |
                    NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture,
                    out var parsed) ||
                parsed < 0m)
            {
                return false;
            }

            value =
                parsed;

            return true;
        }

        private static string FormatSetting(
            string name,
            decimal? value)
        {
            return
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}={1}",
                    name,
                    value.HasValue
                        ? value.Value.ToString(
                            "G29",
                            CultureInfo.InvariantCulture)
                        : string.Empty);
        }

        private static void TryWriteAtomically(
            string destinationPath,
            string[] lines)
        {
            var temporaryPath =
                destinationPath + ".tmp";

            try
            {
                Directory.CreateDirectory(
                    SettingsDirectory);

                if (File.Exists(
                        temporaryPath))
                {
                    File.Delete(
                        temporaryPath);
                }

                File.WriteAllLines(
                    temporaryPath,
                    lines);

                if (File.Exists(
                        destinationPath))
                {
                    try
                    {
                        File.Replace(
                            temporaryPath,
                            destinationPath,
                            destinationBackupFileName: null);

                        return;
                    }
                    catch (IOException)
                    {
                        // Fall back for filesystems that do not support Replace.
                    }
                    catch (PlatformNotSupportedException)
                    {
                        // Fall back to overwrite below.
                    }
                }

                File.Copy(
                    temporaryPath,
                    destinationPath,
                    overwrite: true);

                File.Delete(
                    temporaryPath);
            }
            catch (IOException)
            {
                TryDeleteTemporaryFile(
                    temporaryPath);
            }
            catch (UnauthorizedAccessException)
            {
                TryDeleteTemporaryFile(
                    temporaryPath);
            }
        }

        private static void TryDeleteTemporaryFile(
            string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Settings cleanup must never affect SSMS usage.
            }
        }
    }
}
