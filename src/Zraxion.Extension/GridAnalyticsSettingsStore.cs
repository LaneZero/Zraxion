using System;
using System.Globalization;
using System.IO;

namespace Zraxion.Extension
{
    internal static class GridAnalyticsSettingsStore
    {
        private const string SettingsFileName =
            "grid-analytics.settings";

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

        public static GridAnalyticsDisplayOptions Load()
        {
            var options =
                new GridAnalyticsDisplayOptions();

            lock (SyncRoot)
            {
                if (!File.Exists(
                        SettingsFilePath))
                {
                    return options;
                }

                try
                {
                    foreach (var line in
                             File.ReadAllLines(
                                 SettingsFilePath))
                    {
                        ApplySetting(
                            options,
                            line);
                    }
                }
                catch (IOException)
                {
                    return
                        new GridAnalyticsDisplayOptions();
                }
                catch (UnauthorizedAccessException)
                {
                    return
                        new GridAnalyticsDisplayOptions();
                }
            }

            return options;
        }

        public static void Save(
            GridAnalyticsDisplayOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(
                    nameof(options));
            }

            var lines =
                new[]
                {
                    FormatSetting(
                        "ShowSum",
                        options.ShowSum),
                    FormatSetting(
                        "ShowCount",
                        options.ShowCount),
                    FormatSetting(
                        "ShowAverage",
                        options.ShowAverage),
                    FormatSetting(
                        "ShowMin",
                        options.ShowMin),
                    FormatSetting(
                        "ShowMax",
                        options.ShowMax),
                    FormatSetting(
                        "ShowNullOrEmpty",
                        options.ShowNullOrEmpty),
                    FormatSetting(
                        "ShowNonNumeric",
                        options.ShowNonNumeric),
                    FormatSetting(
                        "AutoRefresh",
                        options.AutoRefresh)
                };

            lock (SyncRoot)
            {
                TryWriteAtomically(
                    SettingsFilePath,
                    lines);
            }
        }

        private static void ApplySetting(
            GridAnalyticsDisplayOptions options,
            string line)
        {
            if (string.IsNullOrWhiteSpace(
                    line))
            {
                return;
            }

            var separatorIndex =
                line.IndexOf('=');

            if (separatorIndex <= 0 ||
                separatorIndex ==
                line.Length - 1)
            {
                return;
            }

            var name =
                line.Substring(
                        0,
                        separatorIndex)
                    .Trim();

            var valueText =
                line.Substring(
                        separatorIndex + 1)
                    .Trim();

            if (!bool.TryParse(
                    valueText,
                    out var value))
            {
                return;
            }

            switch (name)
            {
                case "ShowSum":
                    options.ShowSum = value;
                    break;

                case "ShowCount":
                    options.ShowCount = value;
                    break;

                case "ShowAverage":
                    options.ShowAverage = value;
                    break;

                case "ShowMin":
                    options.ShowMin = value;
                    break;

                case "ShowMax":
                    options.ShowMax = value;
                    break;

                case "ShowNullOrEmpty":
                    options.ShowNullOrEmpty = value;
                    break;

                case "ShowNonNumeric":
                    options.ShowNonNumeric = value;
                    break;

                case "AutoRefresh":
                    options.AutoRefresh = value;
                    break;
            }
        }

        private static string FormatSetting(
            string name,
            bool value)
        {
            return
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}={1}",
                    name,
                    value);
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
