using System;
using System.IO;

namespace Zraxion.Extension
{
    internal static class ZraxionWelcomeState
    {
        private static readonly object syncRoot = new object();

        private static string SettingsDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Zraxion");

        private static string SettingsPath => Path.Combine(SettingsDirectory, "welcome.settings");

        internal static bool ShouldShowForCurrentVersion()
        {
            lock (syncRoot)
            {
                try
                {
                    if (!File.Exists(SettingsPath)) return true;
                    foreach (var line in File.ReadAllLines(SettingsPath))
                    {
                        const string prefix = "LastShownVersion=";
                        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                        var value = line.Substring(prefix.Length).Trim();
                        return !string.Equals(value, ZraxionProductInfo.Version, StringComparison.OrdinalIgnoreCase);
                    }
                }
                catch (IOException) { return true; }
                catch (UnauthorizedAccessException) { return true; }
                return true;
            }
        }

        internal static void MarkCurrentVersionShown()
        {
            lock (syncRoot)
            {
                try
                {
                    Directory.CreateDirectory(SettingsDirectory);
                    File.WriteAllText(SettingsPath, "LastShownVersion=" + ZraxionProductInfo.Version + Environment.NewLine);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
